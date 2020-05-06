using UnityEditor;

namespace PigletViewer
{
    /// <summary>
    /// This class enables Android .apk builds from the command line,
    /// by running:
    ///
    /// `$ Unity.exe -quit -batchmode -projectPath $UNITY_PROJECT_PATH -buildTarget Android -executeMethod AndroidBuilder.Build`
    ///
    /// Note: This script needs to be placed under an `Editor` folder under
    /// `Assets`.
    /// </summary>
    public class AndroidBuilder
    {
        static void Build() {
            string[] scenes = {"Assets/PigletViewer/Scenes/MainScene.unity"};
            BuildPipeline.BuildPlayer(scenes, "Builds/Android/piglet.apk",
                BuildTarget.Android, BuildOptions.None);
        }
    }
}