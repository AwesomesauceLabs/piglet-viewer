using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PigletViewer
{
    /// <summary>
    /// Switches the active render pipeline (e.g. built-in, URP)
    /// for the current Unity project.
    /// </summary>
    public class RenderPipelineSwitcher
    {
        /// <summary>
        /// <para>
        /// Returns true if "PigletViewer/Switch to URP"
        /// should be enabled in the Unity menu. Otherwise the
        /// menu item will be grayed out.
        /// </para>
        /// <para>
        /// "PigletViewer/Switch to URP" should only be enabled
        /// if the Unity version is 2019.3 or newer, since
        /// older Unity versions predate URP.
        /// </para>
        /// <para>
        /// NOTE: The second `true` argument makes this
        /// a validator for another MenuItem function
        /// with the same name. For further info, see:
        /// https://docs.unity3d.com/ScriptReference/MenuItem.html
        /// </para>
        /// </summary>
        /// <returns></returns>
        [MenuItem("PigletViewer/Switch to URP", true)]
        public static bool IsSwitchToURPEnabled()
        {
#if UNITY_2019_3_OR_NEWER
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// <para>
        /// Switch the active render pipeline of the current
        /// Unity project to URP.
        /// </para>
        /// <para>
        /// NOTE: This method has no effect if the URP package
        /// (com.unity.render-pipelines.universal) is not installed.
        /// </para>
        /// </summary>
        [MenuItem("PigletViewer/Switch to URP")]
        public static void SwitchToURP()
        {
#if UNITY_2019_3_OR_NEWER
            var renderPipeline = Resources.Load<RenderPipelineAsset>("RenderPipeline/PigletURPPipelineAsset");
            GraphicsSettings.renderPipelineAsset = renderPipeline;

            // Set color space to linear to match Unity's
            // default setting for new URP projects.
            PlayerSettings.colorSpace = ColorSpace.Linear;
#endif

#if UNITY_2020_2_OR_NEWER
            AssetDatabase.ImportPackage(
                "Assets/Piglet/Extras/URP-Shaders-2020.2.unitypackage", false);
#elif UNITY_2019_3_OR_NEWER
            AssetDatabase.ImportPackage(
                "Assets/Piglet/Extras/URP-Shaders-2019.3.unitypackage", false);
#endif
        }
    }
}
