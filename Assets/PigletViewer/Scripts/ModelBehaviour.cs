﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelBehaviour : MonoBehaviour
{
    /// <summary>
    /// Unity callback that is invoked before the first frame.
    /// </summary>
    void Start()
    {
        InitModelTransformRelativeToCamera(
            gameObject, GameManager.Instance.Camera);
    }

    /// <summary>
    /// Unity callback that is invoked once per frame.
    /// </summary>
    void Update()
    {
    }
    
    public void InitModelTransformRelativeToCamera(
        GameObject model, Camera camera)
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
        float scaleFactor = GameManager.Instance.ModelSize / size;
        model.transform.localScale = scale * scaleFactor;

        // Rotate model to face camera.

        model.transform.up = camera.transform.up;
        model.transform.forward = camera.transform.forward;

        // Translate model at standard offset from camera.

        bounds = BoundsUtil.GetRendererBoundsForHierarchy(model);
        if (!bounds.HasValue)
            return;

        model.transform.Translate(camera.transform.position
            + GameManager.Instance.ModelPositionRelativeToCamera
            - bounds.Value.center);
    }

}