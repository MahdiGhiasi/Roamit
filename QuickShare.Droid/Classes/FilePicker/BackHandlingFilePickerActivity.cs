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
using Com.Nononsenseapps.Filepicker;

namespace QuickShare.Droid.Classes.FilePicker
{
    public class BackHandlingFilePickerActivity : FilePickerActivity
    {
        BackHandlingFilePickerFragment currentFragment;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        protected override AbstractFilePickerFragment GetFragment(string p0, int p1, bool p2, bool p3, bool p4, bool p5)
        {
            // startPath is allowed to be null.
            // In that case, default folder should be SD-card and not "/"
            string path = (p0 ?? Android.OS.Environment.ExternalStorageDirectory.Path);

            currentFragment = new BackHandlingFilePickerFragment();
            currentFragment.SetArgs(p0, p1, p2, p3, p4, p5);
            return currentFragment;
        }

        public override void OnBackPressed()
        {
            if (currentFragment.IsBackTop())
                base.OnBackPressed();
            else
                currentFragment.GoUp();
        }
    }
}