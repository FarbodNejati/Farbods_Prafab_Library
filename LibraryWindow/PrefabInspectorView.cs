using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Farbod.PrefabLibrary
{
    public class PrefabInspectorView : PrefabLibraryCard
    {
        public new class UxmlFactory : UxmlFactory<PrefabInspectorView, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits { }

        protected TextField _newTagField;
        private Button _newTagButton;
        private Toolbar _toolbar;
        private ToolbarMenu _toolbarMenu;

        public PrefabLibraryData targetData { get; private set; }

        public event Action<string> onRegisterNewTag;
        public event Action<string> onRemoveTagGuid;
        public event Action<HashSet<string>> onRemoveDifferingTagsGuid;
        public event Action<string> onOpenTagGuid;

        public PrefabInspectorView() : base()
        {
            AddToClassList("inspector-item");

            //Toolbar
            _toolbar = new();
            hierarchy.Insert(0, _toolbar);

            Label _toolbarLabel = new("Inspector");
            _toolbar.Add(_toolbarLabel);

            ToolbarSpacer _toolbarSpacer = new ToolbarSpacer();
            _toolbar.Add(_toolbarSpacer);

            _toolbarMenu = new();
            _toolbar.Add(_toolbarMenu);

            //Separator above tags
            var sep = new VisualElement();
            sep.AddToClassList("inspector_separator");
            contentContainer.Insert(contentContainer.IndexOf(_tagsContainer), sep);

            //Tag field above tags
            _newTagField = new();
            _newTagField.label = "tags:";
            _newTagField.AddToClassList("new-tag-field");
            _newTagField.RegisterCallback<KeyDownEvent>(OnFieldKeyDown);

            _newTagButton = new Button();
            _newTagButton.text = "add";
            _newTagButton.clicked += RegisterNewTagInput;
            _newTagField.Add(_newTagButton);

            contentContainer.Insert(contentContainer.IndexOf(_tagsContainer), _newTagField);


            ClearInspector();
        }

        
        void SetUpToolbarMenu(PrefabLibraryData data)
        {
            ClearToolbarMenu();
            if (data == null)
            {
                _toolbarMenu.menu.AppendAction("Select prefab", null, DropdownMenuAction.Status.Disabled);
                _toolbarMenu.menu.AppendAction("Open in explorer", null, DropdownMenuAction.Status.Disabled);
                _toolbarMenu.menu.InsertSeparator(null, 2);
                _toolbarMenu.menu.AppendAction("Copy folder path", null, DropdownMenuAction.Status.Disabled);
                _toolbarMenu.menu.AppendAction("Copy full path", null, DropdownMenuAction.Status.Disabled);
                return;
            }
            _toolbarMenu.menu.AppendAction("Select prefab", evt => data.SelectInEditor());
            _toolbarMenu.menu.AppendAction("Open in explorer", evt => ExplorerUtility.OpenProjectPathInFileBrowser(data.assetPath));

            _toolbarMenu.menu.InsertSeparator(null, 2);


            _toolbarMenu.menu.AppendAction("Copy folder path", evt => {
                var path = System.IO.Path.GetFullPath(data.assetPath);
                path = Path.GetDirectoryName(path);
                if (path != null)
                    GUIUtility.systemCopyBuffer = path;
            }); ;
            _toolbarMenu.menu.AppendAction("Copy full path", evt => {
                var path = System.IO.Path.GetFullPath(data.assetPath);
                if (path != null)
                    GUIUtility.systemCopyBuffer = path;
            }); ;
        }
        void ClearToolbarMenu()
        {
            var items = _toolbarMenu.menu.MenuItems();
            if (items == null || items.Count == 0)
                return;

            for (int i = items.Count-1; i >= 0; i--)
            {
                _toolbarMenu.menu.RemoveItemAt(i);
            }
        }
        public void SetTarget(PrefabLibraryData data, List<LibraryTagData> tagRegistry)
        {
            if (data == null)
            {
                ClearInspector();
                return;
            }
                
            targetData = data;
            text = data.prefab.name;
            PreviewTexture = data.previewThumbnail;
            _newTagField.value = "";

            ClearTags();
            //Add tags
            if (data.tagGuids != null)
            {
                var tags = tagRegistry.Where(t => data.tagGuids.Contains(t.guid));
                SetTags(tags.ToArray());
            }

            SetUpToolbarMenu(data);
        }
        public void SetTarget(List<PrefabLibraryData> data, List<LibraryTagData> tagRegistry)
        {
            //Empty selection
            if (data.Count == 0)
            {
                ClearInspector();
                return;
            }

            //Single element list (single selection
            if (data.Count == 1)
            {
                SetTarget(data[0], tagRegistry);
                return;
            }
                
            //Multi selection
            text = $"{data.Count} Items selected";
            PreviewTexture = null;
            ClearTags();

            TagAnalysisResult tagAalysisResult = AnalyzeMultiTags(data);

            //Differing tags
            if (tagAalysisResult.HasDifferingTags)
            {
                var deletableTag = CreateDeletableTag(
                    "Differing Tags",
                    new Color(0.6f, 0.6f, 0.6f, 0.2f),
                    (ve) => {
                        _tagsContainer.Remove(ve);
                        onRemoveDifferingTagsGuid?.Invoke(tagAalysisResult.DifferingTagGuids);
                    },
                    null
                );
            }

            //Add SharedTags
            tagAalysisResult.SharedTagGuids.ToList().ForEach(sharedTag => AddTag(tagRegistry.Find(regTag => regTag.guid == sharedTag)));

            return;
        }

        public void ClearInspector()
        {
            targetData = null;
            text = "No items selected";
            PreviewTexture = null;
            ClearTags();
            SetUpToolbarMenu(null);
        }

        private void OnFieldKeyDown(KeyDownEvent evt)
        {
            // Check if the pressed key is the Enter key (Return key)
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                // Here you can implement your confirmation/submit logic
                // For example, get the current value of the text field and do something with it.
                var textField = evt.target as TextField; // evt.target is the element that triggered the event
                if (textField != null && !string.IsNullOrEmpty(textField.value))
                {
                    Debug.Log($"Enter key pressed! Submitted text: {textField.value}");
                    RegisterNewTagInput();
                }
                evt.StopPropagation(); // Stop the event from propagating further if you've handled it
            }
        }

        /// <summary>
        /// Invoke the register new tag event.
        /// </summary>
        private void RegisterNewTagInput()
        {
            var desiredName = _newTagField.value;

            if (string.IsNullOrEmpty(desiredName) || targetData == null || targetData.prefab == null)
                return;
            _newTagField.value = "";
            onRegisterNewTag?.Invoke(desiredName);
        }

        private void RemoveTagInput(LibraryTagData data, VisualElement ve)
        {
            _tagsContainer.Remove(ve);
            onRemoveTagGuid?.Invoke(data.guid);
        }
        public override VisualElement AddTag(LibraryTagData data)
        {
            var deletableTag = CreateDeletableTag(
                data.name,
                data.color,
                (ve) => RemoveTagInput(data, ve),
                () => onOpenTagGuid?.Invoke(data.guid)
                );

            deletableTag.name = data.guid;

            return deletableTag;
        }

        VisualElement CreateDeletableTag(string text, Color color, Action<VisualElement> deleteAction, Action openAction)
        {
            //Wrapper visual element
            VisualElement tag = new VisualElement();
            tag.style.backgroundColor = color;
            tag.AddToClassList(tagUssClassName);

            //Remove button
            var delBtn = new Button();
            delBtn.text = "-";
            delBtn.AddToClassList("tag-delete-button");
            tag.Add(delBtn);

            //Tag deletion event
            delBtn.clicked += () => deleteAction(tag);

            //Label
            Label tagLabel = new(text);
            tag.Add(tagLabel);

            //Tag opening event
            tag.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    openAction?.Invoke();
            });

            _tagsContainer.Add(tag);
            return tag;
        }


        /// <summary>
        /// Analyze the tags and tells us the shared, and differing tags. used for multiselection inspection.
        /// </summary>
        /// <param name="dataItems"></param>
        /// <returns></returns>
        public static TagAnalysisResult AnalyzeMultiTags(List<PrefabLibraryData> dataItems)
        {
            var result = new TagAnalysisResult();

            if (dataItems == null || !dataItems.Any())
            {
                // No items to analyze, return empty result
                return result;
            }

            // Step 1: Initialize with the tags of the first item.
            // This set will be intersected with subsequent items' tags.
            HashSet<string> potentialSharedTags = new HashSet<string>(dataItems.First().tagGuids);

            // Keep track of all unique tags encountered across all items for the 'differing' evaluation.
            HashSet<string> allTagsEncountered = new HashSet<string>(dataItems.First().tagGuids);

            // Step 2: Iterate through the rest of the items to find intersections and track all tags.
            for (int i = 1; i < dataItems.Count; i++)
            {
                var currentItemTags = dataItems[i].tagGuids;

                if (currentItemTags == null || !currentItemTags.Any())
                {
                    // If an item has no tags, then NO tags can be shared across ALL items.
                    potentialSharedTags.Clear();
                }
                else
                {
                    // Use a temporary HashSet to perform intersection efficiently.
                    HashSet<string> currentItemTagSet = new HashSet<string>(currentItemTags);
                    potentialSharedTags.IntersectWith(currentItemTagSet); // Keep only tags present in both current and previous shared sets

                    // Add current tags to the set of all encountered tags
                    allTagsEncountered.UnionWith(currentItemTagSet);
                }
            }

            // After the loop, potentialSharedTags contains only tags present in ALL items.
            result.SharedTagGuids = potentialSharedTags;

            // Step 3: Evaluate for differing tags.
            // Differing tags are those that are in 'allTagsEncountered' but NOT in 'sharedTagGuids'.
            // This covers tags present in some items but not all.
            result.DifferingTagGuids = new HashSet<string>(allTagsEncountered);
            result.DifferingTagGuids.ExceptWith(result.SharedTagGuids); // Remove shared tags

            result.HasDifferingTags = result.DifferingTagGuids.Any();

            return result;
        }
    }

    public class TagAnalysisResult
    {
        public HashSet<string> SharedTagGuids { get; set; } = new HashSet<string>();
        public HashSet<string> DifferingTagGuids { get; set; } = new HashSet<string>();
        public bool HasDifferingTags { get; set; }

    }
}