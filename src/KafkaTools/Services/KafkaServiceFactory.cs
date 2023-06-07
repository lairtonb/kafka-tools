using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaTools.Services
{
    public class KafkaServiceFactory
    {
        private readonly IServiceProvider _services;

        public KafkaServiceFactory(IServiceProvider services)
        {
            _services = services;
        }

        public IKafkaService CreateKafkaService()
        {
            var kafkaService = _services.GetRequiredService<IKafkaService>();

            return kafkaService;
        }
    }
}
