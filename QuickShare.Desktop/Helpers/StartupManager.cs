using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Desktop.Helpers
{
    // from http://www.visualbx.com/blog/run-wpf-application-at-windows-startup/
    public class StartupManager
    {
        string appName;

        public StartupManager(string _appName)
        {
            appName = _appName;
        }

        public string GetExecutablePath()
        {
            var exec = System.Reflection.Assembly.GetExecutingAssembly().Location;

            var fileName = System.IO.Path.GetFileName(exec);
            var parentDir = System.IO.Path.GetDirectoryName(exec);
            var parentParentDir = System.IO.Path.GetDirectoryName(parentDir);

            var squirrelDummyExe = System.IO.Path.Combine(parentParentDir, fileName);
            if (System.IO.File.Exists(squirrelDummyExe))
                return squirrelDummyExe;
            else
                return exec;
        }

        public void AddApplicationToCurrentUserStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue(appName, "\"" + GetExecutablePath() + "\"");
            }
        }

        public void AddApplicationToAllUserStartup()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.SetValue(appName, "\"" + GetExecutablePath() + "\"");
            }
        }

        public void RemoveApplicationFromCurrentUserStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.DeleteValue(appName, false);
            }
        }

        public void RemoveApplicationFromAllUserStartup()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                key.DeleteValue(appName, false);
            }
        }

        public bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            try
            {
                //get the currently logged in user
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }
    }
}
