using UnityEditor;
using UnityEngine;

namespace PigletViewer
{
    class WebGLBuilder{
        /// <summary>
        /// <para>
        /// Method to run an automated WebGL build from the command line.
        /// The build output is a web app located at `build/webgl` under
        /// the project root.
        /// </para>
        /// <para>
        /// To run the build from the command line, switch to the
        /// project root folder and run:
        /// </para>
        /// <para>
        /// Unity.exe -logFile - -executeMethod
        /// PigletViewer.WebGLBuilder.Build -batchmode -quit
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Build/WebGL")]
        public static void Build()
        {
            const string buildPath = "build/webgl";

            // Configure the "WebGL template", i.e. the index.html
            // file that wraps the Unity canvas.
            //
            // A different WebGL template is used for Unity 2020+,
            // since there were substantial changes to WebGL builds
            // in Unity 2020. See the following link for details:
            // https://forum.unity.com/threads/changes-to-the-webgl-loader-and-templates-introduced-in-unity-2020-1.817698/

#if UNITY_2020_1_OR_NEWER
            PlayerSettings.WebGL.template = "PROJECT:Piglet2020";
#else
            PlayerSettings.WebGL.template = "PROJECT:Piglet2018";
#endif

            // Ensure that drag-and-drop imports are triggered even
            // if the PigletViewer browser tab doesn't have focus. This
            // also allows users to do long-running glTF imports in
            // the background.

            PlayerSettings.runInBackground = true;

            // Allows the Unity WebGL build to work correctly even when the
            // web server doesn't serve the compressed build files with
            // the correct `Content-Encoding` header. For further explanation,
            // see: https://docs.unity3d.com/Manual/webgl-deploying.html
            //
            // This option is required for WebGL builds to work correctly
            // with GitHub Pages or with minimal command-line HTTP servers
            // like `webfsd`.
            //
            // I haven't tested it, but if I understand correctly the
            // "decompression fallback" was built-in and always enabled
            // prior to Unity 2020.1, whereas in Unity 2020.1 and newer,
            // it must be explicitly enabled. For further info about
            // changes to WebGL builds in Unity 2020.1, see:
            // https://forum.unity.com/threads/changes-to-the-webgl-loader-and-templates-introduced-in-unity-2020-1.817698/
#if UNITY_2020_1_OR_NEWER
            PlayerSettings.WebGL.decompressionFallback = true;
#endif

            // Run the build.

            var buildOptions = new BuildPlayerOptions
            {
                target = BuildTarget.WebGL,
                locationPathName = buildPath,
                scenes = new [] { "Assets/PigletViewer/Scenes/MainScene.unity" }
            };

            BuildPipeline.BuildPlayer(buildOptions);

            Debug.Log($"Successfully built WebGL app at {buildPath}!");
        }
    }
}