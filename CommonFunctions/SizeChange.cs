using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace QuickShare.Common
{
    //From http://stackoverflow.com/a/1481176/942659

    // Declare SizeChange class as a sub class of DependencyObject

    // because we need to register attached properties.
    public class SizeChange : DependencyObject
    {
        #region Attached property "IsEnabled"

        // The name of IsEnabled property.
        public const string IsEnabledPropertyName = "IsEnabled";

        // Register an attached property named "IsEnabled".
        // Note that OnIsEnabledChanged method is called when
        // the value of IsEnabled property is changed.
        public static readonly DependencyProperty IsEnabledProperty
             = DependencyProperty.RegisterAttached(
                 IsEnabledPropertyName,
                 typeof(bool),
                 typeof(SizeChange),
                 new PropertyMetadata(false, OnIsEnabledChanged));

        // Getter of IsEnabled property. The name of this method
        // should not be changed because the dependency system
        // uses it.
        public static bool GetIsEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsEnabledProperty);
        }

        // Setter of IsEnabled property. The name of this method
        // should not be changed because the dependency system
        // uses it.
        public static void SetIsEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsEnabledProperty, value);
        }

        #endregion

        #region Attached property "ActualHeight"

        // The name of ActualHeight property.
        public const string ActualHeightPropertyName = "ActualHeight";

        // Register an attached property named "ActualHeight".
        // The value of this property is updated When SizeChanged
        // event is raised.
        public static readonly DependencyProperty ActualHeightProperty
             = DependencyProperty.RegisterAttached(
                 ActualHeightPropertyName,
                 typeof(double),
                 typeof(SizeChange),
                 null);

        // Getter of ActualHeight property. The name of this method
        // should not be changed because the dependency system
        // uses it.
        public static double GetActualHeight(DependencyObject obj)
        {
            return (double)obj.GetValue(ActualHeightProperty);
        }

        // Setter of ActualHeight property. The name of this method
        // should not be changed because the dependency system
        // uses it.
        public static void SetActualHeight(DependencyObject obj, double value)
        {
            obj.SetValue(ActualHeightProperty, value);
        }

        #endregion

        #region Attached property "ActualWidth"

        // The name of ActualWidth property.
        public const string ActualWidthPropertyName = "ActualWidth";

        // Register an attached property named "ActualWidth".
        // The value of this property is updated When SizeChanged
        // event is raised.
        public static readonly DependencyProperty ActualWidthProperty
             = DependencyProperty.RegisterAttached(
                 ActualWidthPropertyName,
                 typeof(double),
                 typeof(SizeChange),
                 null);

        // Getter of ActualWidth property. The name of this method
        // should not be changed because the dependency system
        // uses it.
        public static double GetActualWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(ActualWidthProperty);
        }

        // Setter of ActualWidth property. The name of this method
        // should not be changed because the dependency system
        // uses it.
        public static void SetActualWidth(DependencyObject obj, double value)
        {
            obj.SetValue(ActualWidthProperty, value);
        }

        #endregion

        // This method is called when the value of IsEnabled property
        // is changed. If the new value is true, an event handler is
        // added to SizeChanged event of the target element.
        private static void OnIsEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            // The given object must be a FrameworkElement instance,
            // because we add an event handler to SizeChanged event
            // of it.
            var element = obj as FrameworkElement;

            if (element == null)
            {
                // The given object is not an instance of FrameworkElement,
                // meaning SizeChanged event is not available. So, nothing
                // can be done for the object.
                return;
            }

            // If IsEnabled=True
            if (args.NewValue != null && (bool)args.NewValue == true)
            {
                // Attach to the element.
                Attach(element);
            }
            else
            {
                // Detach from the element.
                Detach(element);
            }
        }

        private static void Attach(FrameworkElement element)
        {
            // Add an event handler to SizeChanged event of the element

            // to take action when actual size of the element changes.
            element.SizeChanged += HandleSizeChanged;
        }

        private static void Detach(FrameworkElement element)
        {
            // Remove the event handler from the element.
            element.SizeChanged -= HandleSizeChanged;
        }

        // An event handler invoked when SizeChanged event is raised.
        private static void HandleSizeChanged(object sender, SizeChangedEventArgs args)
        {
            var element = sender as FrameworkElement;

            if (element == null)
            {
                return;
            }

            // Get the new actual height and width.
            var width = args.NewSize.Width;
            var height = args.NewSize.Height;

            // Update values of SizeChange.ActualHeight and

            // SizeChange.ActualWidth.
            SetActualWidth(element, width);
            SetActualHeight(element, height);
        }
    }
}
