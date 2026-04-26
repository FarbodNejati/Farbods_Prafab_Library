using CodiceApp.Gravatar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;

namespace Farbod.PrefabLibrary
{
    public class PrefabLibraryScrollView : ScrollView
    {
        public new class UxmlFactory : UxmlFactory<PrefabLibraryScrollView, UxmlTraits> { }
        public new class UxmlTraits : ScrollView.UxmlTraits { }

        public new static readonly string ussClassName = "proplibrary_library__scroll";
        public static readonly string cardViewModeUssClassName = "library--boxView";
        public static readonly string rectListModeUssClassName = "library--boxListView";
        public static readonly string compactListModeUssClassName = "library--compactListView";
        ViewMode _currentViewMode;

        private bool _ctrlModifierKeyDown = false;
        private bool _shiftModifierKeyDown = false;

        //Tag --> UnityEditor Asset Guid
        //Dictionary<LibraryTagData, HashSet<string>> _tagToAssetIndex = new();
        List<LibraryTagData> _tagData = new();

        //Asset Guid --> prop data
        Dictionary<string, PrefabLibraryData> _assetDataIndex = new();
        //visual element representation --> Asset Guid
        Dictionary<PrefabLibraryCard, string> _propCardIndexReverse = new();
        Dictionary<string, PrefabLibraryCard> _propCardIndex = new();

        /// <summary>
        /// The active selection
        /// </summary>
        private Dictionary<PrefabLibraryData, PrefabLibraryCard> _selection = new();
        public List<PrefabLibraryData> SelectedData => _selection.Keys.ToList();
        public int activeItemIndex;

#pragma warning disable IDE1006
        /// <summary>
        /// Invoked when the selection list changes.
        /// </summary>
        public event Action<List<PrefabLibraryData>> onSelectionChange;
#pragma warning restore IDE1006

        public PrefabLibraryScrollView()
        {
            AddToClassList(ussClassName);
            EmptyScreen();

            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<KeyUpEvent>(OnKeyUp);

            //Deselect when clicking background;
            RegisterCallback<ClickEvent>(evt => Deselect());
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                // Shift - RangeMultiSelect
                case KeyCode.LeftShift:
                    _shiftModifierKeyDown = true;
                    break;
                // Ctrl - MultiSelect
                case KeyCode.LeftControl:
                    _ctrlModifierKeyDown = true;
                    break;
                // Ctrl+A - SelectAll
                case KeyCode.A:
                    if (_ctrlModifierKeyDown)
                    {
                        _selection = new(_propCardIndexReverse.Count);
                        foreach (var item in _propCardIndexReverse)
                        {
                            AddToSelection(_assetDataIndex[item.Value], item.Key);
                        }
                        onSelectionChange(_selection.Keys.ToList());
                    }
                    break;
                // Ctrl+D - Deselect
                case KeyCode.D:
                    if (_ctrlModifierKeyDown)
                        Deselect();
                    break;

            }
            evt.StopPropagation();
        }


        private void OnKeyUp(KeyUpEvent evt)
        {
            switch (evt.keyCode)
            {
                // Shift - RangeMultiSelect
                case KeyCode.LeftShift:
                    _shiftModifierKeyDown = false;
                    break;
                // Ctrl - MultiSelect
                case KeyCode.LeftControl:
                    _ctrlModifierKeyDown = false;
                    break;

            }

            evt.StopPropagation();
        }

        public void PopulateWithItems(List<PrefabLibraryData> dataList)
        {
            _assetDataIndex.Clear();
            _propCardIndexReverse.Clear();
            contentContainer.Clear();
            _propCardIndex.Clear();

            //Empty list
            if (dataList.Count == 0)
            {
                EmptyScreen();
                return;
            }

            //Build data index
            for (int i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];
                //Register in indexes
                _assetDataIndex.Add(data.assetGuid, data);
                RegisterObjectInTagIndex(data);

                //Populate with visual elements
                var ve = CreatePropItemCard(i, data);
                _propCardIndex.Add(data.assetGuid, ve);
                _propCardIndexReverse.Add(ve, data.assetGuid);
            }
        }
        void EmptyScreen()
        {
            var labelContainer = new VisualElement();

            labelContainer.style.flexGrow = 1;
            labelContainer.style.justifyContent = Justify.Center;
            labelContainer.style.alignItems = Align.Center;
            labelContainer.style.height = 200;

            var label = new Label("No items to display.");
            label.style.unityTextAlign = new(TextAnchor.MiddleCenter);
            label.style.alignSelf = Align.Center;

            labelContainer.Add(label);

            contentContainer.Add(labelContainer);
        }
        public PrefabLibraryCard CreatePropItemCard(int index, PrefabLibraryData data)
        {
            //Create visual element
            var ve = new PrefabLibraryCard();
            ve.focusable = true;
            ve.text = data.prefab.name;
            ve.PreviewTexture = data.previewThumbnail;

            //Go through each tag, see if this asset exists in their hashset, assign tag if so.
            //foreach (var tagHashsetPair in _tagToAssetIndex)
            //{
            //    if (tagHashsetPair.Value.Contains(data.assetGuid))
            //        ve.AddTag(tagHashsetPair.Key);
            //}
            foreach( var tag in data.tagGuids )
            {
                var tagData =  _tagData.Find(t => t.guid == tag);
                if (tagData != null)
                    ve.AddTag(tagData);
            }
            //Register events
            ve.RegisterCallback<ClickEvent>(evt => CardClicked(evt, index, data, ve));
            contentContainer.Add(ve);
            return ve;
        }
        /// <summary>
        /// Called when a prop card in the library is clicked
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="data"></param>
        /// <param name="ve"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CardClicked(ClickEvent evt, int index, PrefabLibraryData data, PrefabLibraryCard ve)
        {
            //-------------------Double click--------------------
            if (evt.clickCount == 2)
            {
                //Select this asset in the project view and inspector
                data.SelectInEditor();
                return;
            }

            //-------------------Selection--------------------
            //Shift click
            if (_shiftModifierKeyDown)
            {
                ClearSelectionVisual();
                int rangeStart = Math.Min(index, activeItemIndex);
                int rangeLength = Math.Abs(index - activeItemIndex) + 1;
                //Find all elements in range
                var selectionElements = contentContainer.Children().ToList().GetRange(rangeStart, rangeLength).ConvertAll(x => (PrefabLibraryCard)x);

                // Find all Data in range based on guid:
                // Element --> GUID --> Data

                _selection = new(selectionElements.Count);
                for (int i = 0; i < selectionElements.Count; i++)
                {
                    if (_propCardIndexReverse.TryGetValue(selectionElements[i], out string guid))
                        AddToSelection(_assetDataIndex[guid], selectionElements[i]);
                }
            }
            //Ctrl click
            else if (_ctrlModifierKeyDown)
            {
                //Set this as active item (focused)
                activeItemIndex = index;
                //Add to selection
                AddToSelection(data, ve);

            }
            //Normal click
            else
            {
                ClearSelectionVisual();
                //Set this as active item (focused)
                activeItemIndex = index;
                //Set this as current selection
                _selection = new() { { data, ve } };
                ve.AddToClassList(PrefabLibraryCard.selectedUssClassName);
            }
            evt.StopPropagation();
            onSelectionChange?.Invoke(_selection.Keys.ToList());
        }
        /// <summary>
        /// Remove the selection class from currently selected items. (only visual)
        /// </summary>
        private void ClearSelectionVisual()
        {
            foreach (var selectedItem in _selection.Values)
            {
                selectedItem.RemoveFromClassList(PrefabLibraryCard.selectedUssClassName);
                selectedItem.Blur();
            }
        }
        private void Deselect()
        {
            ClearSelectionVisual();
            _selection.Clear();
            //_shiftModifierKeyDown = false;
            //_ctrlModifierKeyDown = false;
            onSelectionChange.Invoke(_selection.Keys.ToList());
        }
        /// <summary>
        /// Add an item to our list of selections.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ve"></param>
        private void AddToSelection(PrefabLibraryData data, PrefabLibraryCard ve)
        {
            ve.AddToClassList(PrefabLibraryCard.selectedUssClassName);
            _selection.TryAdd(data, ve);
        }

        public void UpdateTagIndex(List<LibraryTagData> tagData)
        {
            
            if (tagData == null || tagData.Count == 0)
            {
                _tagData = new();
                return;
            }

            _tagData = tagData;
            //Place tags in tag index


            //_tagToAssetIndex = new(tagData.Count);
            //foreach (var tag in tagData)
            //{
            //    HashSet<string> assetsWithTag = new(0);
            //    _tagToAssetIndex.Add(tag, assetsWithTag);
            //}

            ////Assign objects to the hashset of each tag, in the tag index
            //foreach (var kvp in _assetDataIndex)
            //{
            //    PrefabLibraryData assetData = kvp.Value;
            //    assetData.tagGuids.ForEach(tagGuid =>
            //    {
            //        var tag = tagData.Find(t => t.guid == tagGuid);
            //        //If the tag on this asset exists in our tags, add this object's guid to the tag index.
            //        if (_tagToAssetIndex.TryGetValue(tag, out var hashset))
            //        {
            //            hashset.Add(assetData.assetGuid);
            //        }
            //    });
            //}
        }

        void RegisterObjectInTagIndex(PrefabLibraryData assetData)
        {
            //assetData.tagGuids.ForEach(tagGuid =>
            //{
            //    var tag = _tagData.Find(t => t.guid == tagGuid);
            //    HashSet<string> hashset = _tagToAssetIndex[tag];
            //    //If the tag on this asset exists in our tags, add this object's guid to the tag index.
            //    if (hashset != null)
            //    {
            //        hashset.Add(assetData.assetGuid);
            //    }
            //});
        }

        public enum ViewMode
        {
            cards,
            rectList,
            compactList
        }
        public void SetViewMode(ViewMode viewMode)
        {
            if (viewMode == _currentViewMode) return;

            //Change class on library scroll view
            RemoveFromClassList(ViewModeCssClass(_currentViewMode));
            AddToClassList(ViewModeCssClass(viewMode));

            //Change curr mode
            _currentViewMode = viewMode;
        }
        private string ViewModeCssClass(ViewMode viewMode)
        {
            switch (viewMode)
            {
                case ViewMode.cards:
                    return cardViewModeUssClassName;
                case ViewMode.rectList:
                    return rectListModeUssClassName;
                case ViewMode.compactList:
                    return compactListModeUssClassName;
                default:
                    return null;
            }
        }

        /// <summary>
        /// This removes a tag from items within the tag index, and visually, from said items.
        /// </summary>
        public void RemoveTagFromExistingItems<T>(string tagGuid, T assets) where T : IEnumerable<PrefabLibraryData>
        {
            var tag = _tagData.Find(t => t.guid == tagGuid);

            //Attempt to find the hashset related to this tag
            //if (!_tagToAssetIndex.TryGetValue(tag, out var hashset))
            //    return;

            ////Go through all assets and remove them from the hashset
            //foreach (var asset in assets)
            //{
            //    //If this asset has the tag attached
            //    if (hashset.Contains(asset.assetGuid))
            //    {
            //        hashset.Remove(asset.assetGuid);
            //        _propCardIndex[asset.assetGuid].RemoveTag(tag.guid);
            //    }
            //}

            if(tag != null)
            {
                foreach (var asset in assets)
                {
                    _propCardIndex[asset.assetGuid].RemoveTag(tagGuid);
                }
            }
                
        }

        /// <summary>
        /// This removes a tag from items within the tag index, and visually, from said items.
        /// </summary>
        public void AddTagToExistingItems<T>(string tagGuid, T assets) where T : IEnumerable<PrefabLibraryData>
        {
            var tag = _tagData.Find(t => t.guid == tagGuid);
            AddTagToExistingItems(tag, assets);
        }

        /// <summary>
        /// This removes a tag from items within the tag index, and visually, from said items.
        /// </summary>
        public void AddTagToExistingItems<T>(LibraryTagData tag, T assets) where T : IEnumerable<PrefabLibraryData>
        {
            //Attempt to find the hashset related to this tag
            //if (!_tagToAssetIndex.TryGetValue(tag, out var hashset))
            //    return;

            foreach (var asset in assets)
            {
                //hashset.Add(asset.assetGuid);
                _propCardIndex[asset.assetGuid].AddTag(tag);
            }


        }
    }
}