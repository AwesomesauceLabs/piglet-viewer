using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Configures the set of shader variants that are compiled into the build,
/// and provides information at runtime about which shader variants/keywords
/// are available.
/// </summary>
public class GLTFRuntimeShaderConfig
{
    [Serializable]
    public struct ShaderVariantArray
    {
        public ShaderVariant[] Values;
    }

    [Serializable]
    public struct ShaderVariant
    {
        public string ShaderName;
        public string ShaderPassName;
        public string ShaderKeywords;
    }

    protected ShaderVariant[] _shaderVariants;

    public GLTFRuntimeShaderConfig(string json)
    {
        _shaderVariants = JsonUtility.FromJson<ShaderVariantArray>(json).Values;
    }

    protected static string ShaderVariantToString(ShaderVariant shaderVariant)
    {
        return string.Format("(shader: \"{0}\", pass: \"{1}\", keywords: \"{2}\")",
            shaderVariant.ShaderName,
            shaderVariant.ShaderPassName,
            shaderVariant.ShaderKeywords);
    }

#if UNITY_EDITOR
    public void CreateShaderVariantCollectionAsset(string projectPath)
    {
        ShaderVariantCollection shaderVariantCollection = new ShaderVariantCollection();

        char[] keywordSeparators = new char[] {' '};

        foreach (var shaderConfig in _shaderVariants) {

            ShaderVariantCollection.ShaderVariant shaderVariant
                = new ShaderVariantCollection.ShaderVariant();

            shaderVariant.shader = Shader.Find(shaderConfig.ShaderName);
            if (shaderVariant.shader == null) {
                Debug.LogErrorFormat(
                    "failed to add shader variant to collection: "
                    + "no shader found for shader name in {0}",
                    ShaderVariantToString(shaderConfig));
                continue;
            }

            PassType passType;
            if (!Enum.TryParse(shaderConfig.ShaderPassName, true, out passType)) {
                Debug.LogErrorFormat(
                    "failed to add shader variant to collection: "
                    + "unrecognized shader pass name in {0}",
                    ShaderVariantToString(shaderConfig));
                continue;
            }

            shaderVariant.passType = passType;
            shaderVariant.keywords = shaderConfig.ShaderKeywords.Split(
                keywordSeparators, StringSplitOptions.RemoveEmptyEntries);

            shaderVariantCollection.Add(shaderVariant);
        }

        string absolutePath = UnityPathUtil.GetAbsolutePath(projectPath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        File.Delete(absolutePath);
        File.Delete(absolutePath + ".meta");

        AssetDatabase.CreateAsset(shaderVariantCollection, projectPath);
        AssetDatabase.Refresh();
    }
#endif
}