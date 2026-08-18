using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Maui.Handlers;
using Mopups.Hosting;
using PesaScope.App.Data;
using PesaScope.App.Data.Repositories;
using PesaScope.App.Data.Repositories.Interfaces;
using PesaScope.App.Services;
using PesaScope.App.Services.Interfaces;
using PesaScope.App.ViewModels;
using PesaScope.App.Views.Budgets;
using PesaScope.App.Views.Categories;
using PesaScope.App.Views.Dashboard;
using PesaScope.App.Views.Onboarding;
using PesaScope.App.Views.Security;
using PesaScope.App.Views.Settings;
using PesaScope.App.Views.Transactions;
using PesaScope.Core.Services;
using PesaScope.Core.Services.Interfaces;
using Plugin.LocalNotification;
using Plugin.Maui.Biometric;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UraniumUI;

namespace PesaScope.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseLiveCharts()
                .ConfigureMopups()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .UseUraniumUIBlurs()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");

                    fonts.AddMaterialSymbolsFonts();
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler(typeof(Shell), typeof(PesaScope.App.Platforms.Android.PesaLensShellHandler));
#endif
                });
            builder.Services.AddMopupsDialogs();

            ConfigureHandlers();

            // Onboarding pages — transient because they are created once and discarded
            builder.Services.AddTransient<WelcomePage>();
            builder.Services.AddTransient<PermissionPage>();
            builder.Services.AddTransient<ImportProgressPage>();
            builder.Services.AddTransient<TransactionDetailPage>();
            builder.Services.AddTransient<BudgetHistoryPage>();
            builder.Services.AddTransient<ExportPage>();

            // App pages - Registered as Singleton to avoid recreation on every tab switch
            builder.Services.AddSingleton<AppLockPage>();
            builder.Services.AddSingleton<DashboardPage>();
            builder.Services.AddSingleton<TransactionsPage>();
            builder.Services.AddSingleton<CategoriesPage>();
            builder.Services.AddSingleton<BudgetsPage>();
            builder.Services.AddSingleton<SettingsPage>();

            // View models
            builder.Services.AddSingleton<DashboardViewModel>();
            builder.Services.AddSingleton<SettingsViewModel>();
            builder.Services.AddSingleton<TransactionsViewModel>();
            builder.Services.AddSingleton<CategoriesViewModel>();
            builder.Services.AddSingleton<BudgetsViewModel>();

            // It's page will get pushed and popped after use
            builder.Services.AddTransient<TransactionDetailViewModel>();
            builder.Services.AddTransient<BudgetHistoryViewModel>();
            builder.Services.AddTransient<ExportViewModel>();

            // DatabaseService registered as singleton — App.cs resolves and inits it
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pesascope.db");
            builder.Services.AddSingleton(new DatabaseService(dbPath));
            builder.Services.AddSingleton<IBiometricAuthService, BiometricAuthService>();
            builder.Services.AddSingleton<ITransactionRepository, TransactionRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
            builder.Services.AddSingleton<IAutoCategorizationRuleRepository, AutoCategorizationRuleRepository>();
            builder.Services.AddSingleton<IBudgetRepository, BudgetRepository>();
            builder.Services.AddSingleton<IOverallBudgetRepository, OverallBudgetRepository>();
            builder.Services.AddSingleton<ISyncMetadataRepository, SyncMetadataRepository>();
            builder.Services.AddSingleton<IAppSettingsRepository, AppSettingsRepository>();
            builder.Services.AddSingleton<IExportHistoryRepository, ExportHistoryRepository>();
            builder.Services.AddSingleton<IBudgetSnapshotRepository,BudgetSnapshotRepository>();
            builder.Services.AddSingleton<ISmsReaderService,SmsReaderService>();
            builder.Services.AddSingleton<IMpesaSmsParser,MpesaSmsParser>();
            builder.Services.AddSingleton<IAutoCategorizationService, AutoCategorizationService>();
            builder.Services.AddSingleton<IBudgetNotificationService, BudgetNotificationService>();
            builder.Services.AddSingleton<IBudgetSnapshotService,BudgetSnapshotService>();
            builder.Services.AddSingleton<IReportExportService,ReportExportService>();
            builder.Services.AddSingleton<DatabaseSeeder>();

            // Register biometric service
            builder.Services.AddSingleton<IBiometric>(BiometricAuthenticationService.Default);

            // Set questpdf licence
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var app = builder.Build();
            ServiceLocator.Initialize(app.Services);

            return app;
        }

        // To globally override android's behavior in maui of displaying underline on Entry, Picker, DatePickerHandler and Editor controls
        private static void ConfigureHandlers()
        {
#if ANDROID
            PickerHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, view) =>
            {
                handler.PlatformView.Background = null;
            });

            EntryHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, view) =>
            {
                handler.PlatformView.Background = null;
            });

            EditorHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, view) =>
            {
                handler.PlatformView.Background = null;
            });

            DatePickerHandler.Mapper.AppendToMapping("RemoveUnderline", (handler, view) =>
            {
                handler.PlatformView.Background = null;
            });
#endif
        }
    }
}
