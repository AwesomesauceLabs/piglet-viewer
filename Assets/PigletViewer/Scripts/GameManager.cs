#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using B83.Win32;
#endif

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Text;
using Piglet;
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
    /// Handle to the currently running glTF import job.
    /// This task runs in the background and is
    /// incrementally advanced by calling
    /// `PumpImportJob` in `Update`.
    /// </summary>
    private ImportTask _importJob;
    
    /// <summary>
    /// An object that handles drawing UI elements
    /// (e.g. checkboxes, progress messages) on top
    /// of the model viewer window.
    /// </summary>
    private ViewerGUI _gui;

    /// <summary>
    /// Times import steps, and generates nicely formatted
    /// progress messages.
    /// </summary>
    private ImportProgressTracker _progressTracker;
    
    /// <summary>
    /// Reset state variables before importing a new
    /// glTF model.
    /// </summary>
    private void ResetImportState()
    {
        _gui.ResetLog();
        _progressTracker = new ImportProgressTracker();
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
        
        StartImport(Path.Combine(
            Application.streamingAssetsPath, "piglet-1.0.0.glb"));
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
        StartImport(paths[0]);
    }

#endif

#if UNITY_WEBGL && !UNITY_EDITOR

    void Start()
    {
        _gui.FooterMessage = "click \"Browse\" below to load a .gltf/.glb file";

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

    /// <summary>
    /// Create a glTF import task, which will be incrementally advanced
    /// in each call to Update().
    ///
    /// Note that the URI argument must be passed in as a string
    /// rather than a `Uri` object, so that this method
    /// can be invoked from javascript.
    /// </summary>
    /// <param name="uriStr">The URI of the input glTF file.</param>
    void StartImport(string uriStr)
    {
        Uri uri = new Uri(uriStr);
        
        ResetImportState();
        
        _progressTracker.StartImport();
        
        _importJob = GLTFRuntimeImporter
            .GetImportTask(uri, OnImportProgress);

        _importJob.OnCompleted += OnImportCompleted;
        _importJob.OnException += OnImportException;
        _importJob.RethrowExceptionAfterCallbacks = false;
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

    void OnImportProgress(GLTFImporter.ImportStep importStep, int numCompleted, int total)
    {
        _progressTracker.UpdateProgress(importStep, numCompleted, total);

        string message = _progressTracker.GetProgressMessage();

        // Update existing tail log line if we are still importing
        // the same type of glTF entity (e.g. textures), or
        // add a new line if we have started to import
        // a new type.
        
#if UNITY_WEBGL && !UNITY_EDITOR        
        if (_progressTracker.IsNewImportStep())
            JsLib.AppendLogLine(message);
        else
            JsLib.UpdateTailLogLine(message);
#else
        if (_progressTracker.IsNewImportStep())
            _gui.Log.Add(message);
        else
            _gui.Log[_gui.Log.Count - 1] = message;
#endif

        Debug.Log(message);
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

    /// <summary>
    /// Invoked after a model has been successfully imported.
    /// </summary>
    private void OnImportCompleted(GameObject model)
    {
        _gui.ResetSpin();

        if (_model != null)
            Destroy(_model);

        _model = model;
        InitModelTransformRelativeToCamera(_model, Camera);

        _importJob = null;
    }

    /// <summary>
    /// Invoked when an exception is thrown during model import.
    /// </summary>
    private void OnImportException(Exception e)
    {
        _gui.FooterMessage = string.Format(
            "error: {0}", e.Message);
        
        _importJob = null;
    }
    
    /// <summary>
    /// Unity callback that is invoked once per frame.
    /// </summary>
    public void Update()
    {
        // advance import job
        _importJob?.MoveNext();
        
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
