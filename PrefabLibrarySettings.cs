using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Farbod.PrefabLibrary
{
    public class PrefabLibrarySettings : ScriptableObject
    {
        /// <summary>
        /// The current settings file. returns null if no settings file is located at the seleted path
        /// </summary>
        public static PrefabLibrarySettings Instance => LocateSettings();



        public static Action<string> onDirectoryChange;

        public readonly static string SETTINGS_OG_DIR = "Assets/Settings";
        public readonly static string SETTINGS_OG_PATH = SETTINGS_OG_DIR+"/PrefabLibrarySettings.asset";
        public static string SETTINGS_PATH
        {
            get { return EditorPrefs.GetString("PrefabLibSettingPath", SETTINGS_OG_PATH); }
            set { EditorPrefs.SetString("PrefabLibSettingPath", value); }
        }

        public PrefabLibraryScrollView.ViewMode viewMode = PrefabLibraryScrollView.ViewMode.rectList;
        public string libraryDirectoryRoot = "";
        public List<LibraryTagData> RegisteredTags = new();

        private static PrefabLibrarySettings LocateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PrefabLibrarySettings>(SETTINGS_PATH);
            return settings;
        }
        public static PrefabLibrarySettings LocateOrCreateSettingsAsset()
        {
            //Load setting from editorPrefs saved path, or OG path
            var settings = LocateSettings();

            // Create the asset if it doesn't exist
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PrefabLibrarySettings>();

                //Ensure existance of settings directory
                string BuildingPath = "Assets";
                var folders = SETTINGS_OG_DIR.Split('/').ToList();
                folders.RemoveAt(0);
                foreach (var folder in folders)
                {
                    if (!AssetDatabase.IsValidFolder(BuildingPath + '/' + folder))
                        AssetDatabase.CreateFolder(BuildingPath, folder);

                    BuildingPath += ("/" + folder);
                }

                AssetDatabase.CreateAsset(settings, SETTINGS_OG_PATH);
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SETTINGS_PATH = SETTINGS_OG_PATH;
                Debug.Log($"Created new {typeof(PrefabLibrarySettings)} asset at: {SETTINGS_OG_PATH}");
            }
                

            return settings;
        }
        /// <summary>
        /// If a missing tag is found it can be registered here.
        /// </summary>
        /// <param name="tagGuid"></param>
        /// <returns>newly registered tag</returns>
        public bool TryRegisterMissingTagGuid(string tagGuid, out LibraryTagData newTag)
        {
            newTag = RegisteredTags.Find(t=>t.guid==tagGuid);

            try
            {
                newTag = new(tagGuid, tagGuid, new(0.4f, 0.4f, 0.4f, 0.2f));
                RegisteredTags.Add(newTag);
                EditorUtility.SetDirty(this);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
        }


        public bool SelectRootDirectory()
        {

            string projectAssetsPath = "Assets";
            string startingPath = AssetDatabase.IsValidFolder(libraryDirectoryRoot) ? libraryDirectoryRoot : projectAssetsPath;
            string folderPath = EditorUtility.OpenFolderPanel("Select Prefab Library", startingPath, "");


            if (string.IsNullOrEmpty(folderPath))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder within your project's Assets directory.", "OK");
                return false;
            }

            string relativePath = FileUtil.GetProjectRelativePath(folderPath);


            if (!relativePath.StartsWith(projectAssetsPath) || !AssetDatabase.IsValidFolder(relativePath))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder within your project's Assets directory.", "OK");
                return false;
            }
            if (relativePath.TrimEnd('/').EndsWith(projectAssetsPath))
            {
                EditorUtility.DisplayDialog("Invalid Folder", "The folder must be a subfolder of your Assets directory.", "OK");
                return false;
            }

            libraryDirectoryRoot = relativePath;
            EditorUtility.SetDirty(this);

            onDirectoryChange?.Invoke(relativePath);
            return true;
        }

        public static bool RelocateSettings()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Relocate Library", "Assets", new string[2] {"Scriptable Object", "asset"});
            

            //Directory validity
            string relativePath = FileUtil.GetProjectRelativePath(path);
            string relativeDir = FileUtil.GetProjectRelativePath(Path.GetDirectoryName(path).Replace('\\','/'));

            if (!AssetDatabase.IsValidFolder(relativeDir))
            {
                EditorUtility.DisplayDialog("Invalid File Path", "Please select a file within your project's Assets directory.", "OK");
                return false;
            }

            //SO check
            var asset = AssetDatabase.LoadAssetAtPath(relativePath, typeof(PrefabLibrarySettings));
            if ((asset) == null)
            {
                EditorUtility.DisplayDialog("Invalid File Type", $"The file must be a '{typeof(PrefabLibrarySettings).Name}' Scriptable object.", "OK");
                return false;
            }
            SETTINGS_PATH = relativePath;
            return true;
        }

    }

    [System.Serializable]
    public class LibraryTagData
    {
        [SerializeField] public string name;
        [SerializeField, HideInInspector] public string guid;
        [SerializeField] public Color color;

        public LibraryTagData(string name, Color color)
        {
            this.name = name;
            this.color = color;
            this.guid = Guid.NewGuid().ToString();
        }

        public LibraryTagData(string guid, string name, Color color)
        {
            if (string.IsNullOrEmpty(guid) || !Guid.TryParse(guid, out var parsedGuid))
            {
                throw new FormatException($"The string '{guid}' is not a valid GUID format.");
            }

            this.name = name;
            this.color = color;
            this.guid = guid;
        }
    }

}