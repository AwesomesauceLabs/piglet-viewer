using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityGLTF;

public class GameManager : MonoBehaviour
{
    public Camera Camera;
    public Vector3 ModelOffsetFromCamera;
    public float ModelSize;

    private GameObject _model;

    void Start()
    {

#if UNITY_EDITOR
        _model = GLTFRuntimeImporter.Import(
            "C:/Users/Ben/test/gltf-models/Box.glb",
            null, OnImportProgress);
#elif UNITY_WEBGL
        JsLib.Init();
#endif

    }

    bool OnImportProgress(string message, int count, int total)
    {
        Debug.LogFormat("{0} [{1}/{2}]", message, count, total);
        return true;
    }

#if UNITY_WEBGL
    public void ImportFileWebGl(string filename)
    {
        var size = JsLib.GetFileSize(filename);
        var jsData = JsLib.GetFileData(filename);

        var data = new byte[size];
        Marshal.Copy(jsData, data, 0, size);

        JsLib.FreeFileData(filename);

        if (_model != null)
            Destroy(_model);

        _model = GLTFRuntimeImporter.Import(
            "model.gltf", data, OnImportProgress);
    }
#endif

    public void OnValidate()
    {
        if (ModelSize < 0.001f)
            ModelSize = 0.001f;
    }

    public void InitModelTransform(GameObject model)
    {
        // Scale model up/down to a standard size, so that the
        // largest dimension of its bounding box is equal to `ModelSize`.

        Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(model);
        if (!bounds.HasValue)
            return;

        float size = bounds.Value.extents.MaxComponent();
        if (size < 0.000001f)
            return;

        Vector3 scale = model.transform.localScale;
        float scaleFactor = ModelSize / size;
        model.transform.localScale = scale * scaleFactor;

        // Rotate model to face camera.

        model.transform.forward = Camera.transform.forward;

        // Translate model at standard offset from camera.

        bounds = BoundsUtil.GetRendererBoundsForHierarchy(model);
        if (!bounds.HasValue)
            return;

        model.transform.Translate(Camera.transform.position 
            + ModelOffsetFromCamera - bounds.Value.center);
    }

}
