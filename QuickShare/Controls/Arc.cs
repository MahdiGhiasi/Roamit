using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

//From http://blog.jerrynixon.com/2012/06/windows-8-animated-pie-slice.html
//With modifications

namespace QuickShare.Controls
{
    public sealed class Arc : Path
    {
        private bool m_HasLoaded = false;
        public Arc()
        {
            Loaded += (s, e) =>
            {
                m_HasLoaded = true;
                UpdatePath();
            };
        }

        // StartAngle
        public static readonly DependencyProperty StartAngleProperty
            = DependencyProperty.Register("StartAngle", typeof(double), typeof(Arc),
            new PropertyMetadata(DependencyProperty.UnsetValue, (s, e) => { Changed(s as Arc); }));
        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        // Angle
        public static readonly DependencyProperty AngleProperty
            = DependencyProperty.Register("Angle", typeof(double), typeof(Arc),
            new PropertyMetadata(DependencyProperty.UnsetValue, (s, e) => { Changed(s as Arc); }));
        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        // Radius
        public static readonly DependencyProperty RadiusProperty
            = DependencyProperty.Register("Radius", typeof(double), typeof(Arc),
            new PropertyMetadata(DependencyProperty.UnsetValue, (s, e) => { Changed(s as Arc); }));
        public double Radius
        {
            get { return (double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        private static void Changed(Arc pieSlice)
        {
            if (pieSlice.m_HasLoaded)
                pieSlice.UpdatePath();
        }

        public void UpdatePath()
        {
            // ensure variables
            if (GetValue(StartAngleProperty) == DependencyProperty.UnsetValue)
                throw new ArgumentNullException("Start Angle is required");
            if (GetValue(RadiusProperty) == DependencyProperty.UnsetValue)
                throw new ArgumentNullException("Radius is required");
            if (GetValue(AngleProperty) == DependencyProperty.UnsetValue)
                throw new ArgumentNullException("Angle is required");

            Width = Height = 2 * (Radius + StrokeThickness);
            var _EndAngle = StartAngle + Angle;

            var thicknessMargin = StrokeThickness / 2.0;

            // path container
            var _StartX = Radius + Math.Sin(StartAngle * Math.PI / 180) * Radius + thicknessMargin;
            var _StartY = Radius - Math.Cos(StartAngle * Math.PI / 180) * Radius + thicknessMargin;
            var _StartP = new Point(_StartX, _StartY);
            var _Figure = new PathFigure
            {
                StartPoint = _StartP,
                IsClosed = false,
            };

            // outer arc
            var _ArcX = Radius + Math.Sin(_EndAngle * Math.PI / 180) * Radius + thicknessMargin;
            var _ArcY = Radius - Math.Cos(_EndAngle * Math.PI / 180) * Radius + thicknessMargin;
            var _ArcS = new Size(Radius, Radius);
            var _ArcP = new Point(_ArcX, _ArcY);
            var _Arc = new ArcSegment
            {
                IsLargeArc = Angle >= 180.0,
                Point = _ArcP,
                Size = _ArcS,
                SweepDirection = SweepDirection.Clockwise,
            };
            _Figure.Segments.Add(_Arc);

            // finalé
            Data = new PathGeometry { Figures = { _Figure } };
            InvalidateArrange();
        }

    }
}
