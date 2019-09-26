using System.Collections.Generic;
using UnityEngine;

public class GLTFRuntimeImporterCache
{
    public List<byte[]> Buffers;
    public List<Texture2D> Images;
    public List<Texture2D> Textures;
    public List<Material> Materials;

    public GLTFRuntimeImporterCache()
    {
        Buffers = new List<byte[]>();
        Images = new List<Texture2D>();
        Textures = new List<Texture2D>();
        Materials = new List<Material>();
    }
}