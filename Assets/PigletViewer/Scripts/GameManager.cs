using System;
using System.IO;
using Piglet;
using UnityEngine;
using UnityGLTF;

public class GameManager : Singleton<GameManager>
{
    public Vector3 ModelPositionRelativeToCamera;
    public float ModelSize;

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
    private GltfImportTask _importTask;

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
        Uri uri = new Uri(filename, UriKind.Relative);

        GltfImportTask importTask = RuntimeGltfImporter
            .GetImportTask(data,
                ImportLog.Instance.OnImportProgress);

        StartImport(importTask, uri);
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

        GltfImportTask importTask = RuntimeGltfImporter
            .GetImportTask(uri,
                ImportLog.Instance.OnImportProgress);

        StartImport(importTask, uri);
    }

    public void StartImport(GltfImportTask importTask, Uri uri)
    {
        ImportLog.Instance.StartImport();

        string basename = Path.GetFileName(uri.ToString());
        string message = String.Format("Loading {0}...", basename);
        ImportLog.Instance.AddLine(message);

        importTask.OnCompleted += OnImportCompleted;
        importTask.OnException += OnImportException;
        importTask.RethrowExceptionAfterCallbacks = false;

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

        _model.GetComponent<ModelBehaviour>().RotateAboutCenter(rotation);
    }

    public void OnValidate()
    {
        if (ModelSize < 0.001f)
            ModelSize = 0.001f;
    }

    /// <summary>
    /// Invoked after a model has been successfully imported.
    /// </summary>
    public void OnImportCompleted(GameObject model)
    {
        ViewerGUI.Instance.ResetSpin();
        ViewerGUI.Instance.ResetFooterMessage();

        ImportLog.Instance.AddLine(
            String.Format("Total import time: {0} ms",
                ImportLog.Instance.Stopwatch.ElapsedMilliseconds));

        ImportLog.Instance.AddLine(
            String.Format("Longest Unity thread stall: {0} ms",
                _importTask.LongestStepInMilliseconds()));

        ImportLog.Instance.AddLine("Success!");

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
        ViewerGUI.Instance.ShowDialogBox("Failed to Load Model",
            StringUtil.WrapText(e.Message, 50));

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
