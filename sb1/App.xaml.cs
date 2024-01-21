using System.Windows;

namespace sbx
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += (sender, e) =>
            {
                MessageBox.Show(e.Exception.Message + $"\n{e.Exception.StackTrace}", $"Unhandled {e.GetType().Name}");
                e.Handled = !System.Diagnostics.Debugger.IsAttached;
            };
        }
    }
}
