using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

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

            this.Loaded += CircularProgressBar_Loaded;
        }

        private void CircularProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            CircularProgressBar_ValueChanged(sender, null);
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

        bool isIndeterminate = false;
        public new bool IsIndeterminate
        {
            get
            {
                return isIndeterminate;
            }
            set
            {
                isIndeterminate = value;
                CircularProgressBar_ValueChanged(this, null);
            }
        }

        TimeSpan valueChangeAnimationLength = TimeSpan.FromMilliseconds(250);
        public TimeSpan ValueChangeAnimationLength
        {
            get
            {
                return valueChangeAnimationLength;
            }
            set
            {
                valueChangeAnimationLength = value;
                CircularProgressBar_ValueChanged(this, null);
            }
        }

        TimeSpan indeterminateLoopAnimationLength = TimeSpan.FromMilliseconds(1000);
        public TimeSpan IndeterminateLoopAnimationLength
        {
            get
            {
                return indeterminateLoopAnimationLength;
            }
            set
            {
                indeterminateLoopAnimationLength = value;
                CircularProgressBar_ValueChanged(this, null);
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

            if (this.IsIndeterminate)
                UpdateArcAsIndeterminate();
            else
                UpdateArcValues();
        }

        private void UpdateArcAsIndeterminate()
        {
            storyboard = new Storyboard();

            double _value = 1.0 / 12.0;
            double currentAngle = indicatorArc.Angle;
            double newAngle = _value * 360.0;

            DoubleAnimation da1 = new DoubleAnimation()
            {
                From = currentAngle,
                To = newAngle,
                //EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut },
                Duration = ValueChangeAnimationLength,
                EnableDependentAnimation = true,
            };
            Storyboard.SetTarget(da1, indicatorArc);
            Storyboard.SetTargetProperty(da1, "Angle");

            DoubleAnimation da2 = new DoubleAnimation()
            {
                From = 0.0,
                To = 360.0,
                Duration = IndeterminateLoopAnimationLength,
                EnableDependentAnimation = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(da2, indicatorArc);
            Storyboard.SetTargetProperty(da2, "StartAngle");

            storyboard.Children.Add(da1);
            storyboard.Children.Add(da2);
            storyboard.Begin();

            if (PercentIndicatorVisibility == Visibility.Visible)
                indicatorPercent.Text = "";
        }

        private void UpdateArcValues()
        {
            storyboard = new Storyboard();

            double _value = Value;
            if (_value < Minimum)
                _value = Minimum;

            if ((_value >= Maximum) && (Maximum != 0))
                _value = Maximum - (Maximum - Minimum) / (1000.0 * Maximum);

            double currentAngle = indicatorArc.Angle;
            double newAngle = ((_value - Minimum) / (Maximum - Minimum)) * 360.0;

            if (Maximum == Minimum)
                newAngle = currentAngle;

            DoubleAnimation da1 = new DoubleAnimation()
            {
                From = currentAngle,
                To = newAngle,
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut },
                Duration = ValueChangeAnimationLength,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(da1, indicatorArc);
            Storyboard.SetTargetProperty(da1, "Angle");

            DoubleAnimation da2 = new DoubleAnimation()
            {
                To = indicatorArc.StartAngle > 180 ? 360 : 0,
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut },
                Duration = ValueChangeAnimationLength,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(da2, indicatorArc);
            Storyboard.SetTargetProperty(da2, "StartAngle");

            storyboard.Children.Add(da1);
            storyboard.Children.Add(da2);
            storyboard.Begin();

            if (PercentIndicatorVisibility == Visibility.Visible)
                indicatorPercent.Text = ((int)Math.Round(((_value - Minimum) / (Maximum - Minimum)) * 100)).ToString() + "%";
        }

        private void SetArcThickness()
        {
            if (indicatorArc == null)
                return;

            indicatorArc.StrokeThickness = thickness;
            SetArcRadius();
        }

        protected override void OnApplyTemplate()
        {
            indicatorArc = this.GetTemplateChild("indicatorArc") as Arc;
            indicatorPercent = this.GetTemplateChild("indicatorPercent") as TextBlock;

            indicatorPercent.Visibility = PercentIndicatorVisibility;

            UpdateArcValues();
            SetArcThickness();

            if (stroke != null)
                indicatorArc.Stroke = Stroke;
            indicatorArc.StrokeThickness = Thickness;


            base.OnApplyTemplate();
        }

    }
}
