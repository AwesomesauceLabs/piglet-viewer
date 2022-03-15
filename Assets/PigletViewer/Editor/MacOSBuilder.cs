using UnityEditor;
using UnityEngine;

namespace PigletViewer
{
    public class MacOSBuilder
    {
        /// <summary>
        /// <para>
        /// Method to run an automated macOS build from the command line.
        /// The build output is a macOS app bundle located at
        /// `build/macos.app` under the project root, and the output executable
        /// is `build/macos.app/Contents/MacOS/PigletViewer`.
        /// </para>
        /// <para>
        /// To run the build from the command line, switch to the
        /// project root folder and run:
        /// </para>
        /// <para>
        /// Unity.exe -logFile /dev/stdout -executeMethod
        /// PigletViewer.MacOSBuilder.Build -batchmode -quit
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Build/macOS")]
        public static void Build()
        {
            const string buildPath = "build/macos";

            // Configure the name of the output executable in
            // `build/macos.app/Contents/MacOS`.

            PlayerSettings.productName = "PigletViewer";

            // Run the build. (Generates macOS app bundle in `build/macos.app`
            // under project root).

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.StandaloneOSX,
                locationPathName = buildPath,
                scenes = new [] { "Assets/PigletViewer/Scenes/MainScene.unity" }
            };

            BuildPipeline.BuildPlayer(buildOptions);

            Debug.Log($"Successfully built macOS app bundle at {buildPath}!");
        }
    }
}
