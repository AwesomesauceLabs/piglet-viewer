using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityGLTF;
using Debug = UnityEngine.Debug;

namespace Piglet
{
    /// <summary>
    /// Times the various steps/substeps of a glTF import and generates
    /// nicely formatted progress messages.
    /// </summary>
    public class ImportLog : Singleton<ImportLog>
    {
        /// <summary>
        /// The list of progress messages generated for the
        /// current glTF import.
        /// </summary>
        public List<string> Lines;

        public struct ProgressStep
        {
            /// <summary>
            /// The current type of glTF entity that is being
            /// imported (e.g. textures, meshes).  This variable
            /// is used to sum the import times for entities
            /// of the same type, and to report the total import
            /// time that type on a single line of the progress
            /// log.
            /// </summary>
            public GltfImporter.ImportStep Step;
            /// <summary>
            /// Number of glTF entities imported so far for the
            /// current import step (e.g. textures, meshes).
            /// </summary>
            public int NumCompleted;
            /// <summary>
            /// Total number of glTF entities to import for the
            /// current import step (e.g. textures, meshes).
            /// </summary>
            public int NumTotal;
            /// <summary>
            /// The total elapsed time in milliseconds since
            /// the beginning of the glTF import, up to and including
            /// this ProgressStep.
            /// </summary>
            public long ElapsedMilliseconds;
        }
        
        /// <summary>
        /// A progress history
        /// </summary>
        private readonly List<ProgressStep> _progressSteps;
        
        /// <summary>
        /// A stopwatch used to track the time used for importing
        /// individual glTF entities (e.g. textures, meshes).
        /// </summary>
        public readonly Stopwatch Stopwatch;

        /// <summary>
        /// Constructor.
        /// </summary>
        protected ImportLog()
        {
            Lines = new List<string>();
            _progressSteps = new List<ProgressStep>();
            Stopwatch = new Stopwatch();
        }

        /// <summary>
        /// Append a line to the import progress log.
        /// </summary>
        public void AddLine(string line)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PigletViewerJsLib.AppendLogLine(line);
#else
            Lines.Add(line);
#endif
        }

        /// <summary>
        /// Replace the last line of the import progress log.
        /// </summary>
        public void ReplaceLastLine(string line)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PigletViewerJsLib.UpdateTailLogLine(line);
#else
            Lines[Lines.Count - 1] = line;
#endif
        }

        /// <summary>
        /// Clear any recorded progress steps and restart
        /// the import stopwatch at zero milliseconds.
        /// </summary>
        public void StartImport()
        {
            Lines.Clear();
            _progressSteps.Clear();
            Stopwatch.Restart();
        }

        public void OnImportProgress(GltfImporter.ImportStep importStep, int numCompleted, int total)
        {
            UpdateProgress(importStep, numCompleted, total);
            string message = GetProgressMessage();

            // Update existing tail log line if we are still importing
            // the same type of glTF entity (e.g. textures), or
            // add a new line if we have started to import
            // a new type.
            
            if (IsNewImportStep())
                AddLine(message);
            else
                ReplaceLastLine(message);

            Debug.Log(message);
        }

        /// <summary>
        /// Record a new progress step.
        /// </summary>
       public void UpdateProgress(GltfImporter.ImportStep importStep,
            int numCompleted, int numTotal)
        {
            _progressSteps.Add(new ProgressStep
                {
                    Step = importStep,
                    NumCompleted = numCompleted,
                    NumTotal = numTotal,
                    ElapsedMilliseconds = Stopwatch.ElapsedMilliseconds
                }
            );
        }

        /// <summary>
        /// Return the total time in milliseconds for the
        /// current import step (e.g. textures, meshes).
        /// </summary>
        public long GetMillisecondsForCurrentImportStep()
        {
            ProgressStep progressStep = _progressSteps[_progressSteps.Count - 1];
            GltfImporter.ImportStep importStep = progressStep.Step;
            
            long endTime = progressStep.ElapsedMilliseconds;
            long startTime = 0;
            
            for (int i = _progressSteps.Count - 1; i >= 0; --i)
            {
                if (_progressSteps[i].Step != importStep)
                    break;

                startTime = _progressSteps[i].ElapsedMilliseconds;
            }

            return endTime - startTime;
        }

        /// <summary>
        /// Return true if the most recent progress step
        /// was the start of a new import stage (e.g.
        /// buffers, textures, meshes).
        /// </summary>
        public bool IsNewImportStep()
        {
            if (_progressSteps.Count <= 1)
                return true;

            return _progressSteps[_progressSteps.Count - 1].Step
               != _progressSteps[_progressSteps.Count - 2].Step;
        }
        
        /// <summary>
        /// Generate a progress message for the most
        /// recent progress step.
        /// </summary>
        public string GetProgressMessage()
        {
            ProgressStep progressStep = _progressSteps[_progressSteps.Count - 1];
            
            int currentStep = Math.Min(
                progressStep.NumCompleted + 1, progressStep.NumTotal);
            
            string message;
            switch (progressStep.Step)
            {
                case GltfImporter.ImportStep.Read:
                    float kb = progressStep.NumCompleted / 1024f;
                    float totalKb = progressStep.NumTotal / 1024f;
                    message = string.Format(
                        "Reading {0:D}/{1:D} KB...",
                        (int)Mathf.Round(kb), (int)Mathf.Round(totalKb));
                    break;
                case GltfImporter.ImportStep.Parse:
                    message = string.Format(
                        "Parsing json {0}/{1}...", currentStep,
                        progressStep.NumTotal);
                    break;
                case GltfImporter.ImportStep.MorphTarget:
                    message = string.Format(
                        "Loading morph targets {0}/{1}...",
                        currentStep, progressStep.NumTotal);
                    break;
                default:
                    message = string.Format("Loading {0} {1}/{2}...",
                        progressStep.Step.ToString().ToLower(),
                        currentStep, progressStep.NumTotal);
                    break;
            }

            if (progressStep.NumCompleted == progressStep.NumTotal)
            {
                message += string.Format(" done ({0} ms)",
                    GetMillisecondsForCurrentImportStep());
            }

            return message;
        }
    }
}