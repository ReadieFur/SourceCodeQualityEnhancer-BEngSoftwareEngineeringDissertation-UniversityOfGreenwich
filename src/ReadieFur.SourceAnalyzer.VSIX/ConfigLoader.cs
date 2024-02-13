#define USE_BACKGROUND_THREAD

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using EnvDTE;

namespace ReadieFur.SourceAnalyzer.VSIX
{
    //https://stackoverflow.com/questions/54044350/vs2017-vsix-just-created-asyncpackage-is-not-being-instanced
    [PackageRegistration(UseManagedResourcesOnly = false, AllowsBackgroundLoading =
#if USE_BACKGROUND_THREAD
        true
#else
        false
#endif
    )]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] //TODO: Template values to be populated in a .resx file.
    [Guid("30618704-b18b-4501-8174-2164f88112a5")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    //https://stackoverflow.com/questions/61313164/how-to-have-a-vspackage-notified-when-initial-solution-loaded-asynchronously
    //[ProvideToolWindow(typeof(ToolWindow))]
    //TODO: (HIGH PRIORITY) Figure out why the VsPackage is never loaded if opening a solution directly. UPDATE: It does load but as it is asyncronous, at least while debugging, it loads too slowly so it misses the events.
    public sealed class ConfigLoader :
#if USE_BACKGROUND_THREAD
        AsyncPackage
#else
        Package
#endif
        //TODO: Work around the package being loaded asyncronously as this can cause the package to not catch the loading of a solution thereby missing the events we need to react to below.
        , IVsSolutionEvents
    {
        private IVsSolution? _currentSolution = null;
        private string? _solutionConfigFile = null;

#if USE_BACKGROUND_THREAD
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Init();
        }
#else
        protected override void Initialize()
        {
            base.Initialize();
            Init();
        }
#endif

        private void Init()
        {
#if DEBUG && false
            //Wait for the debugger when in debug mode as this package loads asyncronously and seems to never be loaded if a solution is opened too fast.
            //By waiting I can wait for this to trigger before manually proceeding.
            while (!System.Diagnostics.Debugger.IsAttached)
                System.Threading.Thread.Sleep(100);
            System.Diagnostics.Debugger.Break();
#endif

            ThreadHelper.ThrowIfNotOnUIThread();

            //Register to IVsSolution.
            if (GetService(typeof(SVsSolution)) is not IVsSolution solution)
                throw new InvalidOperationException();

            //Subscribe to the IDE solution events.
            solution.AdviseSolutionEvents(this, out _);

            //If we are already loaded into a solution then it is highly likley that we missed the above events and so we should call the OnAfterOpenSolution method manually.
            solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, Guid.Empty, out IEnumHierarchies enumHierarchies);
            if (enumHierarchies is not null)
                OnAfterOpenSolution(null, 0);
        }

        //TODO: Check how this class is used, is it reloaded between sessions or should I check for a new session.
        private bool CheckForConfigurationFile(string filePath)
        {
            if (!filePath.EndsWith("source-analyzer.yaml"))
                return false;

            bool loadSuccess = false;
#if USE_BACKGROUND_THREAD
            JoinableTaskFactory.Run(async delegate { loadSuccess = await Core.Config.ConfigLoader.LoadAsync(filePath); });
#else
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            loadSuccess = Core.Config.ConfigLoader.Load(filePath).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
#endif
            return loadSuccess;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //Called before OnAfterOpenSolution. If OnAfterOpenSolution did not find any configuration files then check for a file here.
            if (pHierarchy is null)
                return VSConstants.S_FALSE;

            IVsSolution solution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution.Equals(_currentSolution) && _solutionConfigFile is not null)
                return VSConstants.S_OK;
            _currentSolution = solution;
            _solutionConfigFile = null;

            pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object project);

            if (project is not Project dteProject || dteProject.ProjectItems is null)
                return VSConstants.S_FALSE;
                
            foreach (ProjectItem item in dteProject.ProjectItems)
                if (item.FileCount > 0 && CheckForConfigurationFile(item.FileNames[1]))
                    break;

            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsSolution solution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (solution.Equals(_currentSolution))
                return VSConstants.S_OK;
            _currentSolution = solution;

            solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, Guid.Empty, out IEnumHierarchies enumHierarchies);
            if (enumHierarchies is null)
                return VSConstants.S_FALSE;

            IVsHierarchy[] hierarchies = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchies, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                IVsHierarchy projectHierarchy = hierarchies[0];
                if (projectHierarchy != null)
                {
                    projectHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object project);

                    if (project is Project dteProject)
                    {
                        bool foundValidConfigurationFile = false;
                        foreach (ProjectItem item in dteProject.ProjectItems)
                            if (foundValidConfigurationFile = CheckForConfigurationFile(item.FileNames[1]))
                                break;
                        if (foundValidConfigurationFile)
                            break;
                    }
                }
            }

            return VSConstants.S_OK;
        }

        #region Unused
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        public int OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;
        #endregion
    }
}
