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
using System.CodeDom;
using Notifications.Wpf.Core;
using Serilog;
using KafkaTools.Logging;
using Serilog.Events;
using Microsoft.Extensions.Logging;

namespace KafkaTools
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            var hostBuilder = CreateHostBuilder();

            _host = hostBuilder.Build();
        }

        public IHostBuilder CreateHostBuilder()
        {
            // Initialize Serilog with an in-memory sink
            var logBufferSink = new CircularBufferSink(10);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Sink(logBufferSink)
                .CreateLogger();

            return Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((config) =>
                {
                    config.AddUserSecrets("301fdd9d-69f8-4441-90f8-7d83ddccf23d");
                })
                .ConfigureLogging((_hostBuilderContext, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog();
                })*/
                .ConfigureServices((context, serviceCollection) =>
                {
                    serviceCollection.AddSingleton<CircularBufferSink>(logBufferSink);

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
