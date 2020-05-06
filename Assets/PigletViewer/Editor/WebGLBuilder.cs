using UnityEditor;

namespace PigletViewer
{
    /// <summary>
    /// This class enables WebGL builds from the command line,
    /// by running:
    ///
    /// `$ Unity.exe -quit -batchmode -projectPath $UNITY_PROJECT_PATH -executeMethod WebGLBuilder.Build`
    ///
    /// See https://answers.unity.com/questions/829349/command-line-flag-to-build-webgl.html
    /// and https://gist.github.com/jagwire/0129d50778c8b4462b68 for further
    /// background info.
    ///
    /// Note: This script needs to be placed under an `Editor` folder under
    /// `Assets`.
    /// </summary>
    class WebGLBuilder{
        static void Build() {
            string[] scenes = {"Assets/PigletViewer/Scenes/MainScene.unity"};
            BuildPipeline.BuildPlayer(scenes, "Builds/WebGL/dist", BuildTarget.WebGL, BuildOptions.Development);
        }
    }
}