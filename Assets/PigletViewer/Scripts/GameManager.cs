using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Piglet;
using UnityEngine;
using NDesk.Options;

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
        /// Options for glTF import behaviour.
        /// Currently this just controls whether
        /// the imported model is automatically
        /// scaled to a user-specified size.
        /// </summary>
        public GltfImportOptions ImportOptions;

        /// <summary>
        /// List of queued glTF import tasks.
        /// Each task runs in the background and is
        /// executed incrementally by calling
        /// `MoveNext` in `Update`.
        /// </summary>
        private List<GltfImportTask> _importTasks;

        /// <summary>
        /// If true, print a TSV table of profiling data
        /// to the debug log after each glTF import. This
        /// option is enabled by the `--profile` command line
        /// option.
        /// </summary>
        private bool _logProfilingData;

        /// <summary>
        /// Unity callback that is invoked before the first frame update
        /// and prior to Start().
        /// </summary>
        private void Awake()
        {
            // Create glTF import queue.

            _importTasks = new List<GltfImportTask>();
            _logProfilingData = false;

            // Set import options so that imported models are
            // automatically scaled to a standard size.

            ImportOptions = new GltfImportOptions
            {
                AutoScale = true,
                AutoScaleSize = ModelManager.Instance.DefaultModelSize
            };

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

            // Parse command-line options using NDesk.Options library.

            var optionSet = new OptionSet
            {
                {
                    "i|import=",
                    "import glTF file from {URI} (filename or HTTP URL)",
                    uri => QueueImport(uri)
                },
                {
                    "p|profile",
                    "profile glTF imports and log results in TSV format",
                    enable => _logProfilingData = enable != null
                }
            };

            optionSet.Parse(Environment.GetCommandLineArgs());

            // If no glTF file was specified on the command line,
            // load the default "Sir Piggleston" model.

            if (_importTasks.Count == 0)
            {
                QueueImport(Path.Combine(
                    Application.streamingAssetsPath, "piggleston.glb"));
            }

            // Add platform-specific behaviours.

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            gameObject.AddComponent<WindowsGameManager>();
#elif UNITY_ANDROID
            gameObject.AddComponent<AndroidGameManager>();
#elif UNITY_WEBGL
            gameObject.AddComponent<WebGlGameManager>();
#endif
        }

        /// <summary>
        /// Abort the currently running glTF import (if any) and
        /// remove any queued glTF imports.
        /// </summary>
        private void AbortImports()
        {
            // Call Abort() on each import task so that:
            //
            // (1) Resources are freed for any partially completed glTF imports.
            // (2) Any user-specified OnAborted callbacks get invoked.

            foreach (var importTask in _importTasks)
                importTask.Abort();

            _importTasks.Clear();
        }

        /// <summary>
        /// Abort any running/queued glTF imports and start importing
        /// the given glTF file.
        ///
        /// This version of StartImport is only called by the
        /// WebGL version of the viewer.
        /// </summary>
        /// <param name="data">
        /// A byte[] containing the raw byte content of a .glb/.zip file.
        /// </param>
        /// <param name="filename">
        /// The name of the .glb/.zip file that `data` was read from.
        /// The purpose of this argument is just to provide a filename
        /// to show in the progress log. Note that parameter only
        /// provides the basename for the file, not the complete file path.
        /// (For security reasons, the WebGL build is not allowed to know
        /// the complete path of a user-selected input file.)
        /// </param>
        public void StartImport(byte[] data, string filename)
        {
            AbortImports();
            var importTask = RuntimeGltfImporter.GetImportTask(data, ImportOptions);
            QueueImport(importTask, filename);
        }

        /// <summary>
        /// Abort any running/queued glTF imports and start importing
        /// the given glTF file.
        ///
        /// Note that the URI argument is passed in as a string
        /// rather than a `Uri` object so that this method
        /// can be invoked from javascript.
        /// </summary>
        /// <param name="uriStr">The URI of the input glTF file.</param>
        public void StartImport(string uriStr)
        {
            AbortImports();
            QueueImport(uriStr);
        }

        /// <summary>
        /// Queue the given glTF file to be imported after any other
        /// running/queued glTF imports have completed.
        /// </summary>
        /// <param name="uriStr">
        /// The URI for the input glTF file, which may be either an
        /// absolute file path or HTTP URL.
        /// </param>
        public void QueueImport(string uriStr)
        {
            var uri = new Uri(uriStr);
            var basename = Path.GetFileName(uri.ToString());
            var importTask = RuntimeGltfImporter.GetImportTask(uri, ImportOptions);
            QueueImport(importTask, basename);
        }

        /// <summary>
        /// Queue the given glTF file to be imported after any other
        /// running/queued glTF imports have completed.
        /// </summary>
        /// <param name="importTask">
        /// The glTF import task (coroutine) to add to the queue.
        /// </param>
        /// <param name="filename">
        /// The filename to show in the progress log.
        /// </param>
        public void QueueImport(GltfImportTask importTask, string filename)
        {
            importTask.PushTask(() =>
            {
                // Reset the progress log and print the name of the
                // glTF file we are about to load.
                ProgressLog.Instance.StartImport();
                ProgressLog.Instance.AddLineCallback(
                    String.Format("Loading {0}...", filename));
            });

            importTask.OnProgress += ProgressLog.Instance.OnImportProgress;
            importTask.OnCompleted += OnImportCompleted;
            importTask.OnException += OnImportException;
            importTask.RethrowExceptionAfterCallbacks = false;

            if (_logProfilingData)
            {
                importTask.OnCompleted += _ => LogProfilingData(
                    filename, importTask.ProfilingData);
            }

            _importTasks.Add(importTask);
        }

        /// <summary>
        /// Invoked after a model has been successfully imported.
        /// </summary>
        public void OnImportCompleted(GameObject model)
        {
            Gui.Instance.ResetSpin();
            Gui.Instance.ResetFooterMessage();
            Gui.Instance.ResetAnimationControls();

            ProgressLog.Instance.AddLineCallback(
                String.Format("Total import time: {0} ms",
                    ProgressLog.Instance.Stopwatch.ElapsedMilliseconds));

            ProgressLog.Instance.AddLineCallback(
                String.Format("Longest Unity thread stall: {0} ms",
                    _importTasks[0].LongestStepInMilliseconds()));

            ProgressLog.Instance.AddLineCallback("Success!");

            ModelManager.Instance.SetModel(model);
        }

        /// <summary>
        /// Invoked when an exception is thrown during model import.
        /// </summary>
        public void OnImportException(Exception e)
        {
            if (e is JsonParseException)
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
        }

        /// <summary>
        /// Print profiling data to the debug log as table in TSV format.
        /// </summary>
        private void LogProfilingData(string filename,
            List<GltfImportTask.ProfilingRecord> profilingData)
        {
            var builder = new StringBuilder();

            builder.Append("BEGIN_PROFILING_DATA\n");

            builder.Append(string.Format("{0}\t{1}\t{2}\n",
                "file", "step", "milliseconds"));

            foreach (var profilingRecord in profilingData)
            {
                builder.Append(string.Format("{0}\t{1}\t{2}\n",
                    filename, profilingRecord.TaskType, profilingRecord.Milliseconds));
            }

            builder.Append("END_PROFILING_DATA\n");

            Debug.Log(builder.ToString());
        }

        /// <summary>
        /// Unity callback that is invoked once per frame.
        /// </summary>
        public void Update()
        {
            // advance execution of import tasks
            while (_importTasks.Count > 0 && !_importTasks[0].MoveNext())
                _importTasks.RemoveAt(0);
        }

    }
}