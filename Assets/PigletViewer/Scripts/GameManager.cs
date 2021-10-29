using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Piglet;
using UnityEngine;
using NDesk.Options;
using Debug = UnityEngine.Debug;

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
        private List<IEnumerator> _importTasks;

        /// <summary>
        /// Variables that are controlled by command-line options.
        /// </summary>
        private struct CommandLineOptions
        {
            /// <summary>
            /// If true, print a TSV table of profiling data
            /// to the debug log after each glTF import.
            /// </summary>
            public bool Profile;

            /// <summary>
            /// If true, exit the application immediately
            /// after running all tasks specified on
            /// the command line (e.g. --import).
            /// </summary>
            public bool Quit;
        }

        private CommandLineOptions _options;

        /// <summary>
        /// Unity callback that is invoked before the first frame update
        /// and prior to Start().
        /// </summary>
        private void Awake()
        {
            // Create glTF import queue.

            _importTasks = new List<IEnumerator>();

            // Command line option defaults.

            _options = new CommandLineOptions
            {
                Profile = false
            };

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

            // Parse command line options.

            ParseCommandLineOptions();

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
        /// <para>
        /// Parse command-line options using NDesk.Options library.
        /// </para>
        /// <para>
        /// The user may specify default command-line options
        /// in a file called `Resources/piglet-viewer-args.txt`,
        /// and then any options specified on the command line
        /// will be appended to these.
        /// </para>
        /// </summary>
        private void ParseCommandLineOptions()
        {
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
                    enable => _options.Profile = enable != null
                },
                {
                    "q|quit",
                    "exit program after performing all command-line actions",
                    enable => _options.Quit = enable != null
                },
                {
                    "s|sleep=",
                    "sleep for {SECONDS} seconds",
                    seconds => _importTasks.Add(Sleep(float.Parse(seconds)))
                }
            };

            // Read command-line options.
            //
            // First read the default command-line options from
            // `Resources/piglet-viewer-args.txt`, if that file exists.
            // This is useful on platforms where invoking the Unity player
            // with custom command-line options is either inconvenient or
            // impossible (e.g. Android, WebGL).

            var args = new List<string>();

            var defaultArgs = Resources.Load<TextAsset>("piglet-viewer-args");
            if (defaultArgs != null)
                args.AddRange(defaultArgs.text.Split(null));

            // Remove all command-line args before "--" separator.
            //
            // Args before "--" are built-in options for the Unity
            // Editor/Player (e.g. -projectPath), while args after
            // "--" (if present) are PigletViewer options.

            var pigletViewerArgs = new Queue<string>(Environment.GetCommandLineArgs());

            while (pigletViewerArgs.Count > 0
                && pigletViewerArgs.Dequeue() != "--") {}

            args.AddRange(pigletViewerArgs);

            // Parse command-line options and invoke handlers.

            optionSet.Parse(args);
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

            foreach (var task in _importTasks)
            {
                var importTask = task as GltfImportTask;
                importTask?.Abort();
            }

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
            var uri = UriUtil.GetAbsoluteUri(uriStr);
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

            importTask.OnCompleted += _ =>
            {
                if (_options.Profile)
                    LogProfilingData(filename, importTask.ProfilingRecords);
            };

            _importTasks.Add(importTask);
        }

        /// <summary>
        /// Invoked after a model has been successfully imported.
        /// </summary>
        public void OnImportCompleted(GameObject model)
        {
            var importTask = _importTasks[0] as GltfImportTask;

            Gui.Instance.ResetSpin();
            Gui.Instance.ResetFooterMessage();
            Gui.Instance.ResetAnimationControls();

            ProgressLog.Instance.AddLineCallback(
                String.Format("Total import time: {0} ms",
                    ProgressLog.Instance.Stopwatch.ElapsedMilliseconds));

            ProgressLog.Instance.AddLineCallback(
                String.Format("Longest Unity thread stall: {0} ms",
                    importTask.LongestStepInMilliseconds()));

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
            Debug.Log("[PigletViewer] BEGIN_PROFILING_DATA\n");
            Debug.LogFormat("[PigletViewer] {0}\t{1}\t{2}\t{3}\t{4}\t{5}\n",
                "file", "step", "move_next_ms", "wallclock_ms",
                "move_next_calls", "frames");

            foreach (var profilingRecord in profilingData)
            {
                Debug.LogFormat(
                    "[PigletViewer] {0}\t{1}\t{2}\t{3}\t{4}\t{5}\n",
                    filename, profilingRecord.TaskType,
                    profilingRecord.StopwatchMoveNext.ElapsedMilliseconds,
                    profilingRecord.StopwatchWallclock.ElapsedMilliseconds,
                    profilingRecord.MoveNextCalls,
                    profilingRecord.Frames);
            }

            Debug.Log("[PigletViewer] END_PROFILING_DATA\n");
        }

        /// <summary>
        /// <para>
        /// Sleep for the given number of seconds before running the next
        /// import task.
        /// </para>
        /// <para>
        /// I added this method (and the corresponding --sleep option) so that
        /// I could prevent Unity player initialization from interfering
        /// with profiling results (--profile).
        /// </para>
        /// </summary>
        private static IEnumerator Sleep(float seconds)
        {
            var origMessage = Gui.Instance.FooterMessage;

            Gui.Instance.FooterMessage = string.Format(
                "sleeping for {0} seconds...", seconds);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < seconds * 1000)
                yield return null;

            Gui.Instance.FooterMessage = origMessage;
        }

        /// <summary>
        /// Unity callback that is invoked once per frame.
        /// </summary>
        public void Update()
        {
            // advance execution of import tasks
            while (_importTasks.Count > 0 && !_importTasks[0].MoveNext())
            {
                _importTasks.RemoveAt(0);

                if (_importTasks.Count == 0)
                {
                    // Magic string that scripts can use to detect when all
                    // command-line actions have completed.
                    //
                    // I use this to facilitate automated profiling of the
                    // PigletViewer WebGL build. Once the "IDLE"
                    // string appears in the Google Chrome log file, I know that
                    // it is safe to extract the profiling data from the log and
                    // kill the Chrome process.

                    if (_options.Profile)
                        Debug.Log("[PigletViewer] IDLE");

                    if (_options.Quit)
                        Application.Quit();
                }
            }
        }

    }
}