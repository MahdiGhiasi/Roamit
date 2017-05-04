using Android.App;
using Android.Widget;
using Android.OS;
using System;
using System.Collections.ObjectModel;
using QuickShare.Droid.RomeComponent;
using System.Collections.Generic;
using QuickShare.DevicesListManager;
using Microsoft.ConnectedDevices;
using Android.Webkit;

namespace QuickShare.Droid
{
    [Activity(Label = "QuickShare.Droid", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        RomePackageManager packageManager = new RomePackageManager();
        DevicesListManager.DevicesListManager listManager = new DevicesListManager.DevicesListManager("", new RemoteSystemNormalizer());

        DevicesListAdapter devicesAdapter;

        private WebView _webView;
        internal Dialog _authDialog;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView (Resource.Layout.Main);

            packageManager.RemoteSystems.CollectionChanged += RemoteSystems_CollectionChanged;

            devicesAdapter = new DevicesListAdapter(this, listManager);
            FindViewById<ListView>(Resource.Id.listView1).Adapter = devicesAdapter;

            InitDiscovery();
        }

        private async void InitDiscovery()
        {
            Platform.FetchAuthCode += Platform_FetchAuthCode;
            await packageManager.InitializeDiscovery(this);
        }

        private void RemoteSystems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                {
                    listManager.AddDevice(item);
                }

            if (e.OldItems != null)
                foreach (var item in e.OldItems)
                {
                    listManager.RemoveDevice(item);
                }
        }

        private void Platform_FetchAuthCode(string oauthUrl)
        {
            _authDialog = new Dialog(this);

            var linearLayout = new LinearLayout(_authDialog.Context);
            _webView = new WebView(_authDialog.Context);
            linearLayout.AddView(_webView);
            _authDialog.SetContentView(linearLayout);

            _webView.SetWebChromeClient(new WebChromeClient());
            _webView.Settings.JavaScriptEnabled = true;
            _webView.Settings.DomStorageEnabled = true;
            _webView.LoadUrl(oauthUrl);

            _webView.SetWebViewClient(new MsaWebViewClient(this));
            _authDialog.Show();
            _authDialog.SetCancelable(true);
        }
    }
}

