using System;
using System.Collections.Generic;
using Piglet;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityGLTF;

namespace PigletViewer
{
    /// <summary>
    /// Draws the UI elements (e.g. progress messages, checkboxes)
    /// on top of the model viewer window.
    /// </summary>
    public class Gui : SingletonBehaviour<Gui>
    {
        /// <summary>
        /// Speed to auto-spin model left-to-right.
        /// </summary>
        public float SpinX;

        /// <summary>
        /// Speed to auto-spin model down-to-up.
        /// </summary>
        public float SpinY;

        /// <summary>
        /// Controls speed of model rotation relative to SpinX/SpinY slider values.
        /// </summary>
        public float SpinSpeed;

        /// <summary>
        /// Message shown along the bottom of the view area.
        /// Typically used for instructions or for reporting
        /// errors.
        /// </summary>
        public string FooterMessage;

        /// <summary>
        /// The default footer message to use when
        /// the footer message is reset.  The footer
        /// message is typically reset after a new
        /// glTF model has been loaded.
        /// </summary>
        public string DefaultFooterMessage;

        /// <summary>
        /// "Piglet" in fancy writing.
        /// </summary>
        private Texture2D _titleImage;

        /// <summary>
        /// Enscapsulates GUIStyles that control GUI rendering (e.g.
        /// font sizes, text alignment).
        /// </summary>
        private class Styles
        {
            public GUIStyle Title;
            public GUIStyle Text;
            public GUIStyle SliderLabel;
            public GUIStyle FooterText;
            public GUIStyle DialogBox;
            public GUIStyle DialogHeading;
            public GUIStyle DialogText;
            public GUIStyle DialogButton;
            public GUIStyle DropDownButton;
            public GUIStyle DropDownList;
            public GUIStyle DropDownListItem;
        }

        /// <summary>
        /// The strings that appear in the drop-down
        /// menu for selecting the active animation clip.
        /// </summary>
        private List<string> _animationClipNames;

        /// <summary>
        /// Holds state information about the
        /// drop-down menu for animation clips
        /// (e.g. the index of the currently
        /// selected clip).
        /// </summary>
        private GuiEx.DropDownState _dropDownState;

        /// <summary>
        /// Expander icon (arrow) shown on right
        /// side of drop-down button.
        /// </summary>
        private Texture2D _dropDownIcon;

        /// <summary>
        /// Width of padding between screen edges and
        /// UI text/controls.
        /// </summary>
        private float _screenEdgePadding;

        /// <summary>
        /// The list of progress messages generated for the
        /// current glTF import.
        /// </summary>
        private List<string> _progressLog;

        /// <summary>
        /// Encapsulates style variables (e.g. font size)
        /// for various parts of the UI.
        /// </summary>
        private Styles _styles;

        /// <summary>
        /// Specifies the title and body text for a dialog box.
        /// This is used to show error messages to the
        /// user.
        /// </summary>
        private class DialogBoxContent
        {
            public string Title;
            public string Message;
        }

        /// <summary>
        /// The title and body text for the currently displayed
        /// dialog box. This variable is null if no box is being
        /// displayed.
        /// </summary>
        private DialogBoxContent _dialogBoxContent;

        public Gui()
        {
            _styles = null;
            _screenEdgePadding = 25;

            Reset();
        }

        public void Start()
        {
            _titleImage = Resources.Load<Texture2D>("PigletTitle");
        }

        /// <summary>
        /// Append a new line to the progress log.
        /// </summary>
        public void AddProgressLogLine(string message)
        {
            _progressLog.Add(message);
        }

        /// <summary>
        /// Replace the most recent line of the progress log.
        /// </summary>
        public void UpdateProgressLogLine(string message)
        {
            if (_progressLog.Count == 0)
            {
                AddProgressLogLine(message);
                return;
            }

            _progressLog[_progressLog.Count - 1] = message;
        }

        /// <summary>
        /// Clear the progress log.
        /// </summary>
        public void ResetProgressLog()
        {
            _progressLog = new List<string>();
        }

        /// <summary>
        /// Reset the "Spin X" / "Spin Y" sliders to zero, so
        /// that the model does not spin automatically (like a
        /// record turntable).
        /// </summary>
        public void ResetSpin()
        {
            SpinX = 0;
            SpinY = 0;
        }

        /// <summary>
        /// Reset the text message shown along the bottom of the
        /// window to the default message.  (This message may
        /// be used to show status, give the user hints/instructions,
        /// or to display error messages.)
        /// </summary>
        public void ResetFooterMessage()
        {
            FooterMessage = DefaultFooterMessage;
        }

        /// <summary>
        /// Reset UI controls for animation playback
        /// (e.g. drop-down menu for selecting animation
        /// clip).
        /// </summary>
        public void ResetAnimationControls()
        {
            _animationClipNames = null;
            _dropDownState = new GuiEx.DropDownState
            {
                selectedIndex = 0,
                expanded = false
            };
        }

        /// <summary>
        /// Reset all elements of the GUI to their default states.
        /// </summary>
        public void Reset()
        {
            ResetAnimationControls();
            ResetProgressLog();
            ResetSpin();
            ResetFooterMessage();
            CloseDialogBox();
        }

        /// <summary>
        /// Dismiss the currently displayed dialog box (if any).
        /// </summary>
        public void CloseDialogBox()
        {
            _dialogBoxContent = null;
        }

        /// <summary>
        /// Display a dialog box with the given title and body text.
        /// The dialog box will be dismissed when the user presses the
        /// "OK" button.
        /// </summary>
        public void ShowDialogBox(string title, string message)
        {
            _dialogBoxContent = new DialogBoxContent {Title = title, Message = message};
        }

        /// <summary>
        /// Initialize GUIStyles used to render the GUI
        /// (e.g. font sizes, text alignment).
        /// </summary>
        private void InitStyles()
        {
            if (_styles != null)
                return;

            _dropDownIcon = Resources.Load<Texture2D>("DropDownIcon");

            Texture2D roundedRectTransparent = Resources.Load<Texture2D>("RoundedRectTransparent");
            Texture2D roundedRectWhite = Resources.Load<Texture2D>("RoundedRectWhite");
            Texture2D roundedRectLightGray = Resources.Load<Texture2D>("RoundedRectLightGray");
            Texture2D roundedRectDarkGray = Resources.Load<Texture2D>("RoundedRectDarkGray");
            Texture2D roundedRectLightGrayNoBorder = Resources.Load<Texture2D>("RoundedRectLightGrayNoBorder");

            _styles = new Styles();

            _styles.Title = new GUIStyle(GUI.skin.label);
            _styles.Title.alignment = TextAnchor.MiddleLeft;
            _styles.Title.margin = new RectOffset(
                _styles.Title.margin.left, 0, 20, 20);
            _styles.Title.fontSize = 24;
            _styles.Title.fontStyle = FontStyle.Bold;

            _styles.Text = new GUIStyle(GUI.skin.label);
            _styles.Text.alignment = TextAnchor.MiddleLeft;
            _styles.Text.fontStyle = FontStyle.Normal;
            _styles.Text.padding = new RectOffset(0, 0, 0, 0);
            _styles.Text.fontSize = 18;

            _styles.SliderLabel = new GUIStyle(GUI.skin.label);
            _styles.SliderLabel.alignment = TextAnchor.MiddleCenter;
            _styles.SliderLabel.fontSize = 24;

            _styles.FooterText = new GUIStyle(GUI.skin.label);
            _styles.FooterText.alignment = TextAnchor.MiddleCenter;
            _styles.FooterText.fontStyle = FontStyle.Italic;
            _styles.FooterText.fontSize = 24;

            _styles.DialogBox = new GUIStyle(GUI.skin.window);
            _styles.DialogBox.normal.background = roundedRectLightGray;
            _styles.DialogBox.border = new RectOffset(20, 20, 20, 20);

            _styles.DialogHeading = new GUIStyle(GUI.skin.label);
            _styles.DialogHeading.normal.background = roundedRectDarkGray;
            _styles.DialogHeading.border = new RectOffset(10, 10, 10, 10);
            _styles.DialogHeading.alignment = TextAnchor.MiddleLeft;
            _styles.DialogHeading.margin = new RectOffset(15, 15, 15, 15);
            _styles.DialogHeading.padding = new RectOffset(15, 15, 11, 11);
            _styles.DialogHeading.fontSize = 24;

            _styles.DialogText = new GUIStyle(GUI.skin.label);
            _styles.DialogText.alignment = TextAnchor.MiddleLeft;
            _styles.DialogText.margin = new RectOffset(15, 15, 15, 15);
            _styles.DialogText.padding = new RectOffset(10, 10, 0, 0);
            _styles.DialogText.fontSize = 20;

            _styles.DialogButton = new GUIStyle(GUI.skin.button);
            _styles.DialogButton.normal.background = roundedRectDarkGray;
            _styles.DialogButton.hover.background = roundedRectWhite;
            _styles.DialogButton.active.background = roundedRectWhite;
            _styles.DialogButton.border = new RectOffset(10, 10, 10, 10);
            _styles.DialogButton.margin = new RectOffset(15, 15, 15, 15);
            _styles.DialogButton.padding = new RectOffset(30, 30, 11, 11);
            _styles.DialogButton.alignment = TextAnchor.MiddleCenter;
            _styles.DialogButton.fontSize = 18;

            _styles.DropDownButton = new GUIStyle(GUI.skin.label);
            _styles.DropDownButton.normal.background = roundedRectLightGray;
            _styles.DropDownButton.border = new RectOffset(10, 10, 10, 10);
            _styles.DropDownButton.alignment = TextAnchor.MiddleLeft;
            _styles.DropDownButton.margin = new RectOffset(15, 15, 15, 15);
            _styles.DropDownButton.padding = new RectOffset(15, 15, 11, 11);
            _styles.DropDownButton.fontSize = 20;

            _styles.DropDownList = new GUIStyle(GUI.skin.label);
            _styles.DropDownList.normal.background = roundedRectTransparent;
            _styles.DropDownList.border = new RectOffset(10, 10, 10, 10);
            _styles.DropDownList.alignment = TextAnchor.MiddleLeft;
            _styles.DropDownList.margin = new RectOffset(15, 15, 15, 15);
            _styles.DropDownList.padding = new RectOffset(15, 15, 11, 11);
            _styles.DropDownList.fontSize = 20;

            _styles.DropDownListItem = new GUIStyle(GUI.skin.label);
            _styles.DropDownListItem.hover.background = roundedRectLightGrayNoBorder;
            _styles.DropDownListItem.border = new RectOffset(10, 10, 10, 10);
            _styles.DropDownListItem.alignment = TextAnchor.MiddleLeft;
            _styles.DropDownListItem.margin = new RectOffset(15, 15, 15, 15);
            _styles.DropDownListItem.padding = new RectOffset(15, 15, 11, 11);
            _styles.DropDownListItem.fontSize = 20;
        }

        /// <summary>
        /// Render the GUI for the viewer (e.g. progress log,
        /// "Spin X"/"Spin Y" sliders).
        /// </summary>
        public void DrawGUI()
        {
            // set font color to black
            GUI.contentColor = Color.black;

            // Draw title image ("Piglet" in fancy writing).

            var titleRect = new Rect(
                (Screen.width - _titleImage.width) / 2.0f, 30,
                _titleImage.width, _titleImage.height);

            GUI.DrawTexture(titleRect, _titleImage);

            // In the WebGL build, the import log is shown
            // in the left panel as part of the main web
            // page.
            //
            // On Windows, the import log is drawn on top
            // of the window, in the upper left hand corner.

#if !UNITY_WEBGL || UNITY_EDITOR
            GUILayout.BeginArea(new Rect(
                _screenEdgePadding, _screenEdgePadding,
                Screen.width - 2 * _screenEdgePadding,
                Screen.height - 2 * _screenEdgePadding));

            // progress log messages
            foreach (var line in _progressLog)
                GUILayout.Label(line, _styles.Text);

            GUILayout.EndArea();
#endif

            // Display help message along bottom of window,
            // e.g. "drag .gltf/.glb/.zip onto window to view".

            float footerMessageOffset = 150;

            Rect footerMessageRect = new Rect(
                0, Screen.height - footerMessageOffset,
                Screen.width, footerMessageOffset);

            GUI.Label(footerMessageRect, FooterMessage, _styles.FooterText);

            if (ModelManager.Instance.Animation != null)
                AnimationControlsOnGui();
            else if (ModelManager.Instance.GetModel() != null)
                SpinControlsOnGui();

            DialogBoxOnGui();
        }

        /// <summary>
        /// Draws "Spin X"/"Spin Y" sliders that auto-spin
        /// the currently displayed model, as if it was on
        /// a record turntable.
        /// </summary>
        public void SpinControlsOnGui()
        {
            // We don't show the "Spin X" / "Spin Y" sliders
            // on Android because they are tiny and difficult to
            // interact with.
            if (Application.platform == RuntimePlatform.Android)
                return;

            const float labelWidth = 100;
            const float sliderWidth = 100;
            const float sliderAreaHeight = 50;

            GUILayout.BeginArea(new Rect(
                0,
                Screen.height - sliderAreaHeight,
                Screen.width,
                sliderAreaHeight));

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Spin X", _styles.SliderLabel,
                GUILayout.Width(labelWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            SpinX = GUILayout.HorizontalSlider(
                SpinX, 0, 1, GUILayout.Width(sliderWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Spin Y", _styles.SliderLabel,
                GUILayout.Width(labelWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            SpinY = GUILayout.HorizontalSlider(
                SpinY, 0, 1, GUILayout.Width(sliderWidth));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Draw the UI controls for animation playback, e.g.
        /// the drop-down menu for selecting the active
        /// animation clip. These UI controls are only shown
        /// when the currently glTF model has one or more
        /// animations.
        /// </summary>
        public void AnimationControlsOnGui()
        {
            Animation anim = ModelManager.Instance.Animation;

            const float animationControlsAreaHeight = 75;

            // drop-down menu for selecting animation clip

            if (_animationClipNames == null)
            {
                _dropDownState.selectedIndex = 0;
                _animationClipNames = new List<string> {"Static Pose"};
                int i = 1;
                foreach (AnimationState clip in anim)
                {
                    _animationClipNames.Add(clip.name);
                    if (clip.name == anim.clip.name)
                        _dropDownState.selectedIndex = i;
                    ++i;
                }
            }

            const float buttonWidth = 300;
            var buttonHeight = _styles.DropDownButton.CalcSize(
                new GUIContent("Dummy Text")).y;

            var buttonRect = new Rect(
                Screen.width - _screenEdgePadding - buttonWidth,
                Screen.height - animationControlsAreaHeight
                    + (animationControlsAreaHeight - buttonHeight) / 2,
                buttonWidth, buttonHeight);

            _dropDownState = GuiEx.DropDownMenu(
                buttonRect,
                _animationClipNames,
                _dropDownState,
                _dropDownIcon,
                _styles.DropDownButton,
                _styles.DropDownList,
                _styles.DropDownListItem);
        }

        /// <summary>
        /// Draw a dialog box, using IMGUI functions, which is automatically
        /// sized to its text content.
        /// </summary>
        public void DialogBoxOnGui()
        {
            if (_dialogBoxContent == null)
                return;

            GUIContent content;

            content = new GUIContent(_dialogBoxContent.Title);
            Vector2 headingSize = _styles.DialogHeading.CalcSize(content);

            content = new GUIContent(_dialogBoxContent.Message);
            Vector2 textSize = _styles.DialogText.CalcSize(content);

            content = new GUIContent("OK");
            Vector2 buttonSize = _styles.DialogButton.CalcSize(content);

            var headingMargin = _styles.DialogHeading.margin;
            var textMargin = _styles.DialogText.margin;
            var buttonMargin = _styles.DialogButton.margin;

            var dialogHeight =
                headingMargin.top
                + headingSize.y
                + Mathf.Max(headingMargin.bottom, textMargin.top)
                + textSize.y
                + Mathf.Max(textMargin.bottom, buttonMargin.top)
                + buttonSize.y
                + buttonMargin.bottom;

            var dialogWidth = Mathf.Max(
                headingMargin.left + headingSize.x + headingMargin.right,
                textMargin.left + textSize.x + textMargin.right,
                buttonMargin.left + buttonSize.x + buttonMargin.right);

            var rect = new Rect(
                (Screen.width - dialogWidth) / 2,
                (Screen.height - dialogHeight) / 2,
                dialogWidth, dialogHeight);

            GUI.Box(rect, "", _styles.DialogBox);

            GUILayout.BeginArea(rect);

            GUILayout.Space(headingMargin.top);

            GUILayout.BeginHorizontal();
            GUILayout.Space(headingMargin.left);
            GUILayout.Label(_dialogBoxContent.Title, _styles.DialogHeading);
            GUILayout.Space(headingMargin.right);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(textMargin.left);
            GUILayout.Label(_dialogBoxContent.Message, _styles.DialogText);
            GUILayout.Space(textMargin.right);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(buttonMargin.left);
            if (GUILayout.Button("OK", _styles.DialogButton,
                GUILayout.Width(buttonSize.x),
                GUILayout.Height(buttonSize.y)))
                CloseDialogBox();
            GUILayout.Space(buttonMargin.right);
            GUILayout.EndHorizontal();

            GUILayout.Space(_styles.DialogButton.margin.bottom);

            GUILayout.EndArea();
        }

        /// <summary>
        /// Draws GUI elements (e.g. progress messages, checkboxes)
        /// onto the model viewer window.
        /// </summary>
        public void OnGUI()
        {
            InitStyles();
            DrawGUI();
        }
    }
}