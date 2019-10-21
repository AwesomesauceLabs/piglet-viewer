using System.Collections.Generic;
using UnityEngine;

public class GLTFRuntimeImporterCache
{
    /// <summary>
    /// Binary data buffers loaded from GLTF file.
    /// </summary>
    public List<byte[]> Buffers;
    /// <summary>
    /// Images loaded from GLTF file.
    /// </summary>
    public List<Texture2D> Images;
    /// <summary>
    /// Textures loaded from GLTF file. In GLTF, textures
    /// are images with additional parameters applied
    /// (e.g. scaling, filtering).
    /// </summary>
    public List<Texture2D> Textures;
    /// <summary>
    /// Materials imported from GLTF file.
    /// </summary>
    public List<Material> Materials;
    /// <summary>
    /// Meshes imported from GLTF file. In GLTF, meshes
    /// consist of one or more submeshes called "primitives",
    /// where each primitive can have a different material.
    /// Here the outer list are the top-level meshes and the inner
    /// lists are the primitives that make up each mesh.
    /// </summary>
    public List<List<KeyValuePair<Mesh,Material>>> Meshes;

    public GLTFRuntimeImporterCache()
    {
        Buffers = new List<byte[]>();
        Images = new List<Texture2D>();
        Textures = new List<Texture2D>();
        Materials = new List<Material>();
        Meshes = new List<List<KeyValuePair<Mesh,Material>>>();
    }
}