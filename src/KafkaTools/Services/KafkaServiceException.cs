using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaTools.Services
{
    public class KafkaServiceException : Exception
    {
        public KafkaServiceException()
        {
        }

        public KafkaServiceException(string message)
            : base(message)
        {
        }

        public KafkaServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
