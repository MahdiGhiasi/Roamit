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
using Microsoft.ConnectedDevices;
using Android.Webkit;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace QuickShare.Droid.RomeComponent
{
    public class RomeHelper : IDisposable
    {
        Dialog authDialog;
        WebView webView;
        Context appContext;
        RemoteSystemWatcher remoteSystemWatcher;

        public ObservableCollection<RemoteSystem> RemoteSystems { get; } = new ObservableCollection<RemoteSystem>();

        /// <summary>
        /// Note: Platform.FetchAuthCode event handler should be added before calling this method.
        /// </summary>
        internal async Task InitializeAsync(Context _appContext)
        {
            appContext = _appContext;

            //Assumed Platform.FetchAuthCode is already handled.
            var result = await Platform.InitializeAsync(_appContext, Secrets.CLIENT_ID);
            
            if (result == true)
            {
                System.Diagnostics.Debug.WriteLine("Initialized platform successfully");
                RefreshDevices();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ConnectedDevices Platform initialization failed");
            }
        }

        private void RefreshDevices()
        {
            remoteSystemWatcher = RemoteSystem.CreateWatcher();

            remoteSystemWatcher.RemoteSystemAdded += RemoteSystemWatcher_RemoteSystemAdded;
            remoteSystemWatcher.RemoteSystemRemoved += RemoteSystemWatcher_RemoteSystemRemoved;
            remoteSystemWatcher.RemoteSystemUpdated += RemoteSystemWatcher_RemoteSystemUpdated;

            remoteSystemWatcher.Start();
        }

        private void RemoteSystemWatcher_RemoteSystemAdded(RemoteSystemWatcher watcher, RemoteSystemAddedEventArgs args)
        {
            var remoteSystem = args.P0;

            var existingRs = RemoteSystems.FirstOrDefault(x => x.Id == remoteSystem.Id);
            if (existingRs != null)
                RemoteSystems.Remove(existingRs);

            AddToRemoteSystemsList(remoteSystem);
        }

        private void RemoteSystemWatcher_RemoteSystemRemoved(RemoteSystemWatcher watcher, RemoteSystemRemovedEventArgs args)
        {
            var remoteSystem = RemoteSystems.Where(s => s.Id == args.P0).FirstOrDefault();
            if (remoteSystem != null)
            {
                RemoteSystems.Remove(remoteSystem);
            }
        }

        private void RemoteSystemWatcher_RemoteSystemUpdated(RemoteSystemWatcher watcher, RemoteSystemUpdatedEventArgs args)
        {
            RemoteSystem remoteSystem = null;
            remoteSystem = RemoteSystems.Where(s => s.Id == args.P0.Id).FirstOrDefault();
            if (remoteSystem != null)
                RemoteSystems.Remove(remoteSystem);
            AddToRemoteSystemsList(args.P0);
        }

        public void Dispose()
        {
            if (remoteSystemWatcher != null)
            {
                remoteSystemWatcher.RemoteSystemAdded -= RemoteSystemWatcher_RemoteSystemAdded;
                remoteSystemWatcher.RemoteSystemRemoved -= RemoteSystemWatcher_RemoteSystemRemoved;
                remoteSystemWatcher.RemoteSystemUpdated -= RemoteSystemWatcher_RemoteSystemUpdated;
                remoteSystemWatcher.Stop();
                remoteSystemWatcher = null;
            }
        }

        public void AddToRemoteSystemsList(RemoteSystem r)
        {
            int min = 0, max = RemoteSystems.Count;

            if (r.IsAvailableByProximity)
            {
                while (((max - 1) >= 0) && (!RemoteSystems[max - 1].IsAvailableByProximity))
                {
                    max--;
                }
            }
            else
            {
                while ((min < RemoteSystems.Count) && (RemoteSystems[min].IsAvailableByProximity))
                {
                    min++;
                }
            }

            int i;
            for (i = min; i < max; i++)
            {
                if (string.Compare(RemoteSystems[i].DisplayName, r.DisplayName) >= 0)
                    break;
            }
            RemoteSystems.Insert(i, r);
        }
    }
}
