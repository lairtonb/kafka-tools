using KafkaTools.Helpers;
using KafkaTools.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using KafkaTools.Common;
using System.CodeDom;
using Notifications.Wpf.Core;

namespace KafkaTools
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly IHost _host;

        IServiceProvider ServiceProvider { get; set; }

        public App()
        {
            var hostBuilder = CreateHostBuilder();

            _host = hostBuilder.Build();
        }

        public IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((config) =>
                {
                    config.AddUserSecrets("301fdd9d-69f8-4441-90f8-7d83ddccf23d");
                })
                .ConfigureServices((context, serviceCollection) =>
                {
                    // serviceCollection.AddSingleton(typeof(Window), 
                    // serviceCollection.AddSingleton<MainWindow>();
                    // serviceCollection.RegisterMainWindow<MainWindow>();
                    // serviceCollection.AddSingleton(typeof(Window), typeof(MainWindow));

                    serviceCollection.AddSingleton<INotificationManager, NotificationManager>();
                    serviceCollection.AddMemoryCache();
                    serviceCollection.AddSingleton<KafkaServices>();

                    serviceCollection.AddSingleton<MainWindow>();
                    serviceCollection.AddSingleton<Window>(serviceProvider => serviceProvider.GetRequiredService<MainWindow>());
                    serviceCollection.AddSingleton<IHostLifetime, DesktopLifetime>();
                });
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync();
            }
        }
    }
}
