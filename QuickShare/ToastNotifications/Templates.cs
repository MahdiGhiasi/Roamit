using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.ToastNotifications
{
    internal static class Templates
    {
        internal static string BasicText { get; } = @"
<toast launch='{argsLaunch}'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
</toast>";

        internal static string ProgressBar { get; } = @"
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

        internal static string FileReceived { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open containing folder' activationType='foreground' arguments='action=openContainingFolder&amp;guid={guid}'/>
  </actions>
</toast>";

    }
}
