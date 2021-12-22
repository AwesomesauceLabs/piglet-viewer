using Piglet;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PigletViewer
{
    /// <summary>
    /// Switches the active render pipeline (e.g. built-in, URP)
    /// for the current Unity project.
    /// </summary>
    public class RenderPipelineSwitcher
    {
        private const string URP_RENDERER_ASSET_PATH = "Assets/UniversalRenderPipeline_Renderer.asset";
        private const string URP_PIPELINE_ASSET_PATH = "Assets/UniversalRenderPipeline.asset";

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
        [MenuItem("PigletViewer/Switch Project to URP", true)]
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
        [MenuItem("PigletViewer/Switch Project to URP")]
        public static void SwitchProjectToURP()
        {
#if UNITY_2019_3_OR_NEWER

            // Create default URP render pipeline asset (if it doesn't already exist).

            ForwardRendererData rendererData = null;

            if (!AssetPathUtil.Exists(URP_RENDERER_ASSET_PATH))
            {
                rendererData = ScriptableObject.CreateInstance<ForwardRendererData>();
                AssetDatabase.CreateAsset(rendererData, URP_RENDERER_ASSET_PATH);
                // reload asset so that object reference is "backed" by asset file
                rendererData = AssetDatabase.LoadAssetAtPath<ForwardRendererData>(URP_RENDERER_ASSET_PATH);
            }

            RenderPipelineAsset renderPipeline = null;

            if (!AssetPathUtil.Exists(URP_PIPELINE_ASSET_PATH))
            {
                renderPipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(renderPipeline, URP_PIPELINE_ASSET_PATH);
            }

            // reload asset so that object reference is "backed" by asset file
            renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(URP_PIPELINE_ASSET_PATH);
            GraphicsSettings.renderPipelineAsset = renderPipeline;

            // Set color space to linear to match Unity's
            // default setting for new URP projects.
            PlayerSettings.colorSpace = ColorSpace.Linear;

#endif
        }
    }
}
