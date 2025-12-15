using CommunityToolkit.Maui;
using DuplicateFolderScanner.Pages;
using DuplicateFolderScanner.Services;
using Fonts;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;

namespace DuplicateFolderScanner
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if IOS || MACCATALYST
    				handlers.AddHandler<Microsoft.Maui.Controls.CollectionView, Microsoft.Maui.Controls.Handlers.Items2.CollectionViewHandler2>();
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // Register your services here
            builder.Services.AddSingleton<FolderScannerService>();
            builder.Services.AddSingleton<TimeService>();
            builder.Services.AddSingleton<DuplicateDetectionService>();
            builder.Services.AddSingleton<FolderEnumeratorService>();
            builder.Services.AddSingleton<ProgressTracker>();

            // Register your pages
            builder.Services.AddTransient<MainPage>();

            return builder.Build();
        }
    }
}
