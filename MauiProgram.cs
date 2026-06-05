using Microsoft.Extensions.Logging;
using VentItMaui.Services;
using VentItMaui.ViewModels;
using VentItMaui.Views;

namespace VentItMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Services
		builder.Services.AddSingleton<SettingsService>();
		builder.Services.AddSingleton<DatabaseService>();
		builder.Services.AddSingleton<ClaudeService>();
		builder.Services.AddSingleton<JournalService>();
		builder.Services.AddSingleton<ChatSessionService>();
		builder.Services.AddSingleton<TimetableService>();
		builder.Services.AddSingleton<SongwritingService>();
		builder.Services.AddSingleton<GoogleDriveService>();

		// ViewModels
		builder.Services.AddSingleton<SettingsViewModel>();

		// Views
		builder.Services.AddSingleton<ChatPage>();
		builder.Services.AddSingleton<JournalPage>();
		builder.Services.AddSingleton<NotesPage>();
		builder.Services.AddSingleton<PlannerPage>();
		builder.Services.AddSingleton<RoutinePage>();
		builder.Services.AddSingleton<TimetablePage>();
		builder.Services.AddSingleton<MindMapPage>();
		builder.Services.AddSingleton<SongwritingPage>();
		builder.Services.AddSingleton<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
