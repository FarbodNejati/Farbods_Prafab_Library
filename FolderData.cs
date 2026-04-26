using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Farbod.PrefabLibrary
{

    /// <summary>
    /// WARNING : AI GENERATED
    /// </summary>
    [System.Serializable]
    public class FolderData
    {
        public string Name;
        public string AssetsRelativePath; // Store the Unity Asset path
        public List<FolderData> Subfolders = new List<FolderData>();

        // Constructor
        public FolderData(string name, string assetPath)
        {
            Name = name;
            AssetsRelativePath = assetPath;
        }

        // Method to load subfolders using AssetDatabase
        public void LoadSubfolders()
        {
            // Clear existing subfolders to avoid duplicates if called multiple times
            Subfolders.Clear();

            // --- Use AssetDatabase to get subdirectories ---
            // AssetDatabase.GetSubFolders returns paths relative to the project root.
            string[] subFolderPaths = AssetDatabase.GetSubFolders(AssetsRelativePath);

            foreach (string subFolderPath in subFolderPaths)
            {
                // Get the name of the folder from the full path
                // Path.GetFileName works on these paths because AssetDatabase returns paths
                // that are compatible with standard path manipulation after the initial lookup.
                string folderName = Path.GetFileName(subFolderPath);

                // Create a new FolderData for the subfolder
                FolderData subfolderData = new FolderData(folderName, subFolderPath);

                // Recursively load its subfolders
                subfolderData.LoadSubfolders();

                // Add the newly created subfolder to our list
                Subfolders.Add(subfolderData);
            }
        }
    }

}