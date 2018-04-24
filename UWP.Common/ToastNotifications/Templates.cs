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

        public static string SinglePhotoReceived { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <image placement='hero' src='{heroImagePath}'/>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open file' imageUri='Assets/ToastNotificationIcons/OpenFile.png' activationType='foreground' arguments='action=openSingleFile&amp;guid={guid}'/>
    <action content='Open folder' imageUri='Assets/ToastNotificationIcons/OpenFolder.png' activationType='foreground' arguments='action=openFolderSingleFile&amp;guid={guid}'/>
    <action content='Move' imageUri='Assets/ToastNotificationIcons/Move.png' activationType='foreground' arguments='action=saveAsSingleFile&amp;guid={guid}'/>
  </actions>
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
    <action content='Open file' imageUri='Assets/ToastNotificationIcons/OpenFile.png' activationType='foreground' arguments='action=openSingleFile&amp;guid={guid}'/>
    <action content='Open folder' imageUri='Assets/ToastNotificationIcons/OpenFolder.png' activationType='foreground' arguments='action=openFolderSingleFile&amp;guid={guid}'/>
    <action content='Move' imageUri='Assets/ToastNotificationIcons/Move.png' activationType='foreground' arguments='action=saveAsSingleFile&amp;guid={guid}'/>
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
    <action content='Open containing folder' imageUri='Assets/ToastNotificationIcons/OpenFolder.png' activationType='foreground' arguments='action=openFolderSingleFile&amp;guid={guid}'/>
    <action content='Move received files' imageUri='Assets/ToastNotificationIcons/Move.png' activationType='foreground' arguments='action=saveAsSingleFile&amp;guid={guid}'/>
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
    <action content='Open containing folder' imageUri='Assets/ToastNotificationIcons/OpenFolder.png' activationType='foreground' arguments='action=openFolder&amp;guid={guid}'/>
    <action content='Move received files' imageUri='Assets/ToastNotificationIcons/Move.png' activationType='foreground' arguments='action=saveAs&amp;guid={guid}'/>
  </actions>
</toast>";



        public static string SaveAsSuccessfulSingleFileNoOpenFile { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open containing folder' imageUri='Assets/ToastNotificationIcons/OpenFolder.png' activationType='foreground' arguments='action=openFolderSingleFile&amp;guid={guid}'/>
  </actions>
</toast>";

        public static string SaveAsSuccessfulSingleFile { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open containing folder' imageUri='Assets/ToastNotificationIcons/OpenFolder.png' activationType='foreground' arguments='action=openFolderSingleFile&amp;guid={guid}'/>
    <action content='Open file' imageUri='Assets/ToastNotificationIcons/OpenFile.png' activationType='foreground' arguments='action=openSingleFile&amp;guid={guid}'/>
  </actions>
</toast>";

        public static string SaveAsSuccessful { get; } = @"
<toast launch='action=fileFinished'>
  <visual>
    <binding template='ToastGeneric'>
      <text>{title}</text>
      <text>{subtitle}</text>
    </binding>
  </visual>
  <actions>
    <action content='Open containing folder' imageUri='Assets/ToastNotificationIcons/OpenFolder.png' activationType='foreground' arguments='action=openFolder&amp;guid={guid}'/>
  </actions>
</toast>";
    }
}
