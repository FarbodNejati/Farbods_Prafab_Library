using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Farbod.PrefabLibrary
{
    [System.Serializable]
    public class PrefabAssignedData
    {
        public string[] tagGuids;
    }


    [System.Serializable]
    public class PrefabLibraryData
    {
        public string name;
        public string assetPath;
        public string assetGuid;
        public GameObject prefab;
        public Texture2D previewThumbnail { get; private set; }
        public List<string> tagGuids;
        public PrefabLibraryData(string assetGuid, string assetPath, GameObject prefab, List<string> tagGuids, bool generateThumbnail = true)
        {
            this.assetPath = assetPath;
            this.assetGuid = assetGuid;
            this.prefab = prefab;
            this.name = prefab.name;
            this.tagGuids = tagGuids;

            if (generateThumbnail)
                GeneratePreviewThumbnail();
        }

        public PrefabLibraryData() { }

        public void GeneratePreviewThumbnail()
        {
            Texture2D tex = AssetPreview.GetAssetPreview(prefab);

            // AssetPreview works asynchronously; wait until it’s created
            while (tex == null)
            {
                AssetPreview.GetAssetPreview(prefab);
                System.Threading.Thread.Sleep(50);
                tex = AssetPreview.GetMiniThumbnail(prefab);
            }
            previewThumbnail = tex;
        }

        public void SelectInEditor()
        {
            UnityEditor.Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }
}