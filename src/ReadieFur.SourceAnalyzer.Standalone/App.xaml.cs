using System.Windows;

namespace ReadieFur.SourceAnalyzer.Standalone
{
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            await Program.Main(e.Args);
            Shutdown();
        }
    }
}
