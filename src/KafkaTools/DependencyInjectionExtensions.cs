using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaTools
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection RegisterMainWindow<T>(this IServiceCollection services)
        {
            return services.AddSingleton(typeof(Window), typeof(T));
        }
    }
}
