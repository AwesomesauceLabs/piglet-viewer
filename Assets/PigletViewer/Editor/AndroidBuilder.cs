using UnityEditor;
using UnityEngine;

namespace PigletViewer
{
    public class AndroidBuilder
    {
        /// <summary>
        /// <para>
        /// Method to run an automated Android build from the command line.
        /// The build output is an Android .apk file located at
        /// `build/piglet-viewer.apk' under the project root.
        /// </para>
        /// <para>
        /// To run the build from the command line, switch to the
        /// project root folder and run:
        /// </para>
        /// <para>
        /// Unity.exe -logFile - -executeMethod
        /// PigletViewer.AndroidBuilder.Build -batchmode -quit
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Build/Android")]
        public static void Build()
        {
            const string buildPath = "build/piglet-viewer.apk";

            // Android builds will error out until you change
            // the package name from the default value.

            PlayerSettings.SetApplicationIdentifier(
                BuildTargetGroup.Android, "com.awesomesauce.piglet");

            // Run the build.

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.Android,
                locationPathName = buildPath,
                scenes = new [] { "Assets/PigletViewer/Scenes/MainScene.unity" }
            };

            BuildPipeline.BuildPlayer(buildOptions);

            Debug.Log($"Successfully built Android APK file at {buildPath}!");
        }
    }
}