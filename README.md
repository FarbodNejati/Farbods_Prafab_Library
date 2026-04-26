# About

This is a tool for managing and tagging your prefabs.
You can access the Library from `Toolbar > Tools > Prefab Library Explorer`.

![Application Screenshot](ReadmePreview.png)

## Tags in .Meta files
Tags are saved as *string GUIDs* within the userData of your prefab's **.meta file**. decoupling your prefabs from their meta files when moving them will result in loss of tag data, So when moving your prefabs, or syncing to you version control service of choice, make sure to also include your meta files.

## Tag Registry
After tag GUIDs are read from the meta file of your asset, the correct tag data is loaded from the **Settings** file's Tag registry, which is automatically created within `Assets/Settings`.
If unregistered tag GUIDs are found within your assets, they will be added to the Tag registry, where you can rename them and change their color.
if you move or rename your settings file, you can relocate it within the "Getting Started" window.




* If you rename or move your settings file, please close and reopen the Prefab Library window.
* The UI for this tool is made with the 2021.3.1f1 version of UIElements (UI Toolkit).
* by [Farbod Nejati](https://github.com/FarbodNejati)