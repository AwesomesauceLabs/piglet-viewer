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
    public Vector3 ModelPositionRelativeToCamera;
    public float ModelSize;

    public float MouseRotateSpeed;
    public float MousePanSpeed;
    public float MouseZoomSpeed;

    private GameObject _model;

    void Start()
    {
#if UNITY_EDITOR
        _model = GLTFRuntimeImporter.Import(
            "C:/Users/Ben/test/gltf-models/Box.glb",
            OnImportProgress);
        InitModelTransformRelativeToCamera(_model, Camera);
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

        _model = GLTFRuntimeImporter.Import(data, OnImportProgress);
    }
#endif

    public void OnValidate()
    {
        if (ModelSize < 0.001f)
            ModelSize = 0.001f;

        if (MouseRotateSpeed < 0.01f)
            MouseRotateSpeed = 0.01f;
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
        float scaleFactor = ModelSize / size;
        model.transform.localScale = scale * scaleFactor;

        // Rotate model to face camera.

        model.transform.up = camera.transform.up;
        model.transform.forward = camera.transform.forward;

        // Translate model at standard offset from camera.

        bounds = BoundsUtil.GetRendererBoundsForHierarchy(model);
        if (!bounds.HasValue)
            return;

        model.transform.Translate(camera.transform.position
            + ModelPositionRelativeToCamera - bounds.Value.center);
    }

    public void Update()
    {
        if (_model == null)
            return;

        // left-click: rotate about model center point
        // (i.e. center of renderer bounds)

        if (Input.GetMouseButton(0)) {

            Bounds? bounds = BoundsUtil.GetRendererBoundsForHierarchy(_model);
            if (!bounds.HasValue)
                return;

            GameObject pivot = new GameObject("pivot");
            pivot.transform.position = bounds.Value.center;
            _model.transform.SetParent(pivot.transform, true);

            Vector3 rotation = new Vector3(
                Input.GetAxis("Mouse Y"),
                -Input.GetAxis("Mouse X"),
                0);

            pivot.transform.Rotate(rotation * Time.deltaTime * MouseRotateSpeed);

            _model.transform.SetParent(null, true);
            Destroy(pivot);

        }

        // middle-click / right-click: pan camera

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Vector3 translation = new Vector3(
                -Input.GetAxis("Mouse X"),
                -Input.GetAxis("Mouse Y"),
                0);

            Camera.transform.Translate(
                translation * Time.deltaTime * MousePanSpeed,
                Space.Self);
        }

        // mouse scroll wheel: zoom camera (i.e. move forward
        // on z-axis)

        float zoom = Input.GetAxis("Mouse ScrollWheel")
            * Time.deltaTime * MouseZoomSpeed;

        Camera.transform.Translate(new Vector3(0, 0, zoom),
            Space.Self);

    }


}
