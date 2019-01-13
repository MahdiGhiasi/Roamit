using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using QuickShare.Droid.Classes;

namespace QuickShare.Droid.Activities
{
    public class ThemeAwareActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            if (new Settings(this).Theme == AppTheme.Dark)
                SetTheme(Resource.Style.MyTheme_Dark);
            else
                SetTheme(Resource.Style.MyTheme);

            base.OnCreate(savedInstanceState);
        }
    }
}