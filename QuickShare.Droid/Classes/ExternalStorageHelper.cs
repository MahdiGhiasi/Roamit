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
using Java.IO;

namespace QuickShare.Droid.Classes
{
    class ExternalStorageHelper
    {
        private static string[] GetExternalSdCardPaths()
        {
            List<string> paths = new List<string>();
            foreach (File file in Application.Context.GetExternalFilesDirs("external"))
            {
                if (file != null && !file.Equals(Application.Context.GetExternalFilesDir("external")))
                {
                    int index = file.AbsolutePath.LastIndexOf("/Android/data");
                    if (index < 0)
                    {
                        //Log.w(Application.TAG, "Unexpected external file dir: " + file.getAbsolutePath());
                        continue;
                    }
                    else
                    {
                        String path = file.AbsolutePath.Substring(0, index);
                        try
                        {
                            path = new File(path).CanonicalPath;
                        }
                        catch (IOException e)
                        {
                            // Keep non-canonical path.
                        }
                        paths.Add(path);
                    }
                }
            }
            return paths.ToArray();
        }

    }
}