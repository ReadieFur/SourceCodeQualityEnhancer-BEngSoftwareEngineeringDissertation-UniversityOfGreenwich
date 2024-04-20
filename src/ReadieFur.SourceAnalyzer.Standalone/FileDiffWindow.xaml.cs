using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WPFTemplate.Controls;
using WPFTemplate.Styles;
using Controls = WPFTemplate.Controls;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    public partial class FileDiffWindow : WindowChrome
    {
        private static readonly SolidColorBrush _transparent = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 0, 0, 0));
        private SolutionChanges? _solutionChanges;
        private DocumentId? _currentDocumentId;

        public event Action? OnSaveInPlace = null;
        public event Action<string>? OnSaveAsNew = null;

        public FileDiffWindow()
        {
            _solutionChanges = null;
            Init();
        }

        public FileDiffWindow(SolutionChanges solutionChanges)
        {
            _solutionChanges = solutionChanges;
            Init();
            FilesListBox.Items.Clear();
        }

        private void Init()
        {
            InitializeComponent();

            StylesManager.onChange += StylesManager_onChange;
            //Trigger the styles to be updated here to override the default values.
            StylesManager_onChange();
        }

        private async void StylesManager_onChange()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                Foreground = StylesManager.foreground;
                Background = StylesManager.background;
                BackgroundAlt = StylesManager.backgroundAlt;
                Accent = StylesManager.accent;
            });

            foreach (object item in FilesListBox.Items)
                if (item is Controls.ListBoxItem listBoxItem && listBoxItem.Content is Label label)
                    await UpdateFileListingStyles(listBoxItem, label);
        }

        protected override async void WindowBase_Loaded(object sender, RoutedEventArgs e)
        {
            base.WindowBase_Loaded(sender, e);

            /*await Dispatcher.InvokeAsync(() =>
            {
                ((Grid)HeaderItems.Parent).Children.Remove(HeaderItems);
                headerLeft.Children.Add(HeaderItems);
            });*/

            if (_solutionChanges is not null)
            {
                await Task.Run(async () =>
                {
                    ConcurrentBag<Controls.ListBoxItem> listBoxItems = new();

                    foreach (ProjectChanges projectChanges in _solutionChanges.Value.GetProjectChanges())
                    {
                        foreach (DocumentId documentId in projectChanges.GetChangedDocuments())
                        {
                            if (projectChanges.NewProject.GetDocument(documentId) is not Document newDocument)
                                continue;

                            /*if (projectChanges.OldProject.GetDocument(documentId) is not Document oldDocument)
                                continue;*/

                            await Dispatcher.InvokeAsync(() =>
                            {
                                Label label = new();
                                label.Content = newDocument.Name;
                                label.Tag = projectChanges.OldProject.GetDocument(documentId)?.FilePath ?? string.Empty;

                                Controls.ListBoxItem listBoxItem = new();
                                listBoxItem.Content = label;
                                listBoxItem.Tag = documentId;

                                //Dosen't need to be called asynchonously.
                                UpdateFileListingStyles(listBoxItem, label);

                                //FilesListBox.Items.Add(listBoxItem);
                                listBoxItems.Add(listBoxItem);
                            });
                        }
                    }

                    IOrderedEnumerable<Controls.ListBoxItem> orderedItems = listBoxItems.OrderBy(i => ((Label)i.Content).Tag);

                    await Dispatcher.InvokeAsync(() =>
                    {
                        foreach (Controls.ListBoxItem listBoxItem in orderedItems)
                            FilesListBox.Items.Add(listBoxItem);

                        if (FilesListBox.Items.Count > 0)
                            FilesListBox.SelectedIndex = 0;
                    });
                });
            }
        }

        private DispatcherOperation UpdateFileListingStyles(Controls.ListBoxItem listBoxItem, Label label)
        {
            return Dispatcher.InvokeAsync(() =>
            {
                listBoxItem.MouseOverBackground = StylesManager.backgroundAlt;
                listBoxItem.MouseOverBorderBrush = StylesManager.backgroundAlt;
                listBoxItem.SelectedActiveBackground = StylesManager.accent;
                listBoxItem.SelectedActiveBorderBrush = StylesManager.accent;
                listBoxItem.SelectedInactiveBackground = StylesManager.accent;
                listBoxItem.SelectedInactiveBorderBrush = StylesManager.accent;

                label.Foreground = StylesManager.foreground;
            });
        }

        private async void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //If the item is already selected, do not re-generate the diff.
            if (FilesListBox.SelectedItem is not Controls.ListBoxItem listBoxItem || listBoxItem.Tag is not DocumentId documentId || documentId == _currentDocumentId)
                return;
            
            _currentDocumentId = documentId;

            await GenerateFileDiff(documentId);
        }

        private async Task GenerateFileDiff(DocumentId documentId, CancellationToken cancellationToken = default)
        {
            ProjectChanges? projectChanges = null;
            Document? newDocument = null;
            Document? oldDocument = null;
            foreach (ProjectChanges _projectChanges in _solutionChanges!.Value.GetProjectChanges())
            {
                if (_projectChanges.NewProject.GetDocument(documentId) is not Document _newDocument
                    || _projectChanges.OldProject.GetDocument(documentId) is not Document _oldDocument)
                    continue;

                projectChanges = _projectChanges;
                newDocument = _newDocument;
                oldDocument = _oldDocument;
                break;
            }

            if (projectChanges is null || newDocument is null || oldDocument is null)
                return;

            //SourceText newDocumentSource = await newDocument.GetTextAsync(cancellationToken);
            //SourceText oldDocumentSource = await oldDocument.GetTextAsync(cancellationToken);
            //GetTextChanges is producing unreliable results.
            //IReadOnlyList<TextChange> textChanges = newDocumentText.GetTextChanges(oldDocumentText);

            //Manual differencing is required.
            SyntaxTree newDocumentSyntax = await newDocument.GetSyntaxTreeAsync(cancellationToken);
            SyntaxTree oldDocumentSyntax = await oldDocument.GetSyntaxTreeAsync(cancellationToken);

            IEnumerable<SyntaxTreeLine> newDocumentLines = await GetSyntaxTreeLines(newDocumentSyntax, cancellationToken);
            IEnumerable<SyntaxTreeLine> oldDocumentLines = await GetSyntaxTreeLines(oldDocumentSyntax, cancellationToken);

            DiffView.OldText = string.Join("", oldDocumentLines);
            DiffView.NewText = string.Join("", newDocumentLines);
        }

        private async Task<IEnumerable<SyntaxTreeLine>> GetSyntaxTreeLines(SyntaxTree syntaxTree, CancellationToken cancellationToken = default)
        {
            Dictionary<int, List<SyntaxNodeTokenOrTrivia>> lines = new();

            foreach (SyntaxToken syntaxToken in (await syntaxTree.GetRootAsync(cancellationToken)).DescendantTokens(_ => true, true))
            {
                int tokenLine = syntaxToken.GetLocation().GetLineSpan().StartLinePosition.Line;

                if (!lines.ContainsKey(tokenLine))
                    lines.Add(tokenLine, new());

                foreach (SyntaxTrivia syntaxTrivia in syntaxToken.LeadingTrivia)
                {
                    int triviaLine = syntaxTrivia.GetLocation().GetLineSpan().StartLinePosition.Line;
                    
                    if (!lines.ContainsKey(triviaLine))
                        lines.Add(triviaLine, new());

                    lines[triviaLine].Add(syntaxTrivia);
                }

                lines[tokenLine].Add(syntaxToken);

                foreach (SyntaxTrivia syntaxTrivia in syntaxToken.TrailingTrivia)
                {
                    int triviaLine = syntaxTrivia.GetLocation().GetLineSpan().StartLinePosition.Line;

                    if (!lines.ContainsKey(triviaLine))
                        lines.Add(triviaLine, new());

                    lines[triviaLine].Add(syntaxTrivia);
                }
            }

            IEnumerable<SyntaxTreeLine> sortedLines = lines
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => new SyntaxTreeLine(kvp.Key, kvp.Value));
            return sortedLines;
        }

        private void SaveInPlace_Click(object sender, RoutedEventArgs e) => OnSaveInPlace?.Invoke();

        //https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/how-to-open-common-system-dialog-box?view=netdesktop-8.0&viewFallbackFrom=netframeworkdesktop-4.8
        //https://stackoverflow.com/questions/11624298/how-do-i-use-openfiledialog-to-select-a-folder
        private void SaveAsNew_Click(object sender, RoutedEventArgs e)
        {
            //Don't use the WinForms FolderBrowserDialog as it uses the bad tree-only view.
            //System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new();
            FolderBrowserEx.FolderBrowserDialog folderBrowserDialog = new();
            folderBrowserDialog.Title = "Select an empty folder to save solution to";
            //folderBrowserDialog.InitialFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            folderBrowserDialog.InitialFolder =
                _solutionChanges.HasValue && File.Exists(_solutionChanges.Value.GetProjectChanges().First().OldProject.FilePath)
                ? Directory.GetParent(_solutionChanges.Value.GetProjectChanges().First().OldProject.FilePath).FullName
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            folderBrowserDialog.AllowMultiSelect = false;

            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            try
            {
                const string NOT_EMPTY_MESSAGE = "The selected folder is not empty. Please select an empty folder to save the solution to.";

                if (Directory.GetFiles(folderBrowserDialog.SelectedFolder).Length > 0)
                {
                    Console.WriteLine(NOT_EMPTY_MESSAGE);
                    MessageBox.Show(NOT_EMPTY_MESSAGE, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Directory.GetDirectories(folderBrowserDialog.SelectedFolder).Length > 0)
                {
                    Console.WriteLine(NOT_EMPTY_MESSAGE);
                    MessageBox.Show(NOT_EMPTY_MESSAGE, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch
            {
                const string ACCESS_DENIED_MESSAGE = "Access to the folder is denied.";
                Console.WriteLine(ACCESS_DENIED_MESSAGE);
                MessageBox.Show(ACCESS_DENIED_MESSAGE, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OnSaveAsNew?.Invoke(folderBrowserDialog.SelectedFolder);
        }
    }
}
