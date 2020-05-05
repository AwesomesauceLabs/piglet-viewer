#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using System;
using System.Collections.Generic;
using System.IO;
using B83.Win32;
using Piglet;
using UnityEngine;
using UnityGLTF;

/// <summary>
/// Implements PigletViewer behaviour that is specific to the Win64 platform,
/// such as drag-and-drop and parsing of command-line arguments.
/// </summary>
public class WindowsViewerBehaviour : MonoBehaviour
{
    /// <summary>
    /// Drag-and-drop implementation for Unity player on Windows.
    /// </summary>
    UnityDragAndDropHook _dragAndDropHook;

    /// <summary>
    /// Unity callback that is invoked before the first frame update.
    /// Here we initialize drag-and-drop, parse command line arguments,
    /// and start the initial glTF model import (if any).
    /// </summary>
    void Start()
    {
        _dragAndDropHook = new UnityDragAndDropHook();
        _dragAndDropHook.InstallHook();
        _dragAndDropHook.OnDroppedFiles += OnDropFiles;

        ViewerGUI.Instance.DefaultFooterMessage
            = "drag .gltf/.glb/.zip onto window to view";
        ViewerGUI.Instance.ResetFooterMessage();
        
        // By default, the Windows Unity Player will pause
        // execution when it loses focus.  Setting
        // `Application.runInBackground` to true overrides
        // this behaviour and tells it to keep running
        // always.
        //
        // The player must be continuously running in
        // order for drag-and-drop of files to work in
        // an intuitive manner.  Otherwise, dropping
        // a .gltf/.glb file onto a non-focused player
        // window will not immediately trigger an import,
        // and the user will have to additionally click
        // the window to give it focus again, before the
        // glTF import will start running.
        
        Application.runInBackground = true;
        
        ParseCommandLineArgs();
    }

    /// <summary>
    /// Parse command line arguments and start initial model
    /// import (if any).
    /// </summary>
    private void ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        
        bool profile = false;
        bool quitAfterLoad = false;
        long delayLoadMilliseconds = 0;
        
        // default model to load at startup, unless
        // --load or --no-load is used

        Uri uri = new Uri(Path.Combine(Application.streamingAssetsPath,
            "piglet-1.0.0.glb"));
        
        GltfImportTask importTask = RuntimeGltfImporter
            .GetImportTask(uri,
                ImportLog.Instance.OnImportProgress);

        for (int i = 0; i < args.Length; ++i)
        {
            if (args[i] == "--delay-load")
            {
                // Delay initial model import at startup.
                // I added this option so that I could prevent
                // Unity player loading/initialization from affecting
                // my profiling results. 
                delayLoadMilliseconds = Int64.Parse(args[i + 1]);
            }
            else if (args[i] == "--load")
            {
                // Specify a model to load at startup,
                // in place of the default Piglet model.
                uri = new Uri(args[i + 1]);
                importTask = RuntimeGltfImporter
                    .GetImportTask(uri,
                        ImportLog.Instance.OnImportProgress);
            }
            else if (args[i] == "--no-load")
            {
                // Don't load a model at startup.
                importTask = null;
            }
            else if (args[i] == "--profile")
            {
                // Record and log profiling results while
                // importing the initial model. This option times
                // IEnumerator.MoveNext() calls and identifies
                // which import subtasks cause the longest
                // interruptions the main Unity thread.
                profile = true;
            }
            else if (args[i] == "--quit-after-load")
            {
                // Exit the viewer immediately after loading
                // the initial model. This option is usually
                // used in conjunction with --profile to
                // perform automated profiling from the command
                // line.
                quitAfterLoad = true;
            }
        }

        if (importTask == null)
            return;

        if (delayLoadMilliseconds > 0)
            importTask.PushTask(SleepUtil.SleepEnum(
                delayLoadMilliseconds));
            
        if (profile)
            importTask.OnCompleted += _ => importTask.LogProfilingData();

        if (quitAfterLoad)
            importTask.OnCompleted += _ => Application.Quit(0);

        GameManager.Instance.StartImport(importTask, uri);
    }

    /// <summary>
    /// Unity callback that is invoked when this MonoBehaviour is destroyed
    /// (e.g. exiting Play mode).
    /// 
    /// It is important to tear down drag-and-drop here, otherwise
    /// we will have dangling memory pointers and will get a segfault when we exit
    /// Play Mode, which crashes the Unity Editor.
    /// </summary>
    void OnDestroy()
    {
        _dragAndDropHook.UninstallHook();
    }

    /// <summary>
    /// Callback for files that are drag-and-dropped onto the game
    /// window.
    /// </summary>
    void OnDropFiles(List<string> paths, POINT mousePos)
    {
        GameManager.Instance.StartImport(paths[0]);
    }
}

#endif
