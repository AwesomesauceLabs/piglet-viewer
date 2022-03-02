using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// Implements PigletViewer behaviour that is specific to the iOS platform.
    /// In particular, this class handles automatic loading of glTF files
    /// that have been added to the iOS app's private `Documents` folder.
    /// </summary>
    public class iOSGameManager : MonoBehaviour
    {
        /// <summary>
        /// <para>
        /// Files that have appeared in the `Documents` directory
        /// of the PigletViewer iOS app, but may not have
        /// finished copying yet.
        /// </para>
        /// <para>
        /// Unity does not provide a convenient way to determine if a
        /// file has finished copying into the `Documents` directory,
        /// so we use the rather hacky method of watching
        /// the file size. If the file size does not change for
        /// more 1 second, we assume the copying is complete.
        /// </para>
        /// <para>
        /// Note: The problem of reading partially-copied files
        /// only seems to be an issue when files are copied
        /// from a Mac to an iPhone/iPad using Finder [1]. Copying
        /// files to iOS using AirDrop seems to work fine without
        /// the file-size-watching hack.
        /// </para>
        /// <para>
        /// [1]: Files can be copied from Mac to iPhone/iPad
        /// using Finder by: (1) selecting "iPhone" in the left
        /// sidebar, (2) selecting the "Files" tab in the right area,
        /// (3), dragging-and-dropping the files onto the PigletViewer
        /// app.
        /// </para>
        /// </summary>
        private Dictionary<string, PartiallyCopiedFileState> _partiallyCopiedFiles;

        /// <summary>
        /// Files that this class (iOSGameManager) has added
        /// to `GameManager`s import task queue (`GameManager.Instance.Tasks`).
        /// </summary>
        private HashSet<string> _queuedFiles;

        /// <summary>
        /// File size and timing info that is used to determine
        /// when a file has finished copying into the `Documents`
        /// folder of the PigletViewer iOS app.
        /// </summary>
        public class PartiallyCopiedFileState
        {
            /// <summary>
            /// The size of the file in bytes.
            /// </summary>
            public long Size;
            /// <summary>
            /// Records the time elapsed since the
            /// file size last changed.
            /// </summary>
            public Stopwatch Stopwatch;

            public PartiallyCopiedFileState()
            {
                Size = 0;
                Stopwatch = new Stopwatch();
            }
        }

        /// <summary>
        /// Unity callback that is invoked before the first frame update.
        /// </summary>
        void Start()
        {
            _partiallyCopiedFiles = new Dictionary<string, PartiallyCopiedFileState>();
            _queuedFiles = new HashSet<string>();

            ProcessFiles();
        }

        /// <summary>
        /// Unity callback that is invoked once per frame.
        /// </summary>
        void Update()
        {
            ProcessFiles();
        }

        /// <summary>
        /// <para>
        /// This method that is run after every glTF import,
        /// regardless of it whether succeeds or fails.  It
        /// takes care of deleting the glTF file from the
        /// `Documents` folder after the import has finished,
        /// so that the file will not be re-imported the next time
        /// the PigletViewer iOS app is launched.
        /// </para>
        /// </summary>
        protected IEnumerator OnImportFinished(string path)
        {
            _queuedFiles.Remove(path);

            if (File.Exists(path))
                File.Delete(path);

            yield return null;
        }

        /// <summary>
        /// <para>
        /// Check for new glTF files in the `Documents` folder of the
        /// PigletViewer iOS app, and queue glTF import tasks for any
        /// newly-found files.
        /// </para>
        /// <para>
        /// Background: Whenever an iOS app is launched to view a file,
        /// iOS first copies the file into the app's private `Documents`
        /// folder. The user can also copy a file directly into an app's
        /// `Documents` folder using Finder, by: (1) selecting the
        /// "iPhone"/"iPad" in the left sidebar, (2) selecting the
        /// "Files" tab in the right area, (3) dragging-and-dropping
        /// the files onto the target iOS app.
        /// </para>
        /// </summary>
        void ProcessFiles()
        {
            // When a user opens a glTF file on iOS via AirDrop
            // or via another iOS app (e.g. an attachment in Apple Mail),
            // the file is copied to the `Documents/Inbox` subfolder.
            // However, when the user drags-and-drops a file onto the
            // iOS app in Finder, the file copied directly into the
            // `Documents` folder instead.

            var searchDirs = new string[]
            {
                Application.persistentDataPath,
                Application.persistentDataPath + "/Inbox"
            };

            foreach (var dir in searchDirs)
            {
                if (!Directory.Exists(dir))
                    continue;

                foreach (var path in Directory.GetFiles(dir))
                {
                    // Get the size of the file in bytes.
                    //
                    // We watch the file size to determine when
                    // the file has finished copying to the
                    // `Documents` directory. We assume
                    // that the file has finished copying when
                    // the file size has not changed for more
                    // than 1 second. (It is unfortunate that I
                    // have to use such a hacky method, but so far
                    // I have not found any other way to do it.)
                    //
                    // Note: Partially copied files do not seem
                    // to be an issue when copying files to iOS via
                    // AirDrop or when opening files from another
                    // app on iOS (e.g. opening an attachment in Apple Mail.)
                    // The problem only seems to occur when copying files
                    // from macOS to iOS in Finder, via the "iPhone" ->
                    // "Files" tab.

                    var size = new FileInfo(path).Length;

                    // If we are discovering this file for the first time.

                    PartiallyCopiedFileState partiallyCopiedFileState;

                    if (!_partiallyCopiedFiles.TryGetValue(path, out partiallyCopiedFileState)
                        && !_queuedFiles.Contains(path))
                    {
                        _partiallyCopiedFiles.Add(path, new PartiallyCopiedFileState());
                        continue;
                    }

                    // If file size has changed since the last time we checked.

                    if (size != partiallyCopiedFileState.Size)
                    {
                        partiallyCopiedFileState.Size = size;
                        partiallyCopiedFileState.Stopwatch.Restart();
                        continue;
                    }

                    // If file size has not changed for > 1 second, assume file copy
                    // is done and queue the glTF import.

                    if (partiallyCopiedFileState.Stopwatch.ElapsedMilliseconds > 1000)
                    {
                        _partiallyCopiedFiles.Remove(path);

                        // Queue a new task to import the glTF file.
                        //
                        // Note: Both `StartImport` and `QueueImport`
                        // do the same thing, but `StartImport`
                        // aborts any existing tasks first.
                        // This is important when opening the iOS app
                        // with a specific file (e.g. via AirDrop), so
                        // that the default command-line parsing task
                        // is not run.

                        if (_queuedFiles.Count == 0)
                            GameManager.Instance.StartImport(path);
                        else
                            GameManager.Instance.QueueImport(path);

                        // Delete the glTF file once the import has
                        // succeeded/failed. This prevents the file from being
                        // re-imported the next time the iOS app is launched.

                        GameManager.Instance.Tasks.Add(OnImportFinished(path));

                        // Remember the files we have already queued for import,
                        // so we don't re-import them on every call to Update().

                        _queuedFiles.Add(path);
                    }
                }
            }
        }
    }
}
