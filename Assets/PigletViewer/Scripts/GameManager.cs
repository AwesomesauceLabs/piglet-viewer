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
    private ImportTask _importJob;
    
    /// <summary>
    /// An object that handles drawing UI elements
    /// (e.g. checkboxes, progress messages) on top
    /// of the model viewer window.
    /// </summary>
    public ViewerGUI Gui;

    /// <summary>
    /// Times import steps, and generates nicely formatted
    /// progress messages.
    /// </summary>
    private ImportProgressTracker _progressTracker;
    
    /// <summary>
    /// The number fingers that were touching the screen during
    /// the previous frame (Android).
    /// </summary>
    private int _prevTouchCount = 0;
    
    /// <summary>
    /// The distance between the two fingers touching
    /// the screen during the previous frame (Android).  Null
    /// if there weren't two fingers touching the screen
    /// during the previous frame.
    /// </summary>
    private float? _prevPinchDist;
    
    /// <summary>
    /// The midpoint between the two fingers touching
    /// the screen during the previous frame (Android). Null
    /// if there weren't two fingers touching the screen
    /// during the previous frame.
    /// </summary>
    private Vector2? _prevPinchMidpoint;

    /// <summary>
    /// Possible actions to perform on the 3D model,
    /// based on the currently pressed mouse button(s) or
    /// number of fingers touching the screen (Android).
    /// </summary>
    [Flags]
    private enum MouseAction
    {
        None = 0,
        Rotate = 1,
        Pan = 1 << 1,
        Zoom = 1 << 2,
    };

    /// <summary>
    /// Reset state variables before importing a new
    /// glTF model.
    /// </summary>
    private void ResetImportState()
    {
        Gui.ResetLog();
        _progressTracker = new ImportProgressTracker();
    }
    
    private void Awake()
    {
        Gui = new ViewerGUI();
        ResetImportState();
        
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        gameObject.AddComponent<WindowsViewerBehaviour>();
#elif UNITY_ANDROID
        gameObject.AddComponent<AndroidViewerBehaviour>();
#endif
    }
    
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

    void Import(byte[] data)
    {
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
        _importJob = importTask;
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

        MouseAction mouseActions = MouseAction.None;
        bool mouseDown = false;
        float deltaX = 0f;
        float deltaY = 0f;
        float deltaZ = 0f;

        if (Input.touchSupported)
        {
            // Handle touch screen input (Android).
            //
            // To ensure that Touch processing only happens
            // once per frame, we only process Touch input
            // on EventType.Repaint events.  Otherwise,
            // the speed of model rotation/zooming/panning
            // will depend on the number of GUI events per
            // frame.
            //
            // Note 1: EventType.Repaint happens before
            // GUIUtility.hotControl is set by mouse events
            // (e.g. EventType.MouseDown), so the model will
            // continue to rotate/zoom/pan in response to Touch input,
            // even if the user is interacting with IMGUI
            // controls (e.g. sliders, checkboxes).  This is
            // undesirable behaviour, but for the time being it
            // doesn't matter because there are no interactive
            // controls shown on Android.  In particular,
            // I've disabled the "Spin X" / "Spin Y" sliders
            // on Android because they are too small and
            // difficult to interact with.
            //
            // Note 2: This code uses the `Touch` class
            // from Unity's old input system ("Input Manager").
            // At the time of coding, I was not aware that there
            // was a newer Unity input system ("Input System"), 
            // introduced in Unity 2019.1, which provides a new touch
            // input API via `InputSystem.EnhancedTouch.Touch`.
            // See https://forum.unity.com/threads/inputsystem-enhancedtouch-touch-and-unity-ads.779351/
            // The code below works fine and is likely to
            // be supported by Unity for a long time. But if I
            // ever need to make major changes, I should consider
            // using the new input system.
            //
            // Note 3: The old input system (see Note 2 above) simulates
            // IMGUI mouse events in response to touch inputs, whereas
            // the new input system does not. For example e.g. an
            // EventType.MouseDown will be generated
            // in response to a finger touching the screen. The fact
            // that the new input system ("Input System") doesn't simulate
            // mouse events is considered a bug and as of Mar 10, 2020
            // it has not been fixed. See
            // https://forum.unity.com/threads/inputsystem-enhancedtouch-touch-and-unity-ads.779351/
            // for discussion.

            if (@event.type != EventType.Repaint)
                return;

            // if finger(s) were lifted from screen
            if (Input.touchCount == 0)
            {
                _prevTouchCount = Input.touchCount;
                _prevPinchDist = null;
                _prevPinchMidpoint = null;
                return;
            }

            if (_prevTouchCount == 0 && Input.touchCount > 0)
            {
                // perform mouse click actions when finger(s) first touch screen
                mouseDown = true;
            }
            else if (_prevTouchCount == Input.touchCount)
            {
                // perform mouse drag actions while number of fingers
                // touching screen is > 0 and does not change

                if (Input.touchCount == 1)
                {
                    // one-finger drag -> rotate model
                    
                    Touch touch = Input.GetTouch(0);
                    
                    deltaX = touch.deltaPosition.x * 0.3f;
                    deltaY = -touch.deltaPosition.y * 0.3f;

                    mouseActions |= MouseAction.Rotate;
                }
                else if (Input.touchCount == 2)
                {
                    Touch touch0 = Input.GetTouch(0);
                    Touch touch1 = Input.GetTouch(1);

                    // two fingers pinch -> zoom

                    float pinchDist = (touch1.position - touch0.position).magnitude;

                    if (_prevPinchDist.HasValue)
                    {
                        mouseActions |= MouseAction.Zoom;
                        
                        float pinchDelta = pinchDist - _prevPinchDist.Value;
                        deltaZ = pinchDelta * 0.03f;
                    }

                    _prevPinchDist = pinchDist;
                    
                    // two-finger drag -> pan
                    
                    Vector2 pinchMidpoint = (touch0.position + touch1.position) / 2.0f;
                    if (_prevPinchMidpoint.HasValue)
                    {
                        mouseActions |= MouseAction.Pan;

                        Vector2 deltaMidpoint
                            = pinchMidpoint - _prevPinchMidpoint.Value;
                        
                        deltaX = deltaMidpoint.x * 0.3f;
                        deltaY = -deltaMidpoint.y * 0.3f;
                    }

                    _prevPinchMidpoint = pinchMidpoint;
                }
            }

            _prevTouchCount = Input.touchCount;
        }
        else
        {
            // handle mouse input for rotating/zooming/panning the model

            deltaX = @event.delta.x;
            deltaY = @event.delta.y;
            deltaZ = 0f;
            
            switch (@event.type)
            {
                case EventType.MouseDown:
                    mouseDown = true;
                    break;
                case EventType.MouseDrag:
                    if (@event.button == 0)
                        mouseActions = MouseAction.Rotate;
                    else if (@event.button == 1)
                        mouseActions = MouseAction.Pan;
                    break;
                case EventType.ScrollWheel:
                    mouseActions = MouseAction.Zoom;
                    // note: Unity passes in mouse scroll wheel
                    // change via deltaY
                    deltaZ = -deltaY;
                    break;
            }
        }
        
        if (mouseDown)
        {
            // stop auto-spin ("Spin X" / "Spin Y")
            // whenever the user clicks on the
            // model/background.

            Gui.SpinX = 0;
            Gui.SpinY = 0;
        }

        if (mouseActions.HasFlag(MouseAction.Rotate) && _model != null)
        {
            Vector3 rotation = new Vector3(-deltaY, -deltaX, 0)
               * MouseRotateSpeed;
            _model.GetComponent<ModelBehaviour>().RotateAboutCenter(rotation);
        }

        if (mouseActions.HasFlag(MouseAction.Pan))
        {
            Vector3 pan = new Vector3(-deltaX, deltaY, 0)
                * MousePanSpeed;
            Camera.transform.Translate(pan, Space.Self);
        }

        if (mouseActions.HasFlag(MouseAction.Zoom))
        {
            Vector3 zoom = new Vector3(0, 0, deltaZ)
                * MouseZoomSpeed;
            Camera.transform.Translate(zoom, Space.Self);
        }
    }
    
    void OnGUI()
    {
        Gui.OnGUI();
        HandleUnusedMouseEvents();
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
        if (_progressTracker.IsNewImportStep())
            Gui.Log.Add(message);
        else
            Gui.Log[Gui.Log.Count - 1] = message;
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
        Gui.ResetSpin();

        if (_model != null)
            Destroy(_model);

        _model = model;
        _model.AddComponent<ModelBehaviour>();

        _importJob = null;
    }

    /// <summary>
    /// Invoked when an exception is thrown during model import.
    /// </summary>
    public void OnImportException(Exception e)
    {
        Gui.FooterMessage = string.Format(
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
    }

}
