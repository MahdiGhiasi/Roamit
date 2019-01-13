using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V4.View;
using QuickShare.Droid.OnlineServiceHelpers;
using Android.Webkit;
using QuickShare.Droid.Classes;

namespace QuickShare.Droid.Activities
{
    [Activity(Label = "Roamit", Icon = "@drawable/icon", Name = "com.ghiasi.quickshare.intro")]
    [IntentFilter(new[] { Intent.ActionSend, Intent.ActionSendMultiple }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*", Label = "Roamit")]
    public class IntroActivity : Activity
    {
        private ViewPager viewPager;
        private IntroViewPagerAdapter introViewPagerAdapter;
        private ViewPagerPageChangeListener viewPagerPageChangeListener;

        private Button btnSkip, btnNext, btnSignIn, btnAuthorize;
        private TextView dots;
        private ProgressBar signInSpinner;
        private List<int> layouts;

        private static IntroActivity Instance;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestWindowFeature(WindowFeatures.NoTitle);
            base.OnCreate(savedInstanceState);

            if (MSAAuthenticator.HasUserUniqueId() || CloudServiceAuthenticationHelper.IsAuthenticatedForApiV3())
            {
                LaunchHomeScreen();
                return;
            }

            (new Classes.WhatsNew(this)).Shown(); //Don't show what's new dialog if this is the first time user opens the app.

            Instance = this;

            SetTheme(Resource.Style.MyTheme_Dark);
            Window.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window.SetNavigationBarColor(Android.Graphics.Color.Black);
                Window.DecorView.SystemUiVisibility = StatusBarVisibility.Hidden;
            }

            SetContentView(Resource.Layout.Intro);

            viewPager = FindViewById<ViewPager>(Resource.Id.intro_viewPager);
            dots = FindViewById<TextView>(Resource.Id.intro_dotsText);
            btnNext = FindViewById<Button>(Resource.Id.intro_btnNext);
            btnSkip = FindViewById<Button>(Resource.Id.intro_btnSkip);

            layouts = new List<int>()
            {
                Resource.Layout.Intro1,
                Resource.Layout.Intro2,
                Resource.Layout.Intro3,
                Resource.Layout.Intro4,
                Resource.Layout.Intro4_1,
                Resource.Layout.Intro5,
            };

            AddBottomDots(0);

            introViewPagerAdapter = new IntroViewPagerAdapter(this);
            viewPager.Adapter = introViewPagerAdapter;
            viewPagerPageChangeListener = new ViewPagerPageChangeListener();
            viewPager.AddOnPageChangeListener(viewPagerPageChangeListener);

            OSHelper.ClearWebViewCache(ApplicationContext);

            btnNext.Click += BtnNext_Click;
            btnSkip.Click += BtnSkip_Click;
        }

        private void BtnSkip_Click(object sender, EventArgs e)
        {
            viewPager.CurrentItem = 5;
        }

        private void SetUpPageEvents(View view)
        {
            var _btnSignIn = view.FindViewById<Button>(Resource.Id.intro_signInButton);
            if (_btnSignIn != null)
            {
                btnSignIn = _btnSignIn;
                btnSignIn.Click -= BtnSignIn_Click;
                btnSignIn.Click += BtnSignIn_Click;
            }

            var _btnAuthorize = view.FindViewById<Button>(Resource.Id.intro_authorizeButton);
            if (_btnAuthorize != null)
            {
                btnAuthorize = _btnAuthorize;
                btnAuthorize.Click -= BtnAuthorize_Click;
                btnAuthorize.Click += BtnAuthorize_Click;
            }

            var _signInSpinner = view.FindViewById<ProgressBar>(Resource.Id.intro_signInSpinner);
            if (_signInSpinner != null)
            {
                signInSpinner = _signInSpinner;
            }
        }

        private void BtnAuthorize_Click(object sender, EventArgs e)
        {
            LaunchHomeScreen();
        }

        private async void BtnSignIn_Click(object sender, EventArgs e)
        {
            ((Button)sender).Enabled = false;
            signInSpinner.Visibility = ViewStates.Visible;

            var result = await AuthenticateDialog.ShowAsync(this, MsaAuthPurpose.App);
            if (result == MsaAuthResult.Success)
            {
                layouts.Add(Resource.Layout.Intro6);
                UpdateViewPagerLayout();

                GoToNextPage();
            }
            else
            {
                AlertDialog.Builder alert = new AlertDialog.Builder(this);
                alert.SetTitle(result.ToString() + "\n" + "Please try again later.");
                alert.SetPositiveButton("OK", (senderAlert, args) => { });
                RunOnUiThread(() => {
                    alert.Show();
                });

                MSAAuthenticator.DeleteUserUniqueId();
                ((Button)sender).Enabled = true;
            }
            signInSpinner.Visibility = ViewStates.Gone;
        }

        private void UpdateViewPagerLayout()
        {
            introViewPagerAdapter.NotifyDataSetChanged();
            viewPagerPageChangeListener.OnPageSelected(viewPager.CurrentItem);
        }

        private void LaunchHomeScreen()
        {
            //Classes.Settings settings = new Classes.Settings(this);

            if ((Intent.Action == Intent.ActionSend) || (Intent.Action == Intent.ActionSendMultiple))
            {
                //if (settings.UseLegacyUI)
                //    Intent.SetClass(this, typeof(MainActivity));
                //else
                Intent.SetClass(this, typeof(WebViewContainerActivity));

                StartActivity(Intent);
            }
            else
            {
                //if (settings.UseLegacyUI)
                //    StartActivity(new Intent(this, typeof(MainActivity)));
                //else
                StartActivity(new Intent(this, typeof(WebViewContainerActivity)));
            }
            Finish();
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            GoToNextPage();
        }

        private void GoToNextPage()
        {
            int p = viewPager.CurrentItem + 1;
            if (p < layouts.Count)
                viewPager.CurrentItem = p;
        }

        private void AddBottomDots(int currentPage)
        {
            string pageDot = Convert.ToChar(8226).ToString();
            string text = "";

            for (int i = 0; i < layouts.Count; i++)
            {
                if (i <= currentPage)
                    text += $"<font color='white'>{pageDot}</font> ";
                else
                    text += $"<font color='gray'>{pageDot}</font> ";
            }

            dots.SetText(Android.Text.Html.FromHtml(text.Trim()), TextView.BufferType.Spannable);
        }

        internal class IntroViewPagerAdapter : PagerAdapter
        {
            private LayoutInflater layoutInflater;
            private Context context;

            public override int Count => Instance.layouts.Count;

            public IntroViewPagerAdapter(Context _context)
            {
                context = _context;
            }

            public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
            {
                layoutInflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);

                var view = layoutInflater.Inflate(Instance.layouts[position], container, false);
                container.AddView(view);

                Instance.SetUpPageEvents(view);

                return view;
            }

            public override bool IsViewFromObject(View view, Java.Lang.Object @object)
            {
                return view == @object;
            }

            public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object @object)
            {
                var view = (View)@object;
                container.RemoveView(view);
            }
        }

        internal class ViewPagerPageChangeListener : Java.Lang.Object, ViewPager.IOnPageChangeListener
        {           
            public void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
            {

            }

            public void OnPageScrollStateChanged(int state)
            {

            }

            public void OnPageSelected(int position)
            {
                Instance.AddBottomDots(position);

                if (position == Instance.layouts.Count - 1)
                    Instance.btnNext.Visibility = ViewStates.Gone;
                else
                    Instance.btnNext.Visibility = ViewStates.Visible;

                if (position == 0)
                    Instance.btnSkip.Visibility = ViewStates.Visible;
                else
                    Instance.btnSkip.Visibility = ViewStates.Gone;
            }
        }
    }
}