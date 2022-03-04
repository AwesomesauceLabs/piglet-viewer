using System;
using Piglet;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

#if URP_PACKAGE_IS_INSTALLED
using UnityEngine.Rendering.Universal;
#endif

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
        /// Install the Universal Rendering Pipeline and add the
        /// scripting define `URP_PACKAGE_IS_INSTALLED`.
        /// </summary>
        public static void InstallURPPackageAndQuit()
        {
            var request = Client.Add("com.unity.render-pipelines.universal");

            void OnUpdate()
            {
                if (!request.IsCompleted)
                    return;

                EditorApplication.update -= OnUpdate;

                if (request.Status == StatusCode.Success)
                {
                    Debug.Log("Installed: " + request.Result.packageId);

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone,
                        "URP_PACKAGE_IS_INSTALLED");

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL,
                        "URP_PACKAGE_IS_INSTALLED");

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android,
                        "URP_PACKAGE_IS_INSTALLED");

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS,
                        "URP_PACKAGE_IS_INSTALLED");

                    EditorApplication.Exit(0);
                }
                else
                {
                    if (request.Status >= StatusCode.Failure)
                        Debug.LogError(request.Error.message);

                    EditorApplication.Exit(1);
                }

            }

            EditorApplication.update += OnUpdate;
        }

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
#if URP_PACKAGE_IS_INSTALLED
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
#if URP_PACKAGE_IS_INSTALLED

            // Create default URP render pipeline asset (if it doesn't already exist).
            //
            // The #if/#else is needed here because Unity renamed `ForwardRendererData`
            // to `UniversalRendererData` in Unity 2021.2.

#if UNITY_2021_2_OR_NEWER
            UniversalRendererData rendererData = null;

            if (!AssetPathUtil.Exists(URP_RENDERER_ASSET_PATH))
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, URP_RENDERER_ASSET_PATH);
                // reload asset so that object reference is "backed" by asset file
                rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(URP_RENDERER_ASSET_PATH);
            }
#else
            ForwardRendererData rendererData = null;

            if (!AssetPathUtil.Exists(URP_RENDERER_ASSET_PATH))
            {
                rendererData = ScriptableObject.CreateInstance<ForwardRendererData>();
                AssetDatabase.CreateAsset(rendererData, URP_RENDERER_ASSET_PATH);
                // reload asset so that object reference is "backed" by asset file
                rendererData = AssetDatabase.LoadAssetAtPath<ForwardRendererData>(URP_RENDERER_ASSET_PATH);
            }
#endif

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
