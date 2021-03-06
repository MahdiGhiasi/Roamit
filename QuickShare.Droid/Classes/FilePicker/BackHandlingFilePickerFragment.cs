﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using Com.Nononsenseapps.Filepicker;
using Java.IO;
using Java.Lang;

namespace QuickShare.Droid.Classes.FilePicker
{
    public class BackHandlingFilePickerFragment : FilePickerFragment
    {
        /**
          * For consistency, the top level the back button checks against should be the start path.
          * But it will fall back on /.
          */
        public File GetBackTop()
        {
            return GetPath(Arguments.GetString(KeyStartPath, "/"));
        }

        /**
         * @return true if the current path is the startpath or /
         */
        public bool IsBackTop()
        {
            return 0 == CompareFiles(MCurrentPath as File, GetBackTop()) ||
                    0 == CompareFiles(MCurrentPath as File, new File("/"));
        }

        /**
         * Go up on level, same as pressing on "..".
         */
        public override void GoUp()
        {
            var rootPath = GetParent(GetPath(Arguments.GetString(KeyStartPath, "/"))) as File;
            var newPath = GetParent(MCurrentPath) as File;

            //Block going further up than internal storage root.
            if (newPath.AbsolutePath == rootPath.AbsolutePath)
                return;

            MCurrentPath = newPath;
            MCheckedItems.Clear();
            MCheckedVisibleViewHolders.Clear();
            Refresh(MCurrentPath);
        }
    }
}