using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaTools
{
    public class CustomServiceProviderFactory : IServiceProviderFactory<ServiceCollection>
    {
        public CustomServiceProviderFactory(ServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        private ServiceCollection ServiceCollection { get; }

        public ServiceCollection CreateBuilder(IServiceCollection services)
        {
            //foreach (var service in services)
                // ServiceCollection.Add(service)
            return ServiceCollection;
        }

        public IServiceProvider CreateServiceProvider(ServiceCollection containerBuilder)
        {
            return containerBuilder.BuildServiceProvider();
        }
    }
}
