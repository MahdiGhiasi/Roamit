using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using QuickShare.HelperClasses;
using QuickShare.Common;

namespace QuickShare
{
    public sealed partial class Intro : Page
    {
        TextBlock[] indicators;

        public Intro()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DeviceInfo.RefreshFormFactorType();
            if ((DeviceInfo.SystemVersion > DeviceInfo.CreatorsUpdate) && (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Desktop))
            {
                WindowTopBarFunctions.ApplyAcrylic();
                TitleBarStackPanel.Visibility = Visibility.Visible;
            }

            HideFlipViewButton("PreviousButtonHorizontal");
            HideFlipViewButton("NextButtonHorizontal");

            string dot = Convert.ToChar(8226).ToString();
            indicators = new TextBlock[flipView.Items.Count];
            for (int i = 0; i < flipView.Items.Count; i++)
            {
                indicators[i] = new TextBlock()
                {
                    Margin = new Thickness(2, 0, 2, 0),
                    Text = dot,
                    Opacity = (i == 0) ? 1 : 0.5,
                };
                PageIndicators.Children.Add(indicators[i]);
            }
        }

        private void HideFlipViewButton(string name)
        {
            var b = VisualChildFinder.FindVisualChild<Button>(flipView, name);
            b.Opacity = 0;
            b.IsHitTestVisible = false;
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SkipButton == null) || (NextButton == null))
                return;

            for (int i = 0; i < flipView.Items.Count; i++)
            {
                if (i <= flipView.SelectedIndex)
                    indicators[i].Opacity = 1;
                else
                    indicators[i].Opacity = 0.5;
            }

            if (flipView.SelectedIndex == 0)
            {
                SkipButton.Visibility = Visibility.Visible;
                PrevButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                SkipButton.Visibility = Visibility.Collapsed;
                PrevButton.Visibility = Visibility.Visible;
            }


            if (flipView.SelectedIndex == flipView.Items.Count - 1)
                NextButton.Visibility = Visibility.Collapsed;
            else
                NextButton.Visibility = Visibility.Visible;
        }

        private void NextButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (flipView.SelectedIndex == flipView.Items.Count - 1)
                return;

            flipView.SelectedIndex++;
        }

        private void PrevButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (flipView.SelectedIndex == 0)
                return;

            flipView.SelectedIndex--;
        }

        private void FinishButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FinishIntro();
        }

        private void SkipButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FinishIntro();
        }

        private void FinishIntro()
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["FirstRun"] = "false";
            Frame.Navigate(typeof(MainPage));
        }
    }
}
