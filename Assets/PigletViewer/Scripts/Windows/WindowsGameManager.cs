#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.IO;
using B83.Win32;
using Piglet;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// Implements PigletViewer behaviour that is specific to the Win64 platform,
    /// such as drag-and-drop and parsing of command-line arguments.
    /// </summary>
    public class WindowsGameManager : MonoBehaviour
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

            Gui.Instance.DefaultFooterMessage
                = "drag .gltf/.glb/.zip onto window to view";
            Gui.Instance.ResetFooterMessage();

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
}

#endif
