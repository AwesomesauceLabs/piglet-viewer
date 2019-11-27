#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using B83.Win32;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityGLTF;
using Debug = UnityEngine.Debug;

public class GameManager : MonoBehaviour
{
    public Camera Camera;
    public Vector3 ModelPositionRelativeToCamera;
    public float ModelSize;

    public float MouseRotateSpeed;
    public float MousePanSpeed;
    public float MouseZoomSpeed;

    /// <summary>
    /// Root game object for the currently loaded model.
    /// </summary>
    private GameObject _model;

    /// <summary>
    /// Handle to the currently running glTF import task.
    /// This task runs in the background and is polled
    /// for completion in `Update()`.
    /// </summary>
    private Task<GameObject> _importTask;
    
    /// <summary>
    /// A timer used to measure the time to import
    /// individual glTF entities (e.g. textures, meshes).
    /// </summary>
    private Stopwatch _stopwatch;

    /// <summary>
    /// The status message shown when a model is not
    /// currently being loaded.
    /// </summary>
    private string _idleStatusMessage;

    /// <summary>
    /// The status message shown along the bottom
    /// of the viewer window (e.g. "Loading mesh [3/9]").
    /// </summary>
    private string _statusMessage;

    /// <summary>
    /// An object that handles drawing UI elements
    /// (e.g. checkboxes, progress messages) on top
    /// of the model viewer window.
    /// </summary>
    private ViewerGUI _gui;

    private void Awake()
    {
        _gui = new ViewerGUI();
        _stopwatch = new Stopwatch();
    }
    
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

    UnityDragAndDropHook _dragAndDropHook;

    void Start()
    {
        _dragAndDropHook = new UnityDragAndDropHook();
        _dragAndDropHook.InstallHook();
        _dragAndDropHook.OnDroppedFiles += OnDropFiles;

        _idleStatusMessage = "Drag a .gltf/.glb file onto this window to view";
        _statusMessage = _idleStatusMessage;

        Import("C:/Users/Ben/test/gltf-models/Box.glb");

        _statusMessage = _idleStatusMessage;
    }

    void OnDestroy()
    {
        _dragAndDropHook.UninstallHook();
    }

    /// <summary>
    /// Callback for files that are drag-and-dropped onto game
    /// window. Only works in Windows standalone builds.
    /// </summary>
    void OnDropFiles(List<string> paths, POINT mousePos)
    {
        StartImportAsync(paths[0]);
    }

#endif

#if UNITY_WEBGL && !UNITY_EDITOR

    void Start()
    {
        _idleStatusMessage = "Click \"Browse\" to load a .gltf/.glb file";

        JsLib.Init();
    }

    public void ImportFileWebGl(string filename)
    {
        var size = JsLib.GetFileSize(filename);
        var jsData = JsLib.GetFileData(filename);

        var data = new byte[size];
        Marshal.Copy(jsData, data, 0, size);

        JsLib.FreeFileData(filename);

        if (_model != null)
            Destroy(_model);

        Import(data);
    }

#endif

    void Import(string path)
    {
        _stopwatch.Reset();
        _stopwatch.Start();
        _model = GLTFRuntimeImporter.Import(path, OnImportProgress);
        InitModelTransformRelativeToCamera(_model, Camera);
    }

    void Import(byte[] data)
    {
        _stopwatch.Reset();
        _stopwatch.Start();
        _model = GLTFRuntimeImporter.Import(data, OnImportProgress);
        InitModelTransformRelativeToCamera(_model, Camera);
    }

    void StartImportAsync(string path)
    {
        // if we are already importing a .gltf/.glb file
        if (_importTask != null)
            return;

        if (_model != null)
            Destroy(_model);

        // start import task in the background
        
        _stopwatch.Reset();
        _stopwatch.Start();
        _importTask = GLTFRuntimeImporter.ImportAsync(path, OnImportProgress);
    }
    
    void OnGUI()
    {
        _gui.OnGUI();
    }

    bool OnImportProgress(GLTFImporter.Type type, int count, int total)
    {
        _stopwatch.Stop();
        float milliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        _stopwatch.Start();
        
        _gui.OnProgress(type, count, total, milliseconds);
        
        _statusMessage = string.Format("loaded {0} {1}/{2} ({3} ms)",
            type.ToString().ToLower(), count, total, milliseconds);
        Debug.Log(_statusMessage);
        
        return true;
    }

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

    protected void HandleMouseInput()
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

    protected void HandleImportTaskCompletion()
    {
        if (_importTask == null || !_importTask.IsCompleted)
            return;

        // The following rethrows the exception without losing the
        // original stacktrace information. See:
        // https://stackoverflow.com/questions/20170527/how-to-correctly-rethrow-an-exception-of-task-already-in-faulted-state
        if (_importTask.IsFaulted)
            ExceptionDispatchInfo.Capture(_importTask.Exception).Throw();

        if (_importTask.IsCanceled) {
            _importTask = null;
            return;
        }

        _model = _importTask.Result;
        InitModelTransformRelativeToCamera(_model, Camera);
        _importTask = null;
    }

    public void Update()
    {
        HandleMouseInput();
        HandleImportTaskCompletion();
    }

}
