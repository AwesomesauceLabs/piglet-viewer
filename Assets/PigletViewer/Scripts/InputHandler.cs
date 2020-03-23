using System;
using UnityEngine;

/// <summary>
/// Maps mouse input and touchscreen input to corresponding actions in the
/// viewer (e.g. rotate model).
/// </summary>
public class InputHandler : Singleton<InputHandler>
{
    /// <summary>
    /// Describes the state of touchscreen input (e.g. Android)
    /// for a single frame.
    /// </summary>
    private struct TouchState
    {
        /// <summary>
        /// The number fingers in contact with the touch screen.
        /// </summary>
        public int TouchCount;
        
        /// <summary>
        /// The distance between the two fingers touching
        /// the screen, or null if there aren't two fingers
        /// touching the screen.
        /// </summary>
        public float? PinchDist;
        
        /// <summary>
        /// The midpoint between the two fingers touching
        /// the screen, or null if there aren't two fingers
        /// touching the screen.
        /// </summary>
        public Vector2? PinchMidpoint;
    }

    /// <summary>
    /// The touch screen input state from the previous frame.
    /// </summary>
    private TouchState _prevTouchState;

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
    /// Unity callback that is invoked before the first frame update.
    /// </summary>
    protected void Start()
    {
        _prevTouchState = new TouchState();
    }

    /// <summary>
    /// Handle touch screen input (Android and WebGL).
    ///
    /// Note: This code uses the `Touch` class
    /// from Unity's old input system ("Input Manager").
    /// At the time of coding, I was not aware that there
    /// was a newer Unity input system ("Input System",
    /// introduced in Unity 2019.1), which provides a new touch
    /// input API via `InputSystem.EnhancedTouch.Touch`.
    /// See https://forum.unity.com/threads/inputsystem-enhancedtouch-touch-and-unity-ads.779351/
    /// The code below works fine and is likely to
    /// be supported by Unity for a long time. But if I
    /// ever need to make major changes, I should consider
    /// using the new input system.
    /// </summary>
    protected void ProcessTouchInput()
    {
        // if user is currently interacting with an IMGUI control (e.g. a slider)
        if (GUIUtility.hotControl != 0)
            return;

        // if touch screen input is not supported on the current platform
        if (!Input.touchSupported)
            return;
        
        MouseAction mouseActions = MouseAction.None;
        bool mouseDown = false;
        float deltaX = 0f;
        float deltaY = 0f;
        float deltaZ = 0f;

        // if number of fingers has changed
        if (Input.touchCount != _prevTouchState.TouchCount)
        {
            _prevTouchState.PinchDist = null;
            _prevTouchState.PinchMidpoint = null;
            
            // perform mouse click actions when finger(s) first touch screen
            if (_prevTouchState.TouchCount == 0)
                mouseDown = true;
        }
        else if (Input.touchCount == _prevTouchState.TouchCount)
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

                if (_prevTouchState.PinchDist.HasValue)
                {
                    mouseActions |= MouseAction.Zoom;
                    
                    float pinchDelta = pinchDist - _prevTouchState.PinchDist.Value;
                    deltaZ = pinchDelta * 0.03f;
                }

                _prevTouchState.PinchDist = pinchDist;
                
                // two-finger drag -> pan
                
                Vector2 pinchMidpoint = (touch0.position + touch1.position) / 2.0f;
                if (_prevTouchState.PinchMidpoint.HasValue)
                {
                    mouseActions |= MouseAction.Pan;

                    Vector2 deltaMidpoint
                        = pinchMidpoint - _prevTouchState.PinchMidpoint.Value;
                    
                    deltaX = deltaMidpoint.x * 0.3f;
                    deltaY = -deltaMidpoint.y * 0.3f;
                }

                _prevTouchState.PinchMidpoint = pinchMidpoint;
            }
        }

        // stop auto-spin ("Spin X" / "Spin Y")
        // whenever the user clicks on the
        // model/background.
        if (mouseDown)
            ViewerGUI.Instance.ResetSpin();

        if (mouseActions.HasFlag(MouseAction.Rotate))
            GameManager.Instance.RotateModel(new Vector3(-deltaY, -deltaX, 0));

        if (mouseActions.HasFlag(MouseAction.Pan))
            GameManager.Instance.PanCamera(new Vector3(-deltaX, deltaY, 0));

        if (mouseActions.HasFlag(MouseAction.Zoom))
            GameManager.Instance.ZoomCamera(deltaZ);

        _prevTouchState.TouchCount = Input.touchCount;
    }

    /// <summary>
    /// Handle any mouse events that are not consumed by IMGUI controls
    /// (e.g. checkboxes, sliders). This method is used to implement
    /// the conventional mouse behaviour for rotating the model, panning the
    /// camera, and zooming the camera.
    /// </summary>
    protected void ProcessMouseInput()
    {
        // if any GUI element (e.g. checkbox, slider) currently
        // has focus

        if (GUIUtility.hotControl != 0)
            return;

        // Note: The test for Input.touchCount != 0 ensures
        // that the code is only run in response to input from a
        // *real* mouse, rather than mouse events simulated from
        // a touch screen. It would be better/cleaner to set 
        // Input.simulateMouseWithTouches to false to achieve this
        // separation, but I found that the setting has no effect
        // (in Unity 2018.3). Moreover, there is a bug where
        // Input.simulateMouseWithTouches is ignored under WebGL:
        // https://forum.unity.com/threads/input-simulatemousewithtouches-is-ignored-in-webgl.388157/

        if (Input.touchCount != 0)
            return;
        
        Event @event = Event.current;

        MouseAction mouseActions = MouseAction.None;
        bool mouseDown = false;
        float deltaX = 0f;
        float deltaY = 0f;
        float deltaZ = 0f;

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
                deltaX = @event.delta.x;
                deltaY = @event.delta.y;
                break;
            case EventType.ScrollWheel:
                mouseActions = MouseAction.Zoom;
                // note: Unity passes in mouse scroll wheel
                // change via deltaY
                deltaZ = -@event.delta.y;
                break;
        }
        
        // stop auto-spin ("Spin X" / "Spin Y")
        // whenever the user clicks on the
        // model/background.
        if (mouseDown)
            ViewerGUI.Instance.ResetSpin();

        if (mouseActions.HasFlag(MouseAction.Rotate))
            GameManager.Instance.RotateModel(new Vector3(-deltaY, -deltaX, 0));

        if (mouseActions.HasFlag(MouseAction.Pan))
            GameManager.Instance.PanCamera(new Vector3(-deltaX, deltaY, 0));

        if (mouseActions.HasFlag(MouseAction.Zoom))
            GameManager.Instance.ZoomCamera(deltaZ);
    }

    /// <summary>
    /// Unity callback that is invoked for all IMGUI events (e.g. Repaint, MouseDown).
    /// </summary>
    public void OnGUI()
    {
        ProcessMouseInput();
    }

    /// <summary>
    /// Unity callback that is invoked once per frame.
    /// </summary>
    public void Update()
    {
        ProcessTouchInput();
    }
}
