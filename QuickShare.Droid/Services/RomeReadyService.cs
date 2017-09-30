#define ROMEREADY_TIMER1

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
using System.Threading;
using Android.Util;
using QuickShare.Droid.RomeComponent;
using System.Threading.Tasks;
using Android.Media;
using Android.Support.V4.App;

namespace QuickShare.Droid.Services
{
    [Service]
    public class RomeReadyService : Service
    {
        static readonly string TAG = "X:" + typeof(RomeReadyService).Name;

#if ROMEREADY_TIMER
        static readonly int TimerWait = 4000;
        Timer timer;
#endif
        DateTime startTime;
        bool isStarted = false;

        public override void OnCreate()
        {
            base.OnCreate();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            InitService(intent, flags, startId);
            return StartCommandResult.Sticky;
        }

        private async void InitService(Intent intent, StartCommandFlags flags, int startId)
        {
            Log.Debug(TAG, $"OnStartCommand called at {DateTime.Now}, flags={flags}, startid={startId}"); 
            if (isStarted)
            {
                TimeSpan runtime = DateTime.Now.Subtract(startTime);
                Log.Debug(TAG, $"This service was already started, it's been running for {runtime:c}.");
            }
            else
            {
                isStarted = true;
                startTime = DateTime.Now;
                Log.Debug(TAG, $"Starting the service, at {startTime}.");
#if ROMEREADY_TIMER
                timer = new Timer(HandleTimerCallback, startTime, 0, TimerWait);
#endif
            }

            await InitRome();
        }

        private async Task InitRome()
        {
            if (Common.PackageManager == null)
            {
                Common.PackageManager = new RomePackageManager(this);
                Common.PackageManager.Initialize("com.roamit.service");

                await Common.PackageManager.InitializeDiscovery();
            }

            if (Common.MessageCarrierPackageManager == null)
            {
                Common.MessageCarrierPackageManager = new RomePackageManager(this);
                Common.MessageCarrierPackageManager.Initialize("com.roamit.messagecarrierservice");

                await Common.MessageCarrierPackageManager.InitializeDiscovery();
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            // This is a started service, not a bound service, so we just return null.
            return null;
        }

        public override void OnDestroy()
        {
#if ROMEREADY_TIMER
            timer.Dispose();
            timer = null;
#endif
            isStarted = false;

            TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"Service destroyed at {DateTime.UtcNow} after running for {runtime:c}.");
            base.OnDestroy();
        }

#if ROMEREADY_TIMER
        void HandleTimerCallback(object state)
        {
            TimeSpan runTime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"This service has been running for {runTime:c} (since ${state}).");
        }
#endif

    }
}