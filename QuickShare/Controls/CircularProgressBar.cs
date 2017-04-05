using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace QuickShare.Controls
{
    public sealed class CircularProgressBar : ProgressBar
    {
        Arc indicatorArc;
        TextBlock indicatorPercent;
        Storyboard storyboard;

        public CircularProgressBar()
        {
            this.DefaultStyleKey = typeof(CircularProgressBar);
            this.ValueChanged += CircularProgressBar_ValueChanged;
            this.SizeChanged += CircularProgressBar_SizeChanged;
        }

        double thickness = 2.0;
        public double Thickness
        {
            get
            {
                return thickness;
            }
            set
            {
                thickness = value;
                SetArcThickness();
            }
        }

        Brush stroke = null;
        public Brush Stroke
        {
            get
            {
                return stroke;
            }
            set
            {
                stroke = value;
                if (indicatorArc != null)
                    indicatorArc.Stroke = stroke;
            }
        }

        Visibility percentIndicatorVisibility;
        public Visibility PercentIndicatorVisibility
        {
            get
            {
                return percentIndicatorVisibility;
            }
            set
            {
                percentIndicatorVisibility = value;
                if (indicatorPercent != null)
                    indicatorPercent.Visibility = value;
            }
        }

        private void CircularProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (indicatorArc == null)
                return;

            SetArcRadius();
        }

        private void SetArcRadius()
        {
            indicatorArc.Radius = Math.Min(this.Width / 2, this.Height / 2) - thickness;
        }

        private void CircularProgressBar_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (indicatorArc == null)
                return;

            UpdateArcValues();
        }

        private void UpdateArcValues()
        {
            storyboard = new Storyboard();

            double _value = Value;
            if (_value < Minimum)
                _value = Minimum;

            if (_value >= Maximum)
                _value = Maximum - (Maximum - Minimum) / (1000.0 * Maximum);

            double currentAngle = indicatorArc.Angle;
            double newAngle = ((_value - Minimum) / (Maximum - Minimum)) * 360.0;

            DoubleAnimation da = new DoubleAnimation();
            da.From = currentAngle;
            da.To = newAngle;
            da.EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut };
            da.Duration = TimeSpan.FromMilliseconds(250);
            da.EnableDependentAnimation = true;

            Storyboard.SetTarget(da, indicatorArc);
            Storyboard.SetTargetProperty(da, "Angle");

            storyboard.Children.Add(da);
            storyboard.Begin();

            if (PercentIndicatorVisibility == Visibility.Visible)
                indicatorPercent.Text = ((int)Math.Round(((_value - Minimum) / (Maximum - Minimum)) * 100)).ToString() + "%";
        }

        private void SetArcThickness()
        {
            if (indicatorArc == null)
                return;

            indicatorArc.StrokeThickness = thickness;
            indicatorArc.Margin = new Thickness(thickness, thickness, 0, 0);
            SetArcRadius();
        }

        protected override void OnApplyTemplate()
        {
            indicatorArc = this.GetTemplateChild("indicatorArc") as Arc;
            indicatorPercent = this.GetTemplateChild("indicatorPercent") as TextBlock;

            indicatorPercent.Visibility = PercentIndicatorVisibility;

            UpdateArcValues();
            SetArcRadius();

            if (stroke != null)
                indicatorArc.Stroke = Stroke;
            indicatorArc.StrokeThickness = Thickness;


            base.OnApplyTemplate();
        }

    }
}
