using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaTools.Services
{
    public class KafkaServicesException : Exception
    {
        public KafkaServicesException()
        {
        }

        public KafkaServicesException(string message)
            : base(message)
        {
        }

        public KafkaServicesException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
