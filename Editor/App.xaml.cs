using System.Windows;

namespace Editor
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.ToString(), "Unhandled Exception");
                e.Handled = true;
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Start with the welcome window
            var welcomeWindow = new WelcomeWindow();
            welcomeWindow.Show();
        }
    }
} 