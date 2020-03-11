using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelBehaviour : MonoBehaviour
{
    /// <summary>
    /// Unity callback that is invoked before the first frame.
    /// </summary>
    void Start()
    {
        InitModelTransformRelativeToCamera(GameManager.Instance.Camera);
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
        ViewerGUI gui = GameManager.Instance.Gui;
        
        Vector3 rotation = new Vector3(gui.SpinY, -gui.SpinX, 0)
           * Time.deltaTime * GameManager.Instance.SpinSpeed;
        
        RotateAboutCenter(gameObject, rotation);
    }

    /// <summary>
    /// Rotate a GameObject hierarchy about its center, as determined
    /// by the MeshRenderer bounds of the GameObjects in the hierarchy.
    /// </summary>
    public void RotateAboutCenter(GameObject model, Vector3 rotation)
    {
        if (model == null)
            return;
        
        Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(model);
        if (!bounds.HasValue)
            return;

        GameObject pivot = new GameObject("pivot");
        pivot.transform.position = bounds.Value.center;
        model.transform.SetParent(pivot.transform, true);

        pivot.transform.Rotate(rotation);

        model.transform.SetParent(null, true);
        Destroy(pivot);
    }
    
    public void InitModelTransformRelativeToCamera(Camera camera)
    {
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

        transform.up = camera.transform.up;
        transform.forward = camera.transform.forward;

        // Translate model at standard offset from camera.

        bounds = BoundsUtil.GetRendererBoundsForHierarchy(gameObject);
        if (!bounds.HasValue)
            return;

        transform.Translate(camera.transform.position
            + GameManager.Instance.ModelPositionRelativeToCamera
            - bounds.Value.center);
    }

}
