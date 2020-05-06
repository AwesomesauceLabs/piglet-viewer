using UnityEditor;

namespace PigletViewer
{
    public class AndroidBuilder
    {
        static void Build() {
            string[] scenes = {"Assets/PigletViewer/Scenes/MainScene.unity"};
            BuildPipeline.BuildPlayer(scenes, "Builds/Android/piglet.apk",
                BuildTarget.Android, BuildOptions.None);
        }
    }
}