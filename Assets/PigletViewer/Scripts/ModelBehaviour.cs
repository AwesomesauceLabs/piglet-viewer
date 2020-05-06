using System.Collections;
using System.Collections.Generic;
using Piglet;
using PigletViewer;
using UnityEngine;

public class ModelBehaviour : MonoBehaviour
{
    /// <summary>
    /// Unity callback that is invoked before the first frame.
    /// </summary>
    void Start()
    {
        InitModelTransformRelativeToCamera();
    }

    /// <summary>
    /// Unity callback that is invoked once per frame.
    /// </summary>
    void Update()
    {
        SpinModel();
    }
    
    /// <summary>
    /// Auto-rotate model as per "Spin X" / "Spin Y" sliders in GUI.
    /// </summary>
    public void SpinModel()
    {
        ViewerGUI gui = ViewerGUI.Instance;
        
        Vector3 rotation = new Vector3(gui.SpinY, -gui.SpinX, 0)
           * Time.deltaTime * GameManager.Instance.SpinSpeed;
        
        RotateAboutCenter(rotation);
    }

    /// <summary>
    /// Rotate a GameObject hierarchy about its center, as determined
    /// by the MeshRenderer bounds of the GameObjects in the hierarchy.
    /// </summary>
    public void RotateAboutCenter(Vector3 rotation)
    {
        Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(gameObject);
        if (!bounds.HasValue)
            return;

        GameObject pivot = new GameObject("pivot");
        pivot.transform.position = bounds.Value.center;
        transform.SetParent(pivot.transform, true);

        pivot.transform.Rotate(rotation);

        transform.SetParent(null, true);
        Destroy(pivot);
    }

    public void InitModelTransformRelativeToCamera()
    {
        Transform cameraTransform = CameraBehaviour.Instance.transform;

        // Scale model up/down to a standard size, so that the
        // largest dimension of its bounding box is equal to `ModelSize`.

        Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(gameObject);
        if (!bounds.HasValue)
            return;

        float size = bounds.Value.extents.MaxComponent();
        if (size < 0.000001f)
            return;

        Vector3 scale = transform.localScale;
        float scaleFactor = GameManager.Instance.ModelSize / size;
        transform.localScale = scale * scaleFactor;

        // Rotate model to face camera.

        transform.up = cameraTransform.up;
        transform.forward = cameraTransform.forward;

        // Translate model at standard offset from camera.

        bounds = BoundsUtil.GetRendererBoundsForHierarchy(gameObject);
        if (!bounds.HasValue)
            return;

        transform.Translate(cameraTransform.position
            + GameManager.Instance.ModelPositionRelativeToCamera
            - bounds.Value.center);
    }

}
