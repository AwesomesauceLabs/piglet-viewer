﻿using System;
using System.IO;
using Piglet;
using UnityEngine;
using UnityGLTF;

namespace PigletViewer
{
    /// <summary>
    /// Singleton that controls overall application behaviour.
    /// The main responsibility of this class is to start glTF
    /// import tasks, track their progress, and appropriately
    /// handle import errors/successes. At most one glTF import
    /// task can be running at any given time. In addition,
    /// this class handles loading of MonoBehaviours for platform-specific
    /// application behaviour (e.g. AndroidGameManager) at
    /// startup time.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        /// <summary>
        /// Handle to the currently running glTF import task.
        /// This task runs in the background and is
        /// incrementally advanced by calling
        /// `MoveNext` in `Update`.
        /// </summary>
        private GltfImportTask _importTask;

        /// <summary>
        /// Unity callback that is invoked before the first frame update
        /// and prior to Start().
        /// </summary>
        private void Awake()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            gameObject.AddComponent<WindowsGameManager>();
#elif UNITY_ANDROID
        gameObject.AddComponent<AndroidGameManager>();
#elif UNITY_WEBGL
        gameObject.AddComponent<WebGlGameManager>();
#endif
        }

        /// <summary>
        /// Create a glTF import task, which will be incrementally advanced
        /// in each call to Update().
        ///
        /// This version of StartImport is only used by the WebGL viewer.
        /// </summary>
        public void StartImport(byte[] data, string filename)
        {
            Uri uri = new Uri(filename, UriKind.Relative);

            GltfImportTask importTask = RuntimeGltfImporter
                .GetImportTask(data,
                    ImportLog.Instance.OnImportProgress);

            StartImport(importTask, uri);
        }

        /// <summary>
        /// Create a glTF import task, which will be incrementally advanced
        /// in each call to Update().
        ///
        /// Note that the URI argument must be passed in as a string
        /// rather than a `Uri` object, so that this method
        /// can be invoked from javascript.
        /// </summary>
        /// <param name="uriStr">The URI of the input glTF file.</param>
        public void StartImport(string uriStr)
        {
            Uri uri = new Uri(uriStr);

            GltfImportTask importTask = RuntimeGltfImporter
                .GetImportTask(uri,
                    ImportLog.Instance.OnImportProgress);

            StartImport(importTask, uri);
        }

        public void StartImport(GltfImportTask importTask, Uri uri)
        {
            ImportLog.Instance.StartImport();

            string basename = Path.GetFileName(uri.ToString());
            string message = String.Format("Loading {0}...", basename);
            ImportLog.Instance.AddLine(message);

            importTask.OnCompleted += OnImportCompleted;
            importTask.OnException += OnImportException;
            importTask.RethrowExceptionAfterCallbacks = false;

            _importTask = importTask;
        }

        /// <summary>
        /// Invoked after a model has been successfully imported.
        /// </summary>
        public void OnImportCompleted(GameObject model)
        {
            ViewerGUI.Instance.ResetSpin();
            ViewerGUI.Instance.ResetFooterMessage();

            ImportLog.Instance.AddLine(
                String.Format("Total import time: {0} ms",
                    ImportLog.Instance.Stopwatch.ElapsedMilliseconds));

            ImportLog.Instance.AddLine(
                String.Format("Longest Unity thread stall: {0} ms",
                    _importTask.LongestStepInMilliseconds()));

            ImportLog.Instance.AddLine("Success!");

            ModelManager.Instance.SetModel(model);

            _importTask = null;
        }

        /// <summary>
        /// Invoked when an exception is thrown during model import.
        /// </summary>
        public void OnImportException(Exception e)
        {
            ViewerGUI.Instance.ShowDialogBox("Failed to Load Model",
                StringUtil.WrapText(e.Message, 50));

            _importTask = null;
        }

        /// <summary>
        /// Unity callback that is invoked once per frame.
        /// </summary>
        public void Update()
        {
            // advance import job
            _importTask?.MoveNext();
        }

    }
}