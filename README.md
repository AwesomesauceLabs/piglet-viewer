# Table of Contents

* [Description](#description)
* [Live Demo](#live-demo)
* [Build Instructions](#build-instructions)
  * [Unity Project Setup](#unity-project-setup)
  * [Android Build Instructions](#android-build-instructions)
  * [WebGL Build Instructions](#webgl-build-instructions)
  * [Windows Build Instructions](#windows-build-instructions)
* [Navigating the Source Code](#navigating-the-source-code)
* [Licenses and Attributions](#licenses-and-attributions)
* [Footnotes](#footnotes)

# Description

![PigletViewer Demo GIF](scifi-gun.gif)
<br>
*Viewing ["SciFi Gun"](https://sketchfab.com/3d-models/scifi-gun-04a9f3ccb5b14dc38a28b27c1916e18e) by [mrfetch@sketchfab](https://sketchfab.com/mrfetch) in the [live PigletViewer demo](https://awesomesaucelabs.github.io/piglet-webgl-demo/).*

PigletViewer is a Unity application which uses the [Piglet glTF Importer](https://assetstore.unity.com/packages/slug/173425) to load and view 3D models from glTF files (`.gltf`, `.glb`, or `.zip`)<sup>[1](#footnote1)</sup>. It is designed to run on multiple platforms and currently supports builds for Android, WebGL, and Windows (see [Build Instructions](#build-instructions)).

PigletViewer is provided as an example application for customers of the [Piglet glTF Importer](https://assetstore.unity.com/packages/slug/173425). As such, building the application requires purchasing and installing Piglet from the Unity Asset Store. For new users of Piglet, I recommend watching the [Runtime Import Tutorial video](https://youtu.be/f66wmgSTPI0) and/or reading the [Runtime Import Tutorial section of the manual](https://awesomesaucelabs.github.io/piglet-manual/#runtime-import-tutorial) before exploring the PigletViewer code, as the tutorial provides a much quicker and simpler introduction to the API.

# Live Demo

A live demo for the WebGL version of PigletViewer is available at: [https://awesomesaucelabs.github.io/piglet-webgl-demo/](https://awesomesaucelabs.github.io/piglet-webgl-demo/). I have tested the demo in Firefox and Google Chrome<sup>[2](#footnote2)</sup>, on Windows 10 64-bit.

# Build Instructions

## Unity Project Setup

To set up the Unity project for PigletViewer, I recommend the following steps:

1. Create a new project using Unity version 2018.4 or newer.
2. Purchase and install the [Piglet glTF Importer](https://assetstore.unity.com/packages/slug/173425) asset from the Unity Asset Store.
3. Download the latest `.unitypackage` for PigletViewer from the [releases page](https://github.com/AwesomesauceLabs/piglet-viewer/releases), then unpack the `.unitypackage` into your project from `Assets -> Import Package -> Custom Package...` in the Unity menu. 

I recommend installing from the `.unitypackage` rather than doing a `git clone` because the project in this repo is tied to Unity version 2018.4, whereas the `.unitypackage` will also work with newer versions of Unity.

## Android Build Instructions

Before you can build Android apps in Unity, you will need to install the **Android Build Support** module for Unity, the Android Software Development Kit (SDK), the Android Native Development Kit (NDK), and a Java Development Kit (e.g. OpenJDK). Unity Hub can download and install all of this software for you in a few simple steps. See [Android environment setup]( https://docs.unity3d.com/Manual/android-sdksetup.html) for further details.

Once you have completed the basic setup for Android development, you can build the PigletViewer app by the following steps:

1. Double-click `Assets/PigletViewer/Scenes/MainScene.unity` to open the PigletViewer scene.
2. Click `File -> Build Settings...` in the Unity menu.
3. Click `Android` on the left side of the `Build Settings` dialog. If there is a `Build` button in the bottom right corner, then Android is already the active build target. If not, click `Switch Platform` to make Android the active build target.
4. Click `Add Open Scenes` in the top right corner to add the current scene to the build.
5. Ensure that you have enabled [developer options](https://developer.android.com/studio/debug/dev-options) on your Android device, then connect your Android device to your computer with a USB cable.
6. Click the `Build and Run` button in the bottom right corner.
7. Select a location to save the `.apk` file and click `OK` to start the build.

The app will automatically open on your Android phone/tablet once the build has completed, although you may have to wake/unlock your Android device before you see it.

You can open different glTF models on Android by opening `.glb` files in a file browser app<sup>[3](#footnote3)</sup>.

## WebGL Build Instructions

Before you can build WebGL apps, you will need to install the **WebGL Build Support** module for Unity. Unity Hub can download and install this module for you in a few simple steps. See [Adding modules to the Unity Editor]( https://docs.unity3d.com/Manual/GettingStartedAddingEditorComponents.html) for details.

Once you have installed WebGL Build Support, you can build the WebGL version of PigletViewer by the following steps:

1. Change the active WebGL Template under `Edit => Project Settings... => Player => WebGL settings tab => Resolution and Presentation => WebGL Template`. If you are using Unity 2018 or Unity 2019, use the `Piglet2018` template.  If you are using Unity 2020 or newer, use the `Piglet2020` template<sup>[4](#footnote4)</sup>.
2. Double-click `Assets/PigletViewer/Scenes/MainScene.unity` to open the PigletViewer scene.
3. Click `File => Build Settings...` in the Unity menu.
4. Click `WebGL` on the left side of the `Build Settings` dialog. If there is a `Build` button in the bottom right corner, then WebGL is already the active build target. If not, click `Switch Platform` to make WebGL the active build target.
5. Click `Add Open Scenes` in the top right corner to add the current scene to the build.
6. Click `Build and Run` in the bottom right corner.
7. Select an output directory for the WebGL build and click `OK`.

Once the build has completed, Unity will open the WebGL version of PigletViewer in your default web browser.

## Windows Build Instructions

You can build the Windows version of PigletViewer by the following steps:

1. Double-click `Assets/PigletViewer/Scenes/MainScene.unity` to open the PigletViewer scene.
2. Click `File -> Build Settings...` in the Unity menu.
3. Click `PC, Mac & Linux Standalone` on the left side of the `Build Settings` dialog. If there is a `Build` button in the bottom right corner, then PC/Mac/Linux is already the active build target. If not, click `Switch Platform` to make PC/Mac/Linux the active build target.
4. Click `Add Open Scenes` in the top right corner to add the current scene to the build.
5. Click `Build And Run` in the bottom right corner.
6. Select a location to save the build files and click `OK` to start the build.

Once the build has completed, the PigletViewer application will open on your Windows desktop. To view glTF models in the Windows version of PigletViewer, drag-and-drop `.gltf`/`.glb`/`.zip` files from Windows File Explorer onto the PigletViewer window.

## Navigating the Source Code

The best starting point for understanding the PigletViewer code is the `GameManager` class in [`Assets/PigletViewer/Scripts/GameManager.cs`](Assets/PigletViewer/Scripts/GameManager.cs). This class is responsible for starting glTF import tasks and for setting up callbacks that track import progress, handle import errors, and handle successful completion of a glTF import. `GameManager` also handles running scripts that implement platform-specific behaviour, namely `AndroidGameManager`, `WebGlGameManager`, and `WindowsGameManager`.  Depending on the target platform of your game/application, it may be useful to look at those classes as well.

# Licenses and Attributions

The PigletViewer code in this repo is released under an [MIT license](LICENSE).

This repo includes the code for [UnityWindowsFileDragAndDrop](https://github.com/Bunny83/UnityWindowsFileDrag-Drop) under `Assets/PigletViewer/Dependencies/UnityWindowsFileDragDrop`, which also has an [MIT license](Assets/PigletViewer/Dependencies/UnityWindowsFileDragDrop/LICENSE).

Building PigletViewer requires purchasing and installing the [Piglet glTF Importer](https://assetstore.unity.com/packages/slug/173425) source code separately from the Unity Asset Store. Piglet is licensed under the [Unity Asset Store End User License Agreement (EULA)](https://unity3d.com/legal/as_terms). Briefly, this means that you are free use Piglet in your applications and games (commercial or otherwise), but you are not allowed to redistribute the source code.

The "Sir Piggleston" model included at `Assets/StreamingAssets/piggleston.glb` is a trademark for the [Piglet glTF Importer](https://assetstore.unity.com/packages/slug/173425). Please contact `awesomesaucelabs` at gmail for licensing terms.

This repo includes several sample glTF models under `Assets/StreamingAssets`, which have been published by various artists on [Sketchfab](https://sketchfab.com) under the [CC Attribution License](https://creativecommons.org/licenses/by/4.0/):

| Name of Work | Author | License | File Path |
| ------------ | ------ | ------- | --------- |
| ["SciFi Gun"](https://sketchfab.com/3d-models/scifi-gun-04a9f3ccb5b14dc38a28b27c1916e18e) | [mrfetch@sketchfab](https://sketchfab.com/mrfetch) |  [CC Attribution License](https://creativecommons.org/licenses/by/4.0/) | `Assets/StreamingAssets/scifi_gun.zip` |
| ["Skull Salazar"](https://sketchfab.com/3d-models/scifi-gun-04a9f3ccb5b14dc38a28b27c1916e18e) | [jvitorsouzadesign@sketchfab](https://sketchfab.com/jvitorsouzadesign) | [CC Attribution License](https://creativecommons.org/licenses/by/4.0/) | `Assets/StreamingAssets/skull_salazar.zip` |
| ["Viking Room"](https://sketchfab.com/3d-models/viking-room-a49f1b8e4f5c4ecf9e1fe7d81915ad38) | [nigelgoh@sketchfab](https://sketchfab.com/nigelgoh) | [CC Attribution License](https://creativecommons.org/licenses/by/4.0/) | `Assets/StreamingAssets/viking_room.zip` |

# Footnotes

<a name="footnote1">1</a>. The WebGL build can only load models from `.glb` and `.zip` files (not `.gltf` files). The main issue with loading `.gltf` files in a web browser is that they typically reference other files on the user's hard drive (e.g. PNG files for textures), and web browsers aren't allowed to load arbitrary files from the user's hard drive, for security reasons.  There is a similar issue for opening `.gltf` files on Android<sup>[3](#footnote3)</sup>.

<a name="footnote2">2</a>. Performance of PigletViewer in Google Chrome can be greatly improved by [enabling hardware acceleration](TODO) (i.e. GPU acceleration) in the browser settings. (This option is currently disabled by default in Chrome.)

<a name="footnote3">3</a>. Opening `.gltf` files from file browser apps does not work on Android. The main problem is that the file browser apps send PigletViewer an opaque [content URI]( https://developer.android.com/guide/topics/providers/content-provider-basics#ContentURIs) for the input `.gltf` file, rather than a file path.  This means that PigletViewer cannot resolve relative file paths used inside the `.gltf` file (e.g. paths to PNG files). There is a similar issue for opening `.gltf` files in the WebGL build<sup>[1](#footnote1)</sup>.

<a name="footnote4">4</a>. There are substantial changes to WebGL builds in Unity 2020, which require using a different WebGL template (`Piglet2020`). For a detailed discussion of WebGL-related changes in Unity 2020, see:  [https://forum.unity.com/threads/changes-to-the-webgl-loader-and-templates-introduced-in-unity-2020-1.817698/"](https://forum.unity.com/threads/changes-to-the-webgl-loader-and-templates-introduced-in-unity-2020-1.817698/)
