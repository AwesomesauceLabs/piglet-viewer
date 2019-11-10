using System.Collections;
using System.Collections.Generic;
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
        bool OnProgress(string message, int count, int total)
        {
            Debug.LogFormat("{0} [{1}/{2}]", message, count, total);
            return true;
        }

        _model = GLTFRuntimeImporter.Import(
            "C:/Users/Ben/test/gltf-models/cannon-cleaner/model.gltf",
            OnProgress);
    }

    public void OnValidate()
    {
        if (ModelSize < 0.001f)
            ModelSize = 0.001f;
    }

    public void UpdateModelTransform(GameObject model)
    {
        if (model == null)
            return;

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

    public void Update()
    {
        UpdateModelTransform(_model);
    }
}
