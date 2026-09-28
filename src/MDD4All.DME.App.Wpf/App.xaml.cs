using MDD4All.AssemblyLoading.Contracts;
using MDD4All.DME.AssemblyLoading;
using MDD4All.DME.DataAccess.DataFiles;
using MDD4All.DME.DataAccess.Serialization;
using MDD4All.DME.ViewModels.DataManager;
using MDD4All.DME.ViewModels.Editor.Settings;
using Synorvia.FileAccess.Contracts;
using Synorvia.FileAccess.WPF;
using MDD4All.Localization;
using MDD4All.Localization.Contracts;
using Synorvia.UI.BlazorComponents.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;

namespace MDD4All.DME.App.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost Host { get; private set; } = null!;

        

        public App()
        {
        }

        

        private void OnStartup(object sender, StartupEventArgs e)
        {
            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder(e.Args)
                .ConfigureServices((context, services) =>
                {
                    // Services
                    services.AddSingleton<MainWindow>();


                    // Blazor Hybrid
                    services.AddWpfBlazorWebView();

                    services.AddBlazorWebViewDeveloperTools();

                    services.AddLocalization(options =>
                    {

                        options.ResourcesPath = "Resources";
                    });


                    // Supported Cultures definieren
                    CultureInfo[] supportedCultures = new[]
                    {
                        new CultureInfo("de-DE"),
                        new CultureInfo("en-US")
                    };

                    // Optionen erstellen
                    RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions
                    {
                        DefaultRequestCulture = new RequestCulture("en-US"),
                        SupportedCultures = supportedCultures,
                        SupportedUICultures = supportedCultures
                    };

                    // Registered optionally, so it is available for DI
                    services.Configure<RequestLocalizationOptions>(options =>
                    {
                        options.DefaultRequestCulture = localizationOptions.DefaultRequestCulture;
                        options.SupportedCultures = supportedCultures;
                        options.SupportedUICultures = supportedCultures;
                    });


                    services.AddScoped<DragDropDataProvider>();

                    // Start in the language that was picked last time. An entry the app no
                    // longer knows must not keep it from starting.
                    services.AddSingleton<ILanguageSetter>(provider =>
                    {
                        DataManagerSettingsViewModel settings =
                            provider.GetRequiredService<DataManagerSettingsViewModel>();

                        CultureInfo storedCulture = supportedCultures[0];

                        foreach (CultureInfo cultureInfo in supportedCultures)
                        {
                            if (cultureInfo.Name == settings.DesiredLanguage)
                            {
                                storedCulture = cultureInfo;
                                break;
                            }
                        }

                        return new LanguageSetter(new List<CultureInfo>(supportedCultures),
                                                 storedCulture);
                    });

                    // Registered after AddLocalization on purpose: the last registration of a
                    // service type wins, so every IStringLocalizer<T> in the app - including the
                    // ones in components we do not own - now reads the picked language instead
                    // of CultureInfo.CurrentUICulture, which this host keeps out of reach.
                    services.AddSingleton<IStringLocalizerFactory>(provider =>
                        new LanguageSetterStringLocalizerFactory(
                            provider.GetRequiredService<ILanguageSetter>(), "Resources"));

                    services.AddSingleton<IFileLoader, WpfFileLoader>();
                    services.AddSingleton<IFileSaver, WpfFileSaver>();
                    services.AddTransient<IAssemblyProvider>(provider => {
                        AssemblyProvider assemblyProvider = new AssemblyProvider();
                        assemblyProvider.ProxiesDllPath = Path.Combine(AppContext.BaseDirectory, "MDD4All.DME.Proxies.dll");
                        return assemblyProvider;
                    });
                    services.AddSingleton<DataSerializer>();
                    services.AddSingleton<DataFileProvider>();
                    services.AddSingleton<DictionaryKeyAnalyzer>();
                    services.AddSingleton<DataManagerObjectViewModel>();
                    services.AddSingleton<DataManagerSettingsViewModel>();
                    services.AddSingleton<DataManagerModelViewModel>();
                    services.AddSingleton<DataManagerFileViewModel>();
                    services.AddSingleton<MDD4All.DME.Views.Localization.AppTextProvider>();
                    services.AddSingleton<MDD4All.DME.Views.Localization.ValidationTextProvider>();
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<EditorViewModel>();
                    services.AddSingleton<EditorAppearanceSettingsViewModel>();
                    services.AddSingleton<ExplorerSettingsViewModel>();
                })
                .Build();

            Host.Start();

            var mainWindow = Host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Host?.Dispose();
            base.OnExit(e);
        }

    }
}
