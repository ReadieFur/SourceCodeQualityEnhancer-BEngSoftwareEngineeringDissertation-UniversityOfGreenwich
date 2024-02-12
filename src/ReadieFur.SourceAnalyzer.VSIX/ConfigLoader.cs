﻿#define USE_BACKGROUND_THREAD

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
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    //[ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
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
            ThreadHelper.ThrowIfNotOnUIThread();

            //Register to IVsSolution.
            (GetService(typeof(SVsSolution)) as IVsSolution).AdviseSolutionEvents(this, out _);
        }

        //TODO: Check how this class is used, is it reloaded between sessions or should I check for a new session.
        private bool CheckForConfigurationFile(string filePath)
        {
            if (!filePath.EndsWith("source-analyzer.yaml"))
                return false;

            bool loadSuccess = false;
            JoinableTaskFactory.Run(async delegate { loadSuccess = await Core.Config.ConfigLoader.Load(filePath); });
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
