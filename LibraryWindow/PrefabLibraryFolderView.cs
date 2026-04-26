using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Farbod.PrefabLibrary
{
    public class PrefabLibraryFolderView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PrefabLibraryFolderView, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public static readonly string ussClassName = "folder-view";
        public static readonly string toolbarUssClassName = ussClassName + "__toolbar";
        public static readonly string headerUssClassName = ussClassName + "__header";
        public static readonly string toolbarMenuUssClassName = toolbarUssClassName + "__menu";
        public static readonly string folderUssClassName = ussClassName + "__folder";
        public static readonly string expandableUssClassName = ussClassName + "__expandable-folder";
        public static readonly string selectedFolderUssClassName = folderUssClassName + "--selected";

        FolderData _rootFolder;
        Toolbar _toolbar;
        Label _rootFolderLabel;
        ToolbarMenu _toolbarMenu;
        ScrollView _scrollView;

        public event Action<string> onSelectPath;
        public event Action onClickRescan;
        public event Action onClickFullReIndex;
        public event Action onClickChangeRoot;


        VisualElement selectedFolderElement;
        FolderData selectedFolder;


        public PrefabLibraryFolderView()
        {
            AddToClassList(ussClassName);

            //Header
            var _header = new Toolbar();
            _header.AddToClassList(headerUssClassName);
            hierarchy.Add(_header);

            //Header icon
            var _headerIcon = new VisualElement();
            var iconImage = EditorGUIUtility.IconContent("d_Folder Icon").image;
            _headerIcon.style.backgroundImage = new(iconImage as Texture2D);
            _headerIcon.AddToClassList("folder-view__icon");
            _header.Add(_headerIcon);

            //Header label
            Label _headerLabel = new("Folder Viewer");
            _header.Add( _headerLabel);

            //Toolbar
            _toolbar = new();
            _toolbar.AddToClassList(toolbarUssClassName);
            hierarchy.Add(_toolbar);
			

            //Root folder label
            _rootFolderLabel = new("No Root Selected");
            _rootFolderLabel.AddToClassList(folderUssClassName);
            _rootFolderLabel.RegisterCallback<ClickEvent>(evt => OnClickFolder(evt, _rootFolder, _rootFolderLabel));

            _toolbar.Add(_rootFolderLabel);

            //Rescan button
            var _rescanButton = new ToolbarButton();
            _rescanButton.name = ussClassName+"_rescan-btn";
            _rescanButton.clicked += () => onClickRescan?.Invoke();
            _toolbar.Add(_rescanButton);

            // Rescan Icon
            Texture2D refreshIconImage = EditorGUIUtility.IconContent("Refresh").image as Texture2D;
            var _refreshIcon = new VisualElement();
            _refreshIcon.style.backgroundImage = new(refreshIconImage);
            _refreshIcon.AddToClassList("folder-view__icon");
            _rescanButton.Insert(0, _refreshIcon);

            //Rescan label
            Label _rescanLabel = new("Rescan");
            _rescanButton.Add(_rescanLabel);

            //Toolbar menu
            _toolbarMenu = new ToolbarMenu();
            _toolbarMenu.AddToClassList(toolbarMenuUssClassName);
            _toolbarMenu.menu.AppendAction("Change Root Directory", e => onClickChangeRoot?.Invoke());
            _toolbarMenu.menu.AppendAction("Full Reindex and Rescan", e => onClickFullReIndex?.Invoke());

            _toolbar.Add(_toolbarMenu);

            //List view
            _scrollView = new ScrollView();
            hierarchy.Add(_scrollView);
        }




        public void Populate(string rootDirectoryPath)
        {
            _scrollView.Clear();

            string rootFolderName = Path.GetFileName(rootDirectoryPath);
            if (string.IsNullOrEmpty(rootFolderName)) rootFolderName = rootDirectoryPath; // Fallback

            _rootFolder = new FolderData(rootFolderName, rootDirectoryPath);
            _rootFolderLabel.text = _rootFolder.AssetsRelativePath;

            // Load all subfolders starting from the root path
            _rootFolder.LoadSubfolders();

            // Populate the ListView
            HandleSubfoldersRecursion(_rootFolder, _scrollView.contentContainer, 0);
        }

        void HandleSubfoldersRecursion(FolderData thisFolder, VisualElement parentElement, int indentLevel)
        {
            if (thisFolder.Subfolders.Count == 0)
                return;

            List<FolderData> Subfolders = thisFolder.Subfolders;

            for (int i = 0; i < Subfolders.Count; i++)
            {
                //Unicode stylization:

                string unicodeStylizer = "└─";
                if (i < Subfolders.Count - 1)
                    unicodeStylizer = "├─";


                AddFolder(Subfolders[i], parentElement, indentLevel, unicodeStylizer);
            }
        }
        void AddFolder(FolderData currentFolder, VisualElement parentElement, int indentLevel, string prefixUnicodeStylizer = "└─")
        {
            //Label with display name
            string displayName = prefixUnicodeStylizer + " " + currentFolder.Name;
            var label = new Label(displayName);
            parentElement.Add(label);

            label.AddToClassList(folderUssClassName);
            label.style.marginLeft = indentLevel * 12;

            //Click callback
            label.RegisterCallback<ClickEvent>(evt => OnClickFolder(evt, currentFolder, label));


            //Recursion for folders that have subfolders.
            HandleSubfoldersRecursion(currentFolder, _scrollView.contentContainer, indentLevel + 1);
        }
        void OnClickFolder(ClickEvent evt, FolderData folder, VisualElement ve)
        {
            //Deselect folder
            if (selectedFolder == folder)
            {
                onSelectPath?.Invoke("");

                selectedFolderElement.RemoveFromClassList(selectedFolderUssClassName);
                selectedFolder = null;
                selectedFolderElement = null;
            }
            //Select folder
            else
            {
                onSelectPath?.Invoke(folder.AssetsRelativePath);

                if (selectedFolderElement != null)
                    selectedFolderElement.RemoveFromClassList(selectedFolderUssClassName);
                ve.AddToClassList(selectedFolderUssClassName);

                selectedFolder = folder;
                selectedFolderElement = ve;
            }

            evt.StopImmediatePropagation();
        }
    }

}