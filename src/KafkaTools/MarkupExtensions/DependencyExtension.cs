using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace KafkaTools.MarkupExtensions
{
    /*
     * Usage:
     * DataContext="{ext:TypeLocator {x:Type cil:IViewModel}}"
     *
     */

    public class TypeLocatorExtension : MarkupExtension
    {
        private readonly Type _type;

        public TypeLocatorExtension(Type type)
        {
            _type = type;
        }

        // ProvideValue, which returns an object instance from the container  
        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (_type == null)
                throw new NullReferenceException(nameof(Type));

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return null;

            try
            {
                return Resolver?.Invoke(_type);
            }
            catch (Exception ex)
            {
                // var services = IoC.Instance.GetInstance<IMessageService>();
                // services.Error("Instance error", "{0}\n{1}", ex.Message, ex.InnerException == null ? null : ex.InnerException.Message);
                return null;
            }
        }

        public static Func<Type, object?>? Resolver { get; set; }
    }
}
