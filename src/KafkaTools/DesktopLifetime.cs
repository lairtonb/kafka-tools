using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace KafkaTools.Helpers
{
    internal class DesktopLifetime : IHostLifetime, IDisposable
    {
        private Window _window;

        public DesktopLifetime(/*IOptions<ConsoleLifetimeOptions> options, */ Window window /*, IHostingEnvironment environment, IApplicationLifetime applicationLifetime*/)
        {
            _window = window;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            _window.Show();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
