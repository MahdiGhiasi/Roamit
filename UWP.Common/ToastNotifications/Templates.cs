using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.ToastNotifications
{
    public static class Templates
    {
        public static string BasicText { get; } = @"
<toast launch='{argsLaunch}'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
</toast>";

        public static string ProgressBar { get; } = @"
<toast launch='{argsLaunch}'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <progress
        title='{progressTitle}'
        value='{progressValue}'
        valueStringOverride='{progressValueStringOverride}'
        status='{progressStatus}'/>
    </binding>
  </visual>  
</toast>";

        public static string SingleFileReceived { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open folder' activationType='foreground' arguments='action=openFolderSingleFile&amp;guid={guid}'/>
    <action content='Open file' activationType='foreground' arguments='action=openSingleFile&amp;guid={guid}'/>
  </actions>
</toast>";

        public static string SingleFileReceivedWithNoOpenFileButton { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open folder' activationType='foreground' arguments='action=openFolderSingleFile&amp;guid={guid}'/>
  </actions>
</toast>";

        public static string MultipleFilesReceived { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open folder' activationType='foreground' arguments='action=openFolder&amp;guid={guid}'/>
  </actions>
</toast>";

    }
}
