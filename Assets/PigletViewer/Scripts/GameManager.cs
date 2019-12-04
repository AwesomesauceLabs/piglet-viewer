#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using B83.Win32;
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
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

    public float SpinSpeed;

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

    /// <summary>
    /// The current type of glTF entity that is being
    /// imported (e.g. textures, meshes).  This variable
    /// is used to sum the import times for entities
    /// of the same type, and to report the total import
    /// time that type on a single line of the progress
    /// log.
    /// </summary>
    private GLTFImporter.Type _currentImportType;

    /// <summary>
    /// The total time spent importing glTF entities of
    /// the current type (e.g. textures, meshes).
    /// Used for generated progress messages in the GUI.
    /// </summary>
    private float _currentImportTypeMilliseconds;

    /// <summary>
    /// Reset state variables before importing a new
    /// glTF model.
    /// </summary>
    private void Reset()
    {
        _gui = new ViewerGUI();
        _stopwatch = new Stopwatch();
        _currentImportType = GLTFImporter.Type.None;
        _currentImportTypeMilliseconds = 0;
    }
    
    private void Awake()
    {
        Reset();
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
        Reset();
        _model = GLTFRuntimeImporter.Import(path, OnImportProgress);
        InitModelTransformRelativeToCamera(_model, Camera);
    }

    void Import(byte[] data)
    {
        Reset();
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

        Reset();
        _importTask = GLTFRuntimeImporter.ImportAsync(path, OnImportProgress);
    }

    /// <summary>
    /// Rotate a GameObject hierarchy about its center, as determined
    /// by the MeshRenderer bounds of the GameObjects in the hierarchy.
    /// </summary>
    protected void RotateAboutCenter(GameObject model, Vector3 rotation)
    {
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
    
    /// <summary>
    /// Handle any mouse events that are not consumed by IMGUI controls
    /// (e.g. checkboxes, sliders).  This method is used to implement
    /// the conventional mouse behaviour for rotating the model, panning the
    /// camera, and zooming the camera.
    /// </summary>
    protected void HandleUnusedMouseEvents()
    {
        // if any GUI element (e.g. checkbox, slider) currently
        // has focus

        if (GUIUtility.hotControl != 0)
            return;

        Event @event = Event.current;

        if (@event.type == EventType.MouseDown)
        {
            // stop auto-spin ("Spin X" / "Spin Y")
            // whenever the user clicks on the
            // model/background.

            _gui.SpinX = 0;
            _gui.SpinY = 0;
        }
        
        if (@event.type == EventType.MouseDrag)
        {
            // drag with left mouse button -> rotate model
            if (@event.button == 0)
            {
                Vector3 rotation = new Vector3(
                   -@event.delta.y, -@event.delta.x, 0)
                   * MouseRotateSpeed;
                
                RotateAboutCenter(_model, rotation);
            }

            // drag with right mouse button -> pan camera
            if (@event.button == 1)
            {
                Vector3 pan = new Vector3(
                    -@event.delta.x, @event.delta.y, 0)
                    * MousePanSpeed;
                
                Camera.transform.Translate(pan, Space.Self);
            }
        }

        // mouse scroll wheel -> zoom camera
        if (@event.type == EventType.ScrollWheel)
        {
            Vector3 zoom = new Vector3(0, 0, -@event.delta.y)
                * MouseZoomSpeed;
            
            Camera.transform.Translate(zoom, Space.Self);
        }
    }
    
    void OnGUI()
    {
        _gui.OnGUI();
        HandleUnusedMouseEvents();
    }

    bool OnImportProgress(GLTFImporter.Type type, int count, int total)
    {
        _stopwatch.Stop();
        float milliseconds = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Reset();
        _stopwatch.Start();

        // sum import times for glTF entities of the same type
        // (e.g. textures, meshes)

        if (type == _currentImportType)
            _currentImportTypeMilliseconds += milliseconds;
        else
            _currentImportTypeMilliseconds = milliseconds;

        string message;
        if (count < total) {
            message = string.Format("Loaded {0} {1}/{2}...",
                type.ToString().ToLower(), count, total);
        } else {
            message = string.Format("Loaded {0} {1}/{2}... done ({3} ms)",
                type.ToString().ToLower(), count, total,
                _currentImportTypeMilliseconds);
        }

        // Update existing tail log line if we are still importing
        // the same type of glTF entity (e.g. textures), or
        // add a new line if we have started to import
        // a new type.
        
        if (type == _currentImportType)
            _gui.Log[_gui.Log.Count - 1] = message;
        else
            _gui.Log.Add(message);

        Debug.Log(message);

        _currentImportType = type;
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
        HandleImportTaskCompletion();
        
        // auto-rotate model as per "Spin X" / "Spin Y"
        // sliders in GUI
        
        Vector3 rotation = new Vector3(_gui.SpinY, -_gui.SpinX, 0)
           * Time.deltaTime * SpinSpeed;
        
        RotateAboutCenter(_model, rotation);
    }
}
