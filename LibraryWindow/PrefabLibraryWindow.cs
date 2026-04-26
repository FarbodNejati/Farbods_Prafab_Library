using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Farbod.PrefabLibrary
{
    /// <summary>
    /// The main PropLibrary editor window.
    /// </summary>
    public class PrefabLibraryWindow : EditorWindow
    {
        public VisualTreeAsset windowTreeAsset;
        public StyleSheet windowStyleSheet;

        private VisualElement _root;
        private PrefabInspectorView _inspector;


        private PrefabLibraryScrollView _library_scroll_view;
        private ToolbarMenu _toolbar_viewMode;
        private ToolbarBreadcrumbs _toolbar_path_breadcrumbs;
        private ToolbarPopupSearchField _toolbar_searchField;
        private PrefabLibraryFolderView _library_folder_view;
        private PrefabLibraryScanner _scanner;

        private VisualElement _get_started_window, _relocate_settings_window;
        private Button _get_started_button, _relocate_settings_button;
        /// <summary>
        /// The active settings asset.
        /// </summary>
        private PrefabLibrarySettings Settings => PrefabLibrarySettings.Instance;
        string LibraryDirectoryRoot => Settings?.libraryDirectoryRoot??"";

        /// <summary>
        /// Sets the viewmode on the scroll view, and saves it to our settings.
        /// </summary>
        PrefabLibraryScrollView.ViewMode ViewMode
        {
            set
            {
                Settings.viewMode = value;
                EditorUtility.SetDirty(Settings);
                _library_scroll_view.SetViewMode(value);
            }
        }

        /// <summary>
        /// The menu item available in the editor toolbar for opening this window.
        /// </summary>
        [MenuItem("Tools/Prefab Library Explorer")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<PrefabLibraryWindow>();
            var icon = EditorGUIUtility.IconContent("d_FilterByLabel").image;
            wnd.titleContent = new GUIContent("Prefab Library", icon);
            wnd.minSize = new(500, 400);
        }

        /// <summary>
        /// The main function that is called when this window is created.
        /// </summary>
        public void CreateGUI()
        {

            // Each editor window contains a root VisualElement object
            _root = rootVisualElement;

            // Import UXML
            //var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/PrefabLibrary/LibraryWindow/PrefabLibraryWindow.uxml");
            var visualTree = windowTreeAsset;
            VisualElement visualTreeInstance = visualTree.Instantiate();
            visualTreeInstance.style.flexGrow = 1;
            _root.Add(visualTreeInstance);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            //var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/PrefabLibrary/LibraryWindow/PrefabLibrary.uss");

            _root.styleSheets.Add(windowStyleSheet);

            
            PrefabLibrarySettings.onDirectoryChange += (p) =>
            {
                if (_scanner == null)
                    return;

                Rescan();
                _scanner.UpdatePath(p);
                _library_folder_view.Populate(p);
            };

            GetElements(_root);

            if (CheckStartUp(()=> Initialize()))
            {
                Initialize();
                Rescan();
            }


            //_scanner = new();
            //GetElements(_root);
            //RegisterCallbacks();

            //if (CheckSettingsInitialization())
            //    Rescan();
            //Rescan();
        }
        private void Initialize()
        {
            _scanner = new(LibraryDirectoryRoot);
            _library_scroll_view.SetViewMode(Settings.viewMode);
            RegisterCallbacks();
            Rescan();
        }
        /// <summary>
        /// Query and cache elements
        /// </summary>
        /// <param name="root"></param>
        private void GetElements(VisualElement root)
        {
            _library_scroll_view = root.Q<PrefabLibraryScrollView>();

            _toolbar_viewMode = root.Q<ToolbarMenu>("library_toolbar__viewMode");
            _toolbar_searchField = root.Q<ToolbarPopupSearchField>("library_toolbar__search");
            _toolbar_path_breadcrumbs = root.Q<ToolbarBreadcrumbs>("proplibrary_library__path-crumbs");

            _inspector = root.Q<PrefabInspectorView>();

            _library_folder_view = root.Q<PrefabLibraryFolderView>();

            _get_started_window = root.Q("get-started-container");
            _get_started_button = root.Q<Button>("get-started-btn");
            _relocate_settings_window = root.Q("relocate-settings-container");
            _relocate_settings_button = root.Q<Button>("relocate-settings-btn");
        }

        /// <summary>
        /// Register callbacks on elements
        /// </summary>
        private void RegisterCallbacks()
        {
            _library_scroll_view.onSelectionChange += OnScrollSelectionChange;


            _inspector.onRemoveTagGuid += guid => HandleTagRemoval(_library_scroll_view.SelectedData, guid);
            _inspector.onRegisterNewTag += tagName => HandleTagAddition(_library_scroll_view.SelectedData, tagName);
            _inspector.onOpenTagGuid += tagName =>
            {
                UnityEditor.Selection.activeObject = Settings;
            };
            _inspector.onRemoveDifferingTagsGuid += differingTags =>
            {
                foreach (var tag in differingTags)
                {
                    HandleTagRemoval(_library_scroll_view.SelectedData, tag);
                }
            };

            //Folder view - rescan button
            _library_folder_view.onClickRescan += Rescan;
            //Folder view - change root button
            _library_folder_view.onClickChangeRoot += () =>
            {
                if(Settings)
                    Settings.SelectRootDirectory();
            };
            _library_folder_view.onClickFullReIndex += () =>
            {
                _scanner.UpdateTagIndex();
                Rescan();
                UpdatePathBreadcrumbs("");
            };
            _library_folder_view.onSelectPath += (path) => OpenDirectory(path);


            //View mode
            _toolbar_viewMode.menu.AppendAction("Cards", new(x =>
            {
                ViewMode = PrefabLibraryScrollView.ViewMode.cards;
            }));
            _toolbar_viewMode.menu.AppendAction("Rect List", new(x =>
            {
                ViewMode = PrefabLibraryScrollView.ViewMode.rectList;
            }));
            _toolbar_viewMode.menu.AppendAction("Compact List", new(x =>
            {
                ViewMode = PrefabLibraryScrollView.ViewMode.compactList;
            }));


            //Search field
            _toolbar_searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
            //Search field cancel button
            _toolbar_searchField.Q<Button>(className: ToolbarSearchField.cancelButtonUssClassName).clicked += () => OpenDirectory("");
        }


        /// <summary>
        /// Open directory at relative path to the project folder.
        /// </summary>
        /// <param name="path">path</param>
        private void OpenDirectory(string path)
        {
            var filterResult = _scanner.FilterByPath(_scanner.fullLibrary, path);
            _library_scroll_view.PopulateWithItems(filterResult);
            _toolbar_searchField.value = "";
            //Toolbar path breadcrumbs
            UpdatePathBreadcrumbs(path);
        }

        /// <summary>
        /// Perform a full rescan
        /// </summary>
        private void Rescan()
        {
            _scanner.Scan();

            _library_folder_view.Populate(LibraryDirectoryRoot);

            _library_scroll_view.UpdateTagIndex(Settings.RegisteredTags);
            _library_scroll_view.PopulateWithItems(_scanner.fullLibrary.ToList());

            UpdatePathBreadcrumbs("");
        }

        /// <summary>
        /// When a key is pressed in the search bar. search when the key is enter.
        /// </summary>
        /// <param name="evt"></param>
        private void OnSearchKeyDown(KeyDownEvent evt)
        {
            //Check if the key code matches either the enter(return) key or keypad return key.
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                string prompt = _toolbar_searchField.value;
                var searchResult = _scanner.FilterBySearchPrompt(_scanner.fullLibrary, _toolbar_searchField.value);
                _library_scroll_view.PopulateWithItems(searchResult);

                UpdatePathBreadcrumbs("");
                _toolbar_path_breadcrumbs.PushItem("Search: " + _toolbar_searchField.value);
            }
        }

        /// <summary>
        /// When a new item is selected withing the scroll view
        /// </summary>
        /// <param name="list"></param>
        private void OnScrollSelectionChange(List<PrefabLibraryData> list)
        {
            _inspector.SetTarget(list, Settings.RegisteredTags);
        }

        private void HandleTagRemoval(List<PrefabLibraryData> assetList, string tagGuid)
        {
            var asstsWithTag = assetList.FindAll(i => i.tagGuids.Contains(tagGuid));
            foreach (PrefabLibraryData data in asstsWithTag)
            {
                //Get all tag guids on object
                var tags = AssetTaggerUtility.GetAssignedTags(data.assetPath);
                //Remove target tag guid
                tags = tags.Where(t => t != tagGuid).ToArray();
                //Rewrite tag guids on object
                AssetTaggerUtility.SetAssignedTags(data.assetPath, tags);

                data.tagGuids.Remove(tagGuid);

                _scanner.RemoveTagFromAsset(data, tagGuid);
            }

            _library_scroll_view.RemoveTagFromExistingItems(tagGuid, asstsWithTag);
            _inspector.RemoveTag(tagGuid);
        }

        private void HandleTagAddition(List<PrefabLibraryData> selectedData, string tagName)
        {
            //First, attempt to find a tag within the registry, with the same name.
            LibraryTagData tagData = Settings.RegisteredTags.Find(t => t.name == tagName);
            //If this tag dosnt exist in the registry, register it.
            if (tagData == null)
                RegisterNewTagInRegistry(tagName, out tagData);

            var asstsMissingTag = selectedData.FindAll(i => !i.tagGuids.Contains(tagData.guid));
            //Add this tag to all items which lack this tag.
            foreach (var data in asstsMissingTag)
            {
                //Get all tag guids on object and add the new tag
                var tags = AssetTaggerUtility.GetAssignedTags(data.assetPath)?.Append(tagData.guid) ?? new string[1] { tagData.guid };
                //Rewrite tag guids on object
                AssetTaggerUtility.SetAssignedTags(data.assetPath, tags.ToArray());

                data.tagGuids.Add(tagData.guid);

                _scanner.AddTagToAsset(data, tagData);
            }
            _library_scroll_view.AddTagToExistingItems(tagData, asstsMissingTag);
            _inspector.AddTag(tagData);
        }

        /// <summary>
        /// Register a new tag in the library's settings
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The index of the new tag, in the registry</returns>
        private int RegisterNewTagInRegistry(string name, out LibraryTagData data)
        {
            data = new(name, new(0f, 0.4f, 0.2f, 0.2f));
            Settings.RegisteredTags.Add(data);
            _scanner.UpdateTagIndex();
            return Settings.RegisteredTags.Count - 1;
        }

        void UpdatePathBreadcrumbs(string path)
        {
            _toolbar_path_breadcrumbs.Clear();

            //Button for accessing root directory
            _toolbar_path_breadcrumbs.PushItem("All Folders");
            _toolbar_path_breadcrumbs.Q<ToolbarButton>().clicked += () => OpenDirectory("");

            if (!string.IsNullOrEmpty(path))
            {
                var initialPathSegments = path.Split('/');
                var rootPrePath = Settings.libraryDirectoryRoot.Split('/').ToList();
                rootPrePath.RemoveAt(rootPrePath.Count - 1);

                //Remove the first segments from root path, otehr then the root folder itself
                var crumbItems = initialPathSegments.TakeLast(initialPathSegments.Length - rootPrePath.Count).ToList();

                crumbItems.ForEach(s => _toolbar_path_breadcrumbs.PushItem(s));

                //Click Events
                var crumbs = _toolbar_path_breadcrumbs.Query<ToolbarButton>(className: ToolbarBreadcrumbs.itemClassName).ToList();

                string pathToCrumbDestination = string.Join('/', rootPrePath);
                for (int i = 1; i < crumbs.Count; i++)
                {
                    pathToCrumbDestination += "/" + crumbs[i].text;
                    string thisPath = pathToCrumbDestination;
                    crumbs[i].clicked += () => OpenDirectory(thisPath);
                }
            }
        }

        bool CheckStartUp(Action onCompletion)
        {
            //If settings is located && a valid root path is set in the settings, stop, as things are working well.
            if (Settings != null && AssetDatabase.IsValidFolder(Settings.libraryDirectoryRoot)){
                _get_started_window.SetEnabled(false);
                return true;
            }
            SetControlsActive(false);


            //Getting started
            _get_started_button.clicked += () =>
            {
                //Ensure the existance of a settings file
                PrefabLibrarySettings.LocateOrCreateSettingsAsset();

                bool pathSuccessful = Settings.SelectRootDirectory();
                if (pathSuccessful)
                {
                    SetControlsActive(true);
                    onCompletion?.Invoke();
                }
                    
            };
            _relocate_settings_button.clicked += () =>
            {
                bool relocateSuccessful = PrefabLibrarySettings.RelocateSettings();
                if (relocateSuccessful && CheckStartUp(onCompletion))
                {
                    SetControlsActive(true);
                    onCompletion?.Invoke();
                }
            };

            return false;
        }

        void SetControlsActive(bool active)
        {
            _get_started_window.SetEnabled(!active);
            _library_scroll_view.SetEnabled(active);
            _relocate_settings_window.SetEnabled(active?false:Settings == null);

            _inspector.SetEnabled(active);
            _toolbar_viewMode.SetEnabled(active);
            _toolbar_searchField.SetEnabled(active);
            _toolbar_path_breadcrumbs.SetEnabled(active);
            _library_folder_view.SetEnabled(active);
        }
    }

}