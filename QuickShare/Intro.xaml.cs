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
using QuickShare.Classes;
using QuickShare.Common;
using System.Threading.Tasks;
using QuickShare.HelperClasses;
using Windows.System;
using Windows.UI.Popups;

namespace QuickShare
{
    public sealed partial class Intro : Page
    {
        TextBlock[] indicators;

        public Intro()
        {
            this.InitializeComponent();

            AppIconImage.Opacity = 0;
            WelcomeTitle.Opacity = 0;
            WelcomeText.Opacity = 0;
            flipView.Opacity = 0;
            PageIndicators.Opacity = 0;

            if (PCExtensionHelper.IsSupported)
            {
                flipView.Items.RemoveAt(flipView.Items.Count - 1);
            }

            flipView.SelectedIndex = 0; // Just in case
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            startStoryboard.Begin();

            DeviceInfo.RefreshFormFactorType();
            if ((DeviceInfo.SystemVersion > DeviceInfo.CreatorsUpdate) && (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Desktop))
            {
                WindowTopBarFunctions.ApplyAcrylic();
                TitleBarStackPanel.Visibility = Visibility.Visible;
            }


            ActivatePCExtensionPrompt.Visibility = PCExtensionHelper.IsSupported ? Visibility.Visible : Visibility.Collapsed;


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

            WhatsNewHelper.InitIntro();
        }

        private void HideFlipViewButton(string name)
        {
            var b = VisualChildFinder.FindVisualChild<Button>(flipView, name);
            b.Opacity = 0;
            b.IsHitTestVisible = false;
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((SkipButton == null) || (NextButton == null) || (indicators == null))
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
            {
                nextHideStoryboard.Begin();
            }
            else
            {
                nextShowStoryboard.Begin();
            }
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
            if (PCExtensionHelper.IsSupported)
                flipView.SelectedIndex = flipView.Items.Count - 1;
            else
                FinishIntro();
        }

        private async void FinishIntro()
        {
            progressRing.Visibility = Visibility.Visible;
            progressRing.IsActive = true;
            endStoryboard.Begin();
            
            await Task.Delay(TimeSpan.FromSeconds(0.1));

            Windows.Storage.ApplicationData.Current.LocalSettings.Values["FirstRun"] = "false";
            Frame.Navigate(typeof(MainPage));
        }

        private async void ActivePCExtensionButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            progressRing.Visibility = Visibility.Visible;
            progressRing.IsActive = true;
            endStoryboard.Begin();

            Windows.Storage.ApplicationData.Current.LocalSettings.Values["SendCloudClipboard"] = true;

            App.PCExtensionLoginFailed += PCExtension_LoginFailed;

            await PCExtensionHelper.StartPCExtension();

            await Task.Delay(TimeSpan.FromSeconds(2));

            Windows.Storage.ApplicationData.Current.LocalSettings.Values["FirstRun"] = "false";
            Frame.Navigate(typeof(MainPage));
        }

        private async void PCExtension_LoginFailed(object sender, EventArgs e)
        {
            await (new MessageDialog("You can enable it later from settings.", "Universal clipboard setup cancelled.")).ShowAsync();

            App.PCExtensionLoginFailed -= PCExtension_LoginFailed;
        }
    }
}
