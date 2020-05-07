using System.Collections;
using System.Collections.Generic;
using Piglet;
using PigletViewer;
using UnityEngine;

public class ModelBehaviour : Singleton<ModelBehaviour>
{
    /// <summary>
    /// The root GameObject of the most recently loaded model,
    /// i.e. the model that is currently being viewed by
    /// the user. (This application only allows viewing one
    /// model at a time.)
    /// </summary>
    private GameObject _model;

    /// <summary>
    /// Unity callback that is invoked once per frame.
    /// </summary>
    void Update()
    {
        SpinModel();
    }

    /// <summary>
    /// Set the current model (if any), with the given model
    /// and make it a child of this game object.
    ///
    /// Adjust the transform of this game object so that the
    /// model has a standard size, distance, and orientation
    /// relative to the main camera.
    /// </summary>
    /// <param name="model"></param>
    public void SetModel(GameObject model)
    {
        if (_model != null)
        {
            if (Application.isPlaying)
                Destroy(_model);
            else
                DestroyImmediate(_model);
        }

        _model = model;

        // Attach the model to this game object
        // (the ModelBehaviour singleton).

        if (model == null)
            return;

        // Initialize the transform of the model so that:
        //
        // (1) the model is a standard size
        // (2) the model is a standard distance from the main camera
        // (3) the model is facing the camera

        InitModelTransformRelativeToCamera();
    }

    /// <summary>
    /// Auto-rotate model as per "Spin X" / "Spin Y" sliders in GUI.
    /// </summary>
    public void SpinModel()
    {
        ViewerGUI gui = ViewerGUI.Instance;

        Vector3 rotation = new Vector3(gui.SpinY, -gui.SpinX, 0)
           * Time.deltaTime * ViewerGUI.Instance.SpinSpeed;

        RotateAboutCenter(rotation);
    }

    /// <summary>
    /// Rotate a GameObject hierarchy about its center, as determined
    /// by the MeshRenderer bounds of the GameObjects in the hierarchy.
    /// </summary>
    public void RotateAboutCenter(Vector3 rotation)
    {
        if (_model == null)
            return;

        Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(_model);
        if (!bounds.HasValue)
            return;

        GameObject pivot = new GameObject("pivot");
        pivot.transform.position = bounds.Value.center;
        _model.transform.SetParent(pivot.transform, true);

        pivot.transform.Rotate(rotation);

        _model.transform.SetParent(null, true);
        Destroy(pivot);
    }

    /// <summary>
    /// Adjust the root transform of the model when it is
    /// first loaded, so that every model: (1) has the same
    /// initial size, (2) has the same initial distance from
    /// the camera, and (3) is initially rotated to face the
    /// camera.
    /// </summary>
    public void InitModelTransformRelativeToCamera()
    {
        if (_model == null)
            return;

        Transform cameraTransform = CameraBehaviour.Instance.transform;

        // Scale model up/down to a standard size, so that the
        // largest dimension of its bounding box is equal to `ModelSize`.

        Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(_model);
        if (!bounds.HasValue)
            return;

        float size = bounds.Value.extents.MaxComponent();
        if (size < 0.000001f)
            return;

        Vector3 scale = _model.transform.localScale;
        float scaleFactor = GameManager.Instance.ModelSize / size;
        _model.transform.localScale = scale * scaleFactor;

        // Rotate model to face camera.

        _model.transform.up = cameraTransform.up;
        _model.transform.forward = cameraTransform.forward;

        // Translate model at standard offset from camera.

        bounds = BoundsUtil.GetRendererBoundsForHierarchy(_model);
        if (!bounds.HasValue)
            return;

        _model.transform.Translate(cameraTransform.position
            + GameManager.Instance.ModelPositionRelativeToCamera
            - bounds.Value.center);
    }

}
