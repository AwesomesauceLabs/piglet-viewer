using System;
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
    public class GameManager : SingletonBehaviour<GameManager>
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
            // Set up callbacks for logging progress messages during
            // glTF imports. The default behaviour is to render
            // progress messages directly on top of the window using
            // IMGUI methods, whereas the WebGL build renders the progress
            // messages as HTML as part of the main web page,
            // outside of the Unity WebGL canvas.

            ProgressLog.Instance.AddLineCallback =
                Gui.Instance.AddProgressLogLine;
            ProgressLog.Instance.UpdateLineCallback =
                Gui.Instance.UpdateProgressLogLine;
            ProgressLog.Instance.ResetLogCallback =
                Gui.Instance.ResetProgressLog;

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
                    ProgressLog.Instance.OnImportProgress);

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
                    ProgressLog.Instance.OnImportProgress);

            StartImport(importTask, uri);
        }

        public void StartImport(GltfImportTask importTask, Uri uri)
        {
            ProgressLog.Instance.StartImport();

            string basename = Path.GetFileName(uri.ToString());
            string message = String.Format("Loading {0}...", basename);
            ProgressLog.Instance.AddLineCallback(message);

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
            Gui.Instance.ResetSpin();
            Gui.Instance.ResetFooterMessage();

            ProgressLog.Instance.AddLineCallback(
                String.Format("Total import time: {0} ms",
                    ProgressLog.Instance.Stopwatch.ElapsedMilliseconds));

            ProgressLog.Instance.AddLineCallback(
                String.Format("Longest Unity thread stall: {0} ms",
                    _importTask.LongestStepInMilliseconds()));

            ProgressLog.Instance.AddLineCallback("Success!");

            ModelManager.Instance.SetModel(model);

            _importTask = null;
        }

        /// <summary>
        /// Invoked when an exception is thrown during model import.
        /// </summary>
        public void OnImportException(Exception e)
        {
            if (e is Newtonsoft.Json.JsonException)
            {
                Gui.Instance.ShowDialogBox(
                    "Failed to Load Model",
                    StringUtil.WrapText("Sorry, the input file does not appear "
                        + "to be a valid glTF file (i.e. a .glb or .gltf file), "
                        + "nor is it a zip archive containing a valid glTF file.",
                        50));
            }
            else
            {
                Gui.Instance.ShowDialogBox("Failed to Load Model",
                    StringUtil.WrapText(e.Message, 50));
            }


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