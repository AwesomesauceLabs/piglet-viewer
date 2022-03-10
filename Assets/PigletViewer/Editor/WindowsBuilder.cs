using UnityEditor;
using UnityEngine;

namespace PigletViewer
{
    public class WindowsBuilder
    {
        /// <summary>
        /// <para>
        /// Method to run an automated Windows build from the command line.
        /// The build output is a Windows 64-bit executable located at
        /// `build/win64/piglet-viewer.exe` under the project root.
        /// </para>
        /// <para>
        /// To run the build from the command line, switch to the
        /// project root folder and run:
        /// </para>
        /// <para>
        /// Unity.exe -logFile /dev/stdout -executeMethod
        /// PigletViewer.WindowsBuilder.Build -batchmode -quit
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Build/Win64")]
        public static void Build()
        {
            const string buildPath = "build/win64/piglet-viewer.exe";

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneWindows64,
                locationPathName = buildPath,
                scenes = new [] { "Assets/PigletViewer/Scenes/MainScene.unity" }
            };

            BuildPipeline.BuildPlayer(buildOptions);

            Debug.Log($"Successfully built Windows 64-bit executable at {buildPath}!");
        }
    }
}
