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
    private ViewerGUI _gui;

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
        //
        // Note: This flag has no effect on Android,
        // iOS, or WebGL, so there is no harm in always
        // setting it to true.
        
        Application.runInBackground = true;
        
        ResetImportState();
    }
    
#if UNITY_ANDROID && !UNITY_EDITOR

    /// <summary>
    /// Get the URI that was used to launch or resume PigletViewer (if any).
    /// This is usually the result of opening a .gltf/.glb in an
    /// Android file browser.
    /// </summary>
    /// <returns></returns>
    string GetAndroidIntentUri()
    {
        AndroidJavaClass player
            = new AndroidJavaClass("com.unity3d.player.UnityPlayer"); 
        AndroidJavaObject currentActivity
            = player.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject intent
            = currentActivity.Call<AndroidJavaObject>("getIntent");
                 
        return intent.Call<string> ("getDataString");
    }
    
    /// <summary>
    /// Unity callback that is invoked when the application starts.
    /// </summary>
    void Start()
    {
        string uri = GetAndroidIntentUri();
 
        if (string.IsNullOrEmpty(uri))
            uri = Path.Combine(Application.streamingAssetsPath, "piglet-1.0.0.glb");
        
        StartImport(uri);
    }

    /// <summary>
    /// Unity callback that is invoked when the application gains
    /// or loses focus.
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        // if Unity Player is losing focus, rather than gaining focus
        if (!hasFocus)
           return;

        string uri = GetAndroidIntentUri();
        
        // if Unity Player is regaining focus without a new model URI
        // to load (e.g. user selected Piglet Viewer in Android app
        // switcher)
        if (string.IsNullOrEmpty(uri))
            return;
        
        StartImport(uri);
    }

#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

    UnityDragAndDropHook _dragAndDropHook;

    void Start()
    {
        _dragAndDropHook = new UnityDragAndDropHook();
        _dragAndDropHook.InstallHook();
        _dragAndDropHook.OnDroppedFiles += OnDropFiles;

        _gui.FooterMessage = "drag .gltf/.glb file onto window to view";
        
        ParseCommandLineArgs();
    }

    /// <summary>
    /// Parse command line arguments and start initial model
    /// import (if any).
    /// </summary>
    private void ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        
        bool profile = false;
        bool quitAfterLoad = false;
        long delayLoadMilliseconds = 0;
        
        // default model to load at startup, unless
        // --load or --no-load is used

        ImportTask importTask = GLTFRuntimeImporter
            .GetImportTask(Path.Combine(
                Application.streamingAssetsPath, "piglet-1.0.0.glb"),
                OnImportProgress);

        for (int i = 0; i < args.Length; ++i)
        {
            if (args[i] == "--delay-load")
            {
                // Delay initial model import at startup.
                // I added this option so that I could prevent
                // Unity player loading/initialization from affecting
                // my profiling results. 
                delayLoadMilliseconds = Int64.Parse(args[i + 1]);
            }
            else if (args[i] == "--load")
            {
                // Specify a model to load at startup,
                // in place of the default Piglet model.
                string uri = args[i + 1];
                importTask = GLTFRuntimeImporter
                    .GetImportTask(uri, OnImportProgress);
            }
            else if (args[i] == "--no-load")
            {
                // Don't load a model at startup.
                importTask = null;
            }
            else if (args[i] == "--profile")
            {
                // Record and log profiling results while
                // importing the initial model. This option times
                // IEnumerator.MoveNext() calls and identifies
                // which import subtasks cause the longest
                // interruptions the main Unity thread.
                profile = true;
            }
            else if (args[i] == "--quit-after-load")
            {
                // Exit the viewer immediately after loading
                // the initial model. This option is usually
                // used in conjunction with --profile to
                // perform automated profiling from the command
                // line.
                quitAfterLoad = true;
            }
        }

        if (importTask == null)
            return;

        if (delayLoadMilliseconds > 0)
            importTask.PushTask(SleepUtil.SleepEnum(
                delayLoadMilliseconds));
            
        if (profile)
            importTask.OnCompleted += _ => importTask.LogProfilingData();

        importTask.OnCompleted += OnImportCompleted;

        if (quitAfterLoad)
            importTask.OnCompleted += _ => Application.Quit(0);
        
        importTask.OnException += OnImportException;
        importTask.RethrowExceptionAfterCallbacks = false;
        
        StartImport(importTask);
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
        
        ImportTask importJob = GLTFRuntimeImporter
            .GetImportTask(uri, OnImportProgress);

        importJob.OnCompleted += OnImportCompleted;
        importJob.OnException += OnImportException;
        importJob.RethrowExceptionAfterCallbacks = false;
        
        StartImport(importJob);
    }

    void StartImport(ImportTask importTask)
    {
        ResetImportState();
        _progressTracker.StartImport();
        _importJob = importTask;
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

            _gui.SpinX = 0;
            _gui.SpinY = 0;
        }

        if (mouseActions.HasFlag(MouseAction.Rotate))
        {
            Vector3 rotation = new Vector3(-deltaY, -deltaX, 0)
               * MouseRotateSpeed;
            RotateAboutCenter(_model, rotation);
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
        
#if UNITY_ANDROID
        // On Android, the "Back" button is mapped to the
        // Escape key. See:
        // https://answers.unity.com/questions/25535/android-back-button-event.html
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AndroidJavaClass player
                = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity
                = player.GetStatic<AndroidJavaObject>("currentActivity");

            activity.Call<bool>("moveTaskToBack", true);
        }
#endif
        
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
