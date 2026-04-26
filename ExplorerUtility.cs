using System.IO;
using UnityEditor;
using UnityEngine;

namespace Farbod.PrefabLibrary
{
    /// <summary>
    /// Class for interacting with the file explorer of the current OS.
    /// </summary>
    public static class ExplorerUtility
    {
        /// <summary>
        /// From https://discussions.unity.com/t/how-to-implement-show-in-explorer/13518/3
        /// </summary>
        public static void OpenProjectPathInFileBrowser(string relativePath)
        {
            string absolutePathFromRelative = System.IO.Path.GetFullPath(relativePath);
            if (string.IsNullOrEmpty(absolutePathFromRelative) || !File.Exists(absolutePathFromRelative))
                return;

            OpenInFileBrowser(absolutePathFromRelative);
        }

        public static void OpenInFileBrowser(string path)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                OpenInLinuxFileBrowser(path);
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                OpenInMacFileBrowser(path);
            }
            else // assume Windows
            {
                OpenInWinFileBrowser(path);
            }
        }

        public static void OpenInLinuxFileBrowser(string path)
        {
            bool openInsidesOfFolder = false;

            string linuxPath = path.Replace("\\", "/"); // linux  doesn't like backward slashes

            if (System.IO.Directory.Exists(linuxPath)) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }

            try
            {
                // https://askubuntu.com/a/1424380
                // Note: xdg-open only works properly when given a folder.
                // If given a path to a file, xdg-open will open that file with the associated program.
                // So we use dbus-send instead if we're showing a file.

                string processName;
                string arguments;
                if (openInsidesOfFolder)
                {
                    processName = "xdg-open";
                    arguments = $"\"{linuxPath}\"";
                }
                else
                {
                    processName = "dbus-send";
                    arguments = $"--print-reply --dest=org.freedesktop.FileManager1 /org/freedesktop/FileManager1 org.freedesktop.FileManager1.ShowItems array:string:\"file://{linuxPath}\" string:\"\"";
                }

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    FileName = processName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                System.Diagnostics.Process.Start(processStartInfo);
            }
            catch (System.Exception e)
            {
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
                                 //Debug.LogError($"{e}");

#if UNITY_EDITOR
                // EditorUtility.RevealInFinder is sure to work, but for files, it doesn't allow us to pre-select the file specified.
                // For folders, it can't open the insides of a folder, instead it will open the parent folder.
                // Very strange behavior, so we use EditorUtility.RevealInFinder only as our last resort.
                UnityEditor.EditorUtility.RevealInFinder(path);
#endif
            }
        }

        public static void OpenInMacFileBrowser(string path)
        {
            bool openInsidesOfFolder = false;

            // try mac
            string macPath = path.Replace("\\", "/"); // mac finder doesn't like backward slashes

            if (System.IO.Directory.Exists(macPath)) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }

            if (!macPath.StartsWith("\""))
            {
                macPath = "\"" + macPath;
            }

            if (!macPath.EndsWith("\""))
            {
                macPath = macPath + "\"";
            }

            string arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;
            try
            {
                System.Diagnostics.Process.Start("open", arguments);
            }
            catch (System.Exception e)
            {
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it

#if UNITY_EDITOR
                // EditorUtility.RevealInFinder is sure to work, but for files, it doesn't allow us to pre-select the file specified.
                // For folders, it can't open the insides of a folder, instead it will open the parent folder.
                // Very strange behavior, so we use EditorUtility.RevealInFinder only as our last resort.
                UnityEditor.EditorUtility.RevealInFinder(path);
#endif
            }
        }

        public static void OpenInWinFileBrowser(string path)
        {
            bool openInsidesOfFolder = false;

            // try windows
            string winPath = path.Replace("/", "\\"); // windows explorer doesn't like forward slashes

            if (System.IO.Directory.Exists(winPath)) // if path requested is a folder, automatically open insides of that folder
            {
                openInsidesOfFolder = true;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + winPath);
            }
            catch (System.Exception e)
            {
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it

#if UNITY_EDITOR
                // EditorUtility.RevealInFinder is sure to work, but for files, it doesn't allow us to pre-select the file specified.
                // For folders, it can't open the insides of a folder, instead it will open the parent folder.
                // Very strange behavior, so we use EditorUtility.RevealInFinder only as our last resort.
                UnityEditor.EditorUtility.RevealInFinder(path);
#endif
            }
        }
    }

}