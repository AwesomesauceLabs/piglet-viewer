using System;
using UnityEngine;

namespace UnityGLTF {
    public class GLTFRuntimeImporter : GLTFImporter
    {
        GLTFRuntimeShaderConfig _shaderConfig;

        public GLTFRuntimeImporter(
            GLTFRuntimeShaderConfig shaderConfig,
            ProgressCallback progressCallback,
            RefreshWindow finishCallback=null)
            : base(progressCallback, finishCallback)
        {
            _shaderConfig = shaderConfig;
        }

        override protected Material CreateUnityMaterial(GLTF.Schema.Material def, int materialIndex)
        {
            Material material = base.CreateUnityMaterial(def, materialIndex);

            Debug.LogFormat("material {0}: keywords = {1}", materialIndex, string.Join(" ", material.shaderKeywords));

            GLTFRuntimeShaderConfig.ShaderVariant? minimalShaderVariant
                = _shaderConfig.GetMinimalShaderVariantForMaterial(material);

            if (!minimalShaderVariant.HasValue) {
                Debug.LogWarningFormat("material {0}: warning: no shader variant found that is a superset of the required keywords", materialIndex);
            } else {
                Debug.LogFormat("material {0}: minimal matching shader variant: {1}",
                    materialIndex, minimalShaderVariant.Value.keywords);
                string[] keywords = minimalShaderVariant.Value.keywords
                    .Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                foreach(var keyword in keywords)
                    material.EnableKeyword(keyword);
            }

            return material;
        }
    }
}