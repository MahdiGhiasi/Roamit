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
using QuickShare.Droid.Classes;
using Newtonsoft.Json;

namespace QuickShare.Droid.Services
{
    [Service]
    public class WaiterService : Service
    {
        readonly TimeSpan _maxIdleLifeSpan = TimeSpan.FromMinutes(2);

        static readonly string TAG = "X:" + typeof(WaiterService).Name;
        static readonly int TimerWait = 4000;
        Timer timer;
        DateTime startTime;
        DateTime lastActiveTime;
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
            try
            {
                Log.Debug(TAG, $"OnStartCommand called at {startTime}, flags={flags}, startid={startId}");
                if (isStarted)
                {
                    TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
                    Log.Debug(TAG, $"This service was already started, it's been running for {runtime:c}.");
                }
                else
                {
                    MessageReceiveHelper.ClearEventRegistrations();
                    MessageReceiveHelper.Activity += MessageReceiveHelper_Activity;
                    MessageReceiveHelper.Finish += MessageReceiveHelper_Finish;
                    MessageReceiveHelper.Init(this);
                    
                }

                lastActiveTime = DateTime.UtcNow;

                string dataJson = intent.GetStringExtra("Data");
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataJson);
                await MessageReceiveHelper.ProcessReceivedMessage(data);
            }
            catch (Exception ex)
            {
                Log.Debug(TAG, "Unhandled exception occured: " + ex.ToString());
                StopSelf();
            }
        }

        private void MessageReceiveHelper_Finish()
        {
            Log.Debug(TAG, $"Service will shut down.");
            StopSelf();
        }

        private void MessageReceiveHelper_Activity()
        {
            lastActiveTime = DateTime.UtcNow;
        }
        
        public override void OnDestroy()
        {
            timer.Dispose();
            timer = null;
            isStarted = false;
            MessageReceiveHelper.ClearEventRegistrations();

            TimeSpan runtime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"Service destroyed at {DateTime.UtcNow} after running for {runtime:c}.");
            base.OnDestroy();
        }

        void HandleTimerCallback(object state)
        {
            TimeSpan runTime = DateTime.UtcNow.Subtract(startTime);
            Log.Debug(TAG, $"This service has been running for {runTime:c} (since ${state}).");

            TimeSpan timeElapsedSinceLastActivity = DateTime.UtcNow.Subtract(lastActiveTime);
            if (timeElapsedSinceLastActivity > _maxIdleLifeSpan)
            {
                Log.Debug(TAG, $"Service is idle for {timeElapsedSinceLastActivity:c}, will shut down.");
                StopSelf();
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            // This is a started service, not a bound service, so we just return null.
            return null;
        }
    }
}