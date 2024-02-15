using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using ReadieFur.SourceAnalyzer.Core.Configuration;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace ReadieFur.SourceAnalyzer.VSIX
{
    [PackageRegistration(UseManagedResourcesOnly = false, AllowsBackgroundLoading = true)]
    [Guid("7d345499-7f6e-4d98-bd53-8c953c0f9d80")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasSingleProject_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionHasMultipleProjects_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class VSPackage : AsyncPackage, IVsSolutionEvents, IVsFileChangeEvents
    {
        //https://stackoverflow.com/questions/23806095/how-to-correctly-react-on-file-change
        private IVsFileChangeEx _fileChangeService;
        private uint _fileChangeCookie;
        private IVsUIShell _uiShell;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

#if false
            _fileChangeService = (IVsFileChangeEx)await GetServiceAsync(typeof(SVsFileChangeEx));
            _uiShell = (IVsUIShell)await GetServiceAsync(typeof(SVsUIShell));

            if (await GetServiceAsync(typeof(SVsSolution)) is not IVsSolution solution)
                throw new InvalidOperationException();

            solution.AdviseSolutionEvents(this, out _);

            //If we are already loaded into a solution then it is highly likley that we missed the above events and so we should call the OnAfterOpenSolution method manually.
            solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION, Guid.Empty, out IEnumHierarchies enumHierarchies);
            if (enumHierarchies is not null)
                OnAfterOpenSolution(null, 0);
#endif
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            SetupFileWatcher();
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            SetupFileWatcher();
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            UnadviseFileWatcher();
            return VSConstants.S_OK;
        }

        private void SetupFileWatcher()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _fileChangeService.AdviseFileChange(
                    ConfigManager.Instance.Manager.ConfigPath,
                    (uint)(_VSFILECHANGEFLAGS.VSFILECHG_Time | _VSFILECHANGEFLAGS.VSFILECHG_Size | _VSFILECHANGEFLAGS.VSFILECHG_Del),
                    this,
                    out _fileChangeCookie
                );

                ShowToastNotification("Loaded source-analyzer.yaml configuration file: " + ConfigManager.Instance.Manager.ConfigPath);
            }
            catch { }
        }

        public int FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            foreach (string file in rgpszFile)
            {
                if (file == ConfigManager.Instance.Manager.ConfigPath)
                {
                    //TODO: Alert user that the IDE needs to be restarted.
                    ShowToastNotification("The active source-analyzer.yaml file has been modified. Please restart the IDE to apply the changes.");
                    break;
                }
            }

            return VSConstants.S_OK;
        }

        private void UnadviseFileWatcher()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _fileChangeService.UnadviseFileChange(_fileChangeCookie);
            }
            catch {}
        }

        private void ShowToastNotification(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //Create a new toast notification
            IVsInfoBarUIFactory infoBarUIFactory = (IVsInfoBarUIFactory)GetService(typeof(SVsInfoBarUIFactory));
            IVsInfoBarUIElement infoBarUIElement = infoBarUIFactory.CreateInfoBar(new InfoBarModel(message));

            //https://learn.microsoft.com/en-us/visualstudio/extensibility/vsix/recipes/notifications?view=vs-2022
            //TODO: See how to do this without the community toolkit.
            //Show the toast notification
            //_uiShell?.PromptInfoBar(infoBarUIElement);
        }

        #region Unused
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

        public int OnBeforeCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        public int OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;

        public int DirectoryChanged(string pszDirectory) => VSConstants.S_OK;
        #endregion
    }
}
