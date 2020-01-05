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
using UnityEngine.Networking;
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
    /// Used to generate progress messages in the GUI.
    /// </summary>
    private float _currentImportTypeMilliseconds;

    /// <summary>
    /// Reset state variables before importing a new
    /// glTF model.
    /// </summary>
    private void ResetImportState()
    {
        _gui.ResetLog();
        _stopwatch = new Stopwatch();
        _currentImportType = GLTFImporter.Type.None;
        _currentImportTypeMilliseconds = 0;
    }
    
    private void Awake()
    {
        _gui = new ViewerGUI();
        
        // By default, the Windows Unity Player will pause
        // execution when it loses focus.  Setting
        // `Application.runInBackground` to true overrides
        // this behaviour and tells it to keep running
        // always.
        //
        // The player must be continuously running in
        // order for drag-and-drop of files to work in
        // an intuitive manner.  Otherwise, dropping
        // a .gltf/.glb file onto a non-focused player
        // window will not immediately trigger an import,
        // and the user will have to additionally click
        // the window to give it focus again, before the
        // glTF import will start running.
        
        Application.runInBackground = true;
        
        ResetImportState();
    }
    
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

    UnityDragAndDropHook _dragAndDropHook;

    void Start()
    {
        _dragAndDropHook = new UnityDragAndDropHook();
        _dragAndDropHook.InstallHook();
        _dragAndDropHook.OnDroppedFiles += OnDropFiles;

        _gui.FooterMessage = "drag .gltf/.glb file onto window to view";

        StartImportAsync(UnityPathUtil.GetAbsolutePath(
            "Assets/PigletViewer/Resources/piglet-1.0.0.glb"));
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
        _gui.FooterMessage = "click \"Browse\" below to load a .gltf/.glb file";

        JsLib.Init();

#if false
        StartImportAsync("https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Models/master/2.0/BoxTextured/glTF-Binary/BoxTextured.glb");
#endif
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
        ResetImportState();
        _model = GLTFRuntimeImporter.Import(path, OnImportProgress);
        InitModelTransformRelativeToCamera(_model, Camera);
    }

    void Import(byte[] data)
    {
        ResetImportState();
        _model = GLTFRuntimeImporter.Import(data, OnImportProgress);
        InitModelTransformRelativeToCamera(_model, Camera);
    }

    void StartImportAsync(string path)
    {
        // If we attempt to import a model while an import
        // is already in progress, just ignore the request.
        //
        // TODO: This case would be more gracefully handled by
        // cancelling the existing import task, via a `CancellationToken`,
        // and starting the requested import immediately. This would
        // in turn require adding `CancellationToken` arguments
        // to the `GLTFImporter` interface methods.

        if (_importTask != null && !_importTask.IsCompleted)
            return;
            
        _importTask = ImportAsync(path);
    }

    private async Task<GameObject> ImportAsync(string path)
    {
        ResetImportState();

        Task<GameObject> task;
        
        if (path.StartsWith("http://") || path.StartsWith("https://"))
        {
            Debug.LogFormat("downloading {0}...", path);
            
            var request = new UnityWebRequest(path);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SendWebRequest();

            while (!request.isDone)
            {
                Debug.LogFormat("downloadProgress: {0}", request.downloadProgress);
                // note: `Task.Delay` does not work properly in WebGL builds
                await new WaitForSeconds(0.5f);
            }

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogFormat("http error: {0}", request.error);
                throw new Exception(string.Format(
                   "failed to download URI {0}: {1}",
                    path, request.error));
            }

            var data = request.downloadHandler.data;
            Debug.LogFormat("data.Length: {0}", data.Length);

            task = GLTFRuntimeImporter
                .ImportAsync(request.downloadHandler.data, OnImportProgress);
        }
        else
        {
            task = GLTFRuntimeImporter
                .ImportAsync(path, OnImportProgress);
        }
        
        try
        {
            await task;
        }
        catch (Exception e)
        {
            _gui.FooterMessage = string.Format(
                "error: {0}", e.Message);
        }

        if (_model != null)
            Destroy(_model);
            
        _gui.ResetSpin();
        
        _model = task.Result;
        InitModelTransformRelativeToCamera(_model, Camera);
        
        return _model;
    }

    /// <summary>
    /// Rotate a GameObject hierarchy about its center, as determined
    /// by the MeshRenderer bounds of the GameObjects in the hierarchy.
    /// </summary>
    protected void RotateAboutCenter(GameObject model, Vector3 rotation)
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
        
#if UNITY_WEBGL && !UNITY_EDITOR        
        if (type == _currentImportType)
            JsLib.UpdateTailLogLine(message);
        else
            JsLib.AppendLogLine(message);
#else
        if (type == _currentImportType)
            _gui.Log[_gui.Log.Count - 1] = message;
        else
            _gui.Log.Add(message);
#endif

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

    public void Update()
    {
        SpinModel();
    }

    /// <summary>
    /// Auto-rotate model as per "Spin X" / "Spin Y" sliders in GUI.
    /// </summary>
    public void SpinModel()
    {
        if (_model == null)
            return;
        
        Vector3 rotation = new Vector3(_gui.SpinY, -_gui.SpinX, 0)
           * Time.deltaTime * SpinSpeed;
        
        RotateAboutCenter(_model, rotation);
    }
}
