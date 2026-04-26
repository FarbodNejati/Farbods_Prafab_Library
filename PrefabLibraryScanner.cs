using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Farbod.PrefabLibrary
{
    /// <summary>
    /// The class in charge of managing and indexing the prefab library
    /// </summary>
    public class PrefabLibraryScanner
    {
        public string rootDirectory;

        /// <summary>
        /// An array containing the data relating to every single prefab asset under our root directory
        /// </summary>
        public PrefabLibraryData[] fullLibrary { get; private set; }

        private List<LibraryTagData> tagRegistry;

        /// <summary>
        /// A dictionary containing all the registered tags, and a hashset containing the objects with those tags
        /// </summary>
        public Dictionary<string, HashSet<string>> tagToAssetIndex { get; private set; } = new();

        /// <summary>
        /// A dictionary containing the path of all subfolder, and a hashset containing the objects directly in those folders;
        /// </summary>
        public Dictionary<string, HashSet<string>> pathToAssetIndex { get; private set; } = new();

        public PrefabLibraryScanner(string path)
        {
            UpdatePath(path);
        }
        void InitializeTagIndex(List<LibraryTagData> tags)
        {
            tagRegistry = tags;
            tagToAssetIndex = new(tags.Count);
            foreach (var tag in tags)
            {
                tagToAssetIndex[tag.guid] = new();
            }
        }
        void InitializePathIndex(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath))
                return;

            //Get all sub directories
            string[] directories = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);

            //Ensure correct separators
            directories = directories.Select(d => d.Replace('\\', '/')).ToArray();

            pathToAssetIndex = new(directories.Length);
            foreach (var dir in directories)
            {
                pathToAssetIndex[dir] = new();
            }
        }

        public void UpdateTagIndex()
        {
            InitializeTagIndex(PrefabLibrarySettings.Instance.RegisteredTags);
        }
        public void UpdatePath(string path)
        {
            rootDirectory = path;

            InitializeTagIndex(PrefabLibrarySettings.Instance.RegisteredTags);
            InitializePathIndex(rootDirectory);
        }

        public void Scan()
        {
            //Check Directory validity
            if (!AssetDatabase.IsValidFolder(rootDirectory))
            {
                Debug.LogError($"Folder '{rootDirectory}' does not exist or is not a valid Unity folder.");
                return;
            }

            //Get all prefab assets under our root directory
            var prefab_guids = AssetDatabase.FindAssets("t:Prefab", new[] { rootDirectory });
            fullLibrary = new PrefabLibraryData[prefab_guids.Length];

            //Fill full library list
            for (int i = 0; i < fullLibrary.Length; i++)
            {
                //Get data
                PrefabLibraryData data = new();
                data.assetGuid = prefab_guids[i];
                var path = AssetDatabase.GUIDToAssetPath(prefab_guids[i]);
                data.assetPath = path;
                data.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                data.name = data.prefab.name;
                data.tagGuids = AssetTaggerUtility.GetAssignedTags(path).ToList();
                data.GeneratePreviewThumbnail();

                //Index in database
                IndexAssetInDatabase(data);
                //Add to data base
                fullLibrary[i] = data;
            }
        }

        public void IndexAssetInDatabase(PrefabLibraryData assetData)
        {
            //================================================================
            //===================== TagGuid -> AssetGuid =====================
            //================================================================

            //Go through each asset tag, and add this asset to the proper hashsets
            assetData.tagGuids.ForEach(tagGuid =>
            {
                var tag = tagRegistry.Find(t => t.guid == tagGuid);

                //If the tag on this asset exists in our tags, add this asset's guid to the tag index.
                if (tag!=null && tagToAssetIndex.TryGetValue(tag.guid, out var hashset))
                {
                    hashset.Add(assetData.assetGuid);
                }
                //handle missing tags.
                else if(PrefabLibrarySettings.Instance.TryRegisterMissingTagGuid(tagGuid, out var missingTag))
                {
                    tagToAssetIndex.Add(missingTag.guid, new(1) { assetData.assetGuid });
                }
            });

            //================================================================
            //====================== Path  -> AssetGuid ======================
            //================================================================

            //Get the directory of this asset
            string dir = Path.GetDirectoryName(assetData.assetPath).Replace("\\", "/");

            //If the directory containing this path exists in our index, add this asset's guid to the tag index.
            if (pathToAssetIndex.TryGetValue(dir, out var hashset))
                hashset.Add(assetData.assetGuid);

            //Otherwise, register the new directory, and add it to our index (also register this object in the index hashset)
            else
                pathToAssetIndex.Add(dir, new(0) { assetData.assetGuid });


        }

        public void RemoveTagFromAsset(PrefabLibraryData assetData, string tagGuid)
        {
            //================================================================
            //===================== TagGuid -> AssetGuid =====================
            //================================================================

            var tag = PrefabLibrarySettings.Instance.RegisteredTags.Find(t => t.guid == tagGuid);
            if (tagToAssetIndex.TryGetValue(tag.guid, out var hashset))
            {
                hashset.Remove(assetData.assetGuid);
            }

        }

        public void AddTagToAsset(PrefabLibraryData assetData, LibraryTagData tag)
        {
            //================================================================
            //===================== TagGuid -> AssetGuid =====================
            //================================================================
            if (tagToAssetIndex.TryGetValue(tag.guid, out var hashset))
            {
                hashset.Add(assetData.assetGuid);
            }
        }
        
        public List<PrefabLibraryData> FilterByTag<T>(T input, string tagGuid) where T : IEnumerable<PrefabLibraryData>
        {
            //Find the target hashset
            HashSet<string> targetHashset;

            //Check if this tag exists
            if (!tagToAssetIndex.TryGetValue(tagGuid, out targetHashset))
            {
                Debug.LogError($"Tag guid '{tagGuid}' is not indexed in the library scanner. If you are sure that this tag exists, perform a full rescan.");
                return new();
            }

            List<PrefabLibraryData> result = input.Where(data => targetHashset.Contains(data.assetGuid)).ToList();

            //Filter input
            return result;
        }

        public List<PrefabLibraryData> FilterByPath<T>(T input, string path) where T : IEnumerable<PrefabLibraryData>
        {
            //Dont apply a filter if the path is empty
            if (string.IsNullOrEmpty(path))
                return input.ToList();

            //Find the target hashset
            HashSet<string> targetHashset;

            //Check if this tag exists
            if (!pathToAssetIndex.TryGetValue(path, out targetHashset))
            {
                Debug.LogError($"The requested path is not indexed in the library scanner. If you are sure that this tag exists, perform a full rescan.");
                return new();
            }

            List<PrefabLibraryData> result = input.Where(data => targetHashset.Contains(data.assetGuid)).ToList();

            //Filter input
            return result;
        }

        public List<PrefabLibraryData> FilterBySearchPrompt<T>(T input, string searchPrompt) where T : IEnumerable<PrefabLibraryData>
        {
            //Dont apply a filter if the path is empty
            if (string.IsNullOrEmpty(searchPrompt))
                return input.ToList();

            //Add items from all tags, that this search might be referancing.
            IEnumerable<PrefabLibraryData> tagSearchResult = new List<PrefabLibraryData>();
            var matchingTags = tagRegistry.FindAll(tag => tag.name.Contains(searchPrompt, StringComparison.CurrentCultureIgnoreCase));
            foreach (var tag in matchingTags)
            {
                var tagResult = FilterByTag(input, tag.guid);
                tagSearchResult = tagSearchResult.Union(tagResult);
            }
            



            //Search by name
            List<PrefabLibraryData> nameSearchResult = input.ToList().Where(asset =>
            {
                return asset.name.Contains(searchPrompt) || asset.assetPath.Contains(searchPrompt);
            }).ToList();

            //Filter input
            return nameSearchResult.Union(tagSearchResult).Union(nameSearchResult).ToList();
        }
    }
}