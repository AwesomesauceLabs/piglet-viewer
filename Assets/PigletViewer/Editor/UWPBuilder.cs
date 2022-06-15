using UnityEditor;
using UnityEngine;

namespace PigletViewer
{
    public class UWPBuilder
    {
        /// <summary>
        /// <para>
        /// Method to run an automated UWP (Universal Windows Platform) build
        /// from the command line. The build output is a UWP app located at
        /// `build/uwp` under the project root.
        /// </para>
        /// <para>
        /// To run the build from the command line, switch to the
        /// project root folder and run:
        /// </para>
        /// <para>
        /// Unity.exe -logFile /dev/stdout -executeMethod
        /// PigletViewer.UWPBuilder.Build -batchmode -quit
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Build/UWP")]
        public static void Build()
        {
            const string buildPath = "build/uwp";

            // Configure the name of the output .exe

            PlayerSettings.productName = "PigletViewer";

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.WSAPlayer,
                locationPathName = buildPath,
                scenes = new [] { "Assets/PigletViewer/Scenes/MainScene.unity" }
            };

            BuildPipeline.BuildPlayer(buildOptions);

            Debug.Log($"Successfully built UWP app at {buildPath}!");
        }
    }
}
