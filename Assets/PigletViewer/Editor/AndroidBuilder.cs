#if UNITY_ANDROID
using System.IO;
using UnityEditor;
using UnityEditor.Android;
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

            // Set the path to the Android NDK.
            //
            // Unity has a longstanding bug where it does not automatically
            // the Android NDK path, even when the Android NDK is installed
            // via Unity Hub. If we don't set the Android NDK path, command
            // line Android builds will fail with an error message
            // that says "unable to find Android NDK".
            //
            // An additional wrinkle is that Unity Hub installs the Android NDK
            // in different locations on Windows and MacOS.

            var appPath = EditorApplication.applicationPath;
            var appFolder = Path.GetDirectoryName(appPath);

            var ndkPath = Application.platform == RuntimePlatform.OSXEditor
                ? $"{appFolder}/PlaybackEngines/AndroidPlayer/NDK"
                : $"{appFolder}/Data/PlaybackEngines/AndroidPlayer/NDK";

            EditorPrefs.SetString("AndroidNdkRootR16b", ndkPath);
            EditorPrefs.SetString("AndroidNdkRoot", ndkPath);
            AndroidExternalToolsSettings.ndkRootPath = ndkPath;

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
#endif
