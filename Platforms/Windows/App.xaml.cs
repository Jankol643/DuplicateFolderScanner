// In Platforms/Windows/App.xaml.cs
using Microsoft.UI.Xaml;

namespace DuplicateFolderScanner.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            // Register for system events that might help with responsiveness
            Microsoft.UI.Xaml.Application.Current.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Log the exception
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");

            // Don't mark as handled if it's a critical error
            if (e.Exception is OutOfMemoryException || e.Exception is StackOverflowException)
            {
                e.Handled = false;
            }
        }
    }
}