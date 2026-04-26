using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Farbod.PrefabLibrary
{
    /// <summary>
    /// Utility class for writing and reading tags on a prefabs .meta file.
    /// </summary>
    public static partial class AssetTaggerUtility
    {
        /// <summary>
        /// Fetch tags assigned to a prop's meta file.
        /// </summary>
        /// <returns>Null if no tags are assigned in userData</returns>
        public static string[] GetAssignedTags(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null || string.IsNullOrEmpty(importer.userData))
                return new string[0];

            try
            {
                return JsonUtility.FromJson<PrefabAssignedData>(importer.userData)?.tagGuids ?? new string[0];
            }
            catch
            {
                return null; // fallback for legacy text format
            }
        }

        /// <summary>
        /// Assign tags to the meta file of an asset
        /// </summary>
        /// <returns>Null if no tags are assigned in userData</returns>
        public static void SetAssignedTags(string assetPath, string[] tags)
        {
            var validTags = tags.Where(t => !string.IsNullOrEmpty(t));

            var importer = AssetImporter.GetAtPath(assetPath);

            if (tags.Length > 0) {
                PrefabAssignedData data = new();
                data.tagGuids = validTags.ToArray();

                importer.userData = JsonUtility.ToJson(data);
            }
            else
                importer.userData = "";
            
            importer.SaveAndReimport();
        }
    }

}
