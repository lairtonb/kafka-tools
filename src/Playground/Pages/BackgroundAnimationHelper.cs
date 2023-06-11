using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows;

namespace Playground.Pages
{
    public static class BackgroundAnimationHelper
    {
        public static readonly DependencyProperty IsNewlyAddedProperty =
            DependencyProperty.RegisterAttached(
                "IsNewlyAdded",
                typeof(bool),
                typeof(BackgroundAnimationHelper),
                new FrameworkPropertyMetadata(false));

        public static bool GetIsNewlyAdded(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsNewlyAddedProperty);
        }

        public static void SetIsNewlyAdded(DependencyObject obj, bool value)
        {
            obj.SetValue(IsNewlyAddedProperty, value);
        }
    }

}
