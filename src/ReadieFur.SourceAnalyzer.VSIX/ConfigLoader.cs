#define USE_BACKGROUND_THREAD

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;

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
        , IVsSolutionEvents
    {
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
            IVsSolution solution = (IVsSolution)GetService(typeof(SVsSolution));
            solution.AdviseSolutionEvents(this, out _);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            //Called before OnAfterOpenSolution. If OnAfterOpenSolution did not find any configuration files then check for a file here.
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            //Check for a solution config file.
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
