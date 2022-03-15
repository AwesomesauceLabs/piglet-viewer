using UnityEditor;
using UnityEngine;

namespace PigletViewer
{
    public class LinuxBuilder
    {
        /// <summary>
        /// <para>
        /// Method to run an automated Linux build from the command line.
        /// The build output is a Linux executable located at
        /// `build/linux/PigletViewer` under the project root.
        /// </para>
        /// <para>
        /// To run the build from the command line, switch to the
        /// project root folder and run:
        /// </para>
        /// <para>
        /// Unity.exe -logFile /dev/stdout -executeMethod
        /// PigletViewer.LinuxBuilder.Build -batchmode -quit
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Build/Linux")]
        public static void Build()
        {
            // Name of output executable (relative to project root).
            const string buildPath = "build/linux/PigletViewer";

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneLinux64,
                locationPathName = buildPath,
                scenes = new [] { "Assets/PigletViewer/Scenes/MainScene.unity" }
            };

            BuildPipeline.BuildPlayer(buildOptions);

            Debug.Log($"Successfully built Linux executable at {buildPath}!");
        }
    }
}
