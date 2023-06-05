using System;
using System.Runtime.Serialization;

namespace Microsoft.Extensions.Configuration
{
    [Serializable]
    internal class AppSettingsException : Exception
    {
        private string v1;
        private string v2;

        public AppSettingsException()
        {
        }

        public AppSettingsException(string? message) : base(message)
        {
        }

        public AppSettingsException(string v1, string v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }

        public AppSettingsException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected AppSettingsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}