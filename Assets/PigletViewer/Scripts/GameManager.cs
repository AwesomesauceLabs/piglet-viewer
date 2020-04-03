﻿#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
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
    /// Unity callback that is invoked before the first frame update
    /// and prior to Start().
    /// </summary>
    private void Awake()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        gameObject.AddComponent<WindowsViewerBehaviour>();
#elif UNITY_ANDROID
        gameObject.AddComponent<AndroidViewerBehaviour>();
#elif UNITY_WEBGL
        gameObject.AddComponent<WebGlViewerBehaviour>();
#endif
    }

    /// <summary>
    /// Create a glTF import task, which will be incrementally advanced
    /// in each call to Update().
    ///
    /// This version of StartImport is only used by the WebGL viewer.
    /// </summary>
    public void StartImport(byte[] data, string filename)
    {
        Uri uri = new Uri(filename);

        ImportTask importJob = GLTFRuntimeImporter
            .GetImportTask(data, OnImportProgress);

        importJob.OnCompleted += OnImportCompleted;
        importJob.OnException += OnImportException;
        importJob.RethrowExceptionAfterCallbacks = false;

        StartImport(importJob, uri);
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
        
        StartImport(importJob, uri);
    }

    public void StartImport(ImportTask importTask, Uri uri)
    {
        string basename = uri.Segments[uri.Segments.Length - 1];
        string message = String.Format("Loading \"{0}\"...", basename);
        ImportProgressTracker.Instance.Log.Add(message);
        
        ImportProgressTracker.Instance.StartImport();

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
        ImportProgressTracker.Instance.UpdateProgress(importStep, numCompleted, total);
        string message = ImportProgressTracker.Instance.GetProgressMessage();

        // Update existing tail log line if we are still importing
        // the same type of glTF entity (e.g. textures), or
        // add a new line if we have started to import
        // a new type.
        
        if (ImportProgressTracker.Instance.IsNewImportStep())
            ImportProgressTracker.Instance.AddLine(message);
        else
            ImportProgressTracker.Instance.ReplaceLastLine(message);

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
        ViewerGUI.Instance.ResetFooterMessage();

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
