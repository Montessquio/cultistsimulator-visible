﻿using UnityEngine;
using System.Diagnostics;
namespace SecretHistories.UI
{
    public static class OpenInFileBrowser
    {
        public static bool IsInMacOS
        {
            get
            {
                return Application.platform == RuntimePlatform.OSXPlayer ||
                       Application.platform == RuntimePlatform.OSXEditor;
            }
        }
 
        public static bool IsInWinOS
        {
            get
            {
                return Application.platform == RuntimePlatform.WindowsPlayer ||
                       Application.platform == RuntimePlatform.WindowsEditor;
            }
        }
 
        public static bool IsInInLinuxOS
        {
            get
            {
                return Application.platform == RuntimePlatform.LinuxPlayer ||
                       Application.platform == RuntimePlatform.LinuxEditor;


            }
        }
        public static void Test()
        {
            Open(UnityEngine.Application.dataPath);
        }
 
        public static void OpenInMac(string path)
        {
            bool openInsidesOfFolder = false;
 
            // try mac
            string macPath = path.Replace("\\", "/"); // mac finder doesn't like backward slashes
 
            if ( System.IO.Directory.Exists(macPath) ) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }
 
            if ( !macPath.StartsWith("\"") )
            {
                macPath = "\"" + macPath;
            }
 
            if ( !macPath.EndsWith("\"") )
            {
                macPath = macPath + "\"";
            }
 
            string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;
 
            try
            {
                System.Diagnostics.Process.Start("open", arguments);
            }
            catch ( System.ComponentModel.Win32Exception e )
            {
                // tried to open mac finder in windows
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }
 
        public static void OpenInWin(string path)
        {
            bool openInsidesOfFolder = false;
 
            // try windows
            string winPath = path.Replace("/", "\\"); // windows explorer doesn't like forward slashes
 
            if ( System.IO.Directory.Exists(winPath) ) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }
 
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + winPath);
            }
            catch ( System.ComponentModel.Win32Exception e )
            {
                // tried to open win explorer in mac
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }

        public static void OpenInLinux(string path)
        {
           
        
            string linuxPath = System.IO.Path.GetFullPath(path);
            string dirPath = System.IO.Path.GetDirectoryName(path);
            if(System.IO.Directory.Exists(dirPath))
            {
                linuxPath = dirPath; // open the directory if it exists
            }
            linuxPath = '"'+linuxPath.Replace("\"","\\\"")+'"';// replace any quotes, wrap path in quotes
            NoonUtility.Log("Linux open path: "+linuxPath);
            try
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "xdg-open";
                info.UseShellExecute = true;
                info.Arguments = linuxPath;
                System.Diagnostics.Process.Start(info);
            }
            catch ( System.Exception e )
            {
                  NoonUtility.Log(e.ToString());
                // tried to open linux explorer not on linux
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }
 
        public static void Open(string path)
        {
            
            if(IsInInLinuxOS)
            {
                OpenInLinux(path);
            }
            else if ( IsInWinOS )
            {
                OpenInWin(path);
            }
            else if ( IsInMacOS )
            {
                OpenInMac(path);
            }
           
            else // couldn't determine OS
            {
               
                OpenInWin(path);
                OpenInMac(path);
                OpenInLinux(path);
            }  
        }
    }
}