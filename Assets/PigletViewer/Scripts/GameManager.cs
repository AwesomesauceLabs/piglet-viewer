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

public class GameManager : Singleton<GameManager>
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
    private ImportTask _importTask;
    
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
        ViewerGUI.Instance.ResetLog();
        _progressTracker = new ImportProgressTracker();
    }
    
    private void Awake()
    {
        ResetImportState();
        
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        gameObject.AddComponent<WindowsViewerBehaviour>();
#elif UNITY_ANDROID
        gameObject.AddComponent<AndroidViewerBehaviour>();
#elif UNITY_WEBGL
        gameObject.AddComponent<WebGlViewerBehaviour>();
#endif
    }

    public void Import(byte[] data)
    {
        if (_model != null)
            Destroy(_model);

        ResetImportState();
        _model = GLTFRuntimeImporter.Import(data, OnImportProgress);
        _model.AddComponent<ModelBehaviour>();
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
    public void StartImport(string uriStr)
    {
        Uri uri = new Uri(uriStr);
        
        ImportTask importJob = GLTFRuntimeImporter
            .GetImportTask(uri, OnImportProgress);

        importJob.OnCompleted += OnImportCompleted;
        importJob.OnException += OnImportException;
        importJob.RethrowExceptionAfterCallbacks = false;
        
        StartImport(importJob);
    }

    public void StartImport(ImportTask importTask)
    {
        ResetImportState();
        _progressTracker.StartImport();
        _importTask = importTask;
    }

    /// <summary>
    /// Rotate the current loaded model (if any) about
    /// its center, as per the given Euler angles.
    /// </summary>
    public void RotateModel(Vector3 rotation)
    {
        if (_model == null)
            return;
        
        _model.GetComponent<ModelBehaviour>().RotateAboutCenter(
            rotation * MouseRotateSpeed);
    }

    /// <summary>
    /// Move the camera as per the given displacement vector.
    /// </summary>
    public void PanCamera(Vector3 pan)
    {
        if (Camera == null)
            return;

        Camera.transform.Translate(pan * MousePanSpeed, Space.Self);
    }

    /// <summary>
    /// Move the camera along the Z-axis, towards/away from the model.
    /// </summary>
    public void ZoomCamera(float deltaZ)
    {
        Vector3 zoom = new Vector3(0, 0, deltaZ);
        Camera.transform.Translate(zoom * MouseZoomSpeed, Space.Self);
    }
    
    public void OnImportProgress(GLTFImporter.ImportStep importStep, int numCompleted, int total)
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
        List<string> log = ViewerGUI.Instance.Log;
        if (_progressTracker.IsNewImportStep())
            log.Add(message);
        else
            log[log.Count - 1] = message;
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

    /// <summary>
    /// Invoked after a model has been successfully imported.
    /// </summary>
    public void OnImportCompleted(GameObject model)
    {
        ViewerGUI.Instance.ResetSpin();

        if (_model != null)
            Destroy(_model);

        _model = model;
        _model.AddComponent<ModelBehaviour>();

        _importTask = null;
    }

    /// <summary>
    /// Invoked when an exception is thrown during model import.
    /// </summary>
    public void OnImportException(Exception e)
    {
        ViewerGUI.Instance.FooterMessage = string.Format(
            "error: {0}", e.Message);
        
        _importTask = null;
    }
    
    /// <summary>
    /// Unity callback that is invoked once per frame.
    /// </summary>
    public void Update()
    {
        // advance import job
        _importTask?.MoveNext();
    }

}
