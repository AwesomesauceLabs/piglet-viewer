﻿using System;
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
            public GUIStyle DropDownListBackground;
            public GUIStyle DropDownListForeground;
            public GUIStyle DropDownListItem;
            public GUIStyle PlayButton;
        }

        /// <summary>
        /// Index of special "Static Pose" item
        /// in animation drop-down menu.
        /// </summary>
        private const int STATIC_POSE_INDEX = 0;

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
        /// Play icon for play/pause button.
        /// </summary>
        private Texture2D _playIcon;

        /// <summary>
        /// Pause icon for play/pause button.
        /// </summary>
        private Texture2D _pauseIcon;

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
            _dropDownState = new GuiEx.DropDownState
            {
                selectedIndex = STATIC_POSE_INDEX + 1,
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
            _playIcon = Resources.Load<Texture2D>("PlayIcon");
            _pauseIcon = Resources.Load<Texture2D>("PauseIcon");

            Texture2D roundedRectTransparent = Resources.Load<Texture2D>("RoundedRectTransparent");
            Texture2D roundedRectWhite = Resources.Load<Texture2D>("RoundedRectWhite");
            Texture2D roundedRectLightGray = Resources.Load<Texture2D>("RoundedRectLightGray");
            Texture2D roundedRectLightGrayNoBorder = Resources.Load<Texture2D>("RoundedRectLightGrayNoBorder");
            Texture2D roundedRectDarkGray = Resources.Load<Texture2D>("RoundedRectDarkGray");

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
            _styles.DropDownButton.clipping = TextClipping.Clip;
            _styles.DropDownButton.wordWrap = false;

            _styles.DropDownListBackground = new GUIStyle(GUI.skin.label);
            _styles.DropDownListBackground.normal.background = roundedRectDarkGray;
            _styles.DropDownListBackground.border = new RectOffset(10, 10, 10, 10);
            _styles.DropDownListBackground.alignment = TextAnchor.MiddleLeft;
            _styles.DropDownListBackground.margin = new RectOffset(15, 15, 15, 15);
            _styles.DropDownListBackground.padding = new RectOffset(15, 15, 11, 11);
            _styles.DropDownListBackground.fontSize = 20;

            _styles.DropDownListForeground = new GUIStyle(GUI.skin.label);
            _styles.DropDownListForeground.normal.background = roundedRectTransparent;
            _styles.DropDownListForeground.border = new RectOffset(10, 10, 10, 10);
            _styles.DropDownListForeground.alignment = TextAnchor.MiddleLeft;
            _styles.DropDownListForeground.margin = new RectOffset(15, 15, 15, 15);
            _styles.DropDownListForeground.padding = new RectOffset(15, 15, 11, 11);
            _styles.DropDownListForeground.fontSize = 20;

            _styles.DropDownListItem = new GUIStyle(GUI.skin.label);
            _styles.DropDownListItem.hover.background = roundedRectLightGrayNoBorder;
            _styles.DropDownListItem.border = new RectOffset(10, 10, 10, 10);
            _styles.DropDownListItem.alignment = TextAnchor.MiddleLeft;
            _styles.DropDownListItem.margin = new RectOffset(15, 15, 15, 15);
            _styles.DropDownListItem.padding = new RectOffset(15, 15, 11, 11);
            _styles.DropDownListItem.fontSize = 20;
            _styles.DropDownListItem.clipping = TextClipping.Clip;
            _styles.DropDownListItem.wordWrap = false;

            _styles.PlayButton = new GUIStyle(GUI.skin.label);
            _styles.PlayButton.normal.background = roundedRectLightGray;
            _styles.PlayButton.border = new RectOffset(10, 10, 10, 10);
            _styles.PlayButton.alignment = TextAnchor.MiddleLeft;
            _styles.PlayButton.margin = new RectOffset(15, 15, 15, 15);
            _styles.PlayButton.padding = new RectOffset(15, 15, 11, 11);
            _styles.PlayButton.fontSize = 20;

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

            var clipKeys = ModelManager.Instance.AnimationClipKeys;
            var clipNames = ModelManager.Instance.AnimationClipNames;

            const float animationControlsAreaHeight = 75;

            // Get a reference to the currently selected animation clip.
            // We use a value of null to indicate that the special
            // "Static Pose" clip is currently selected.

            AnimationState selectedClip = null;
            if (_dropDownState.selectedIndex != STATIC_POSE_INDEX)
                selectedClip = anim[clipKeys[_dropDownState.selectedIndex]];

            // play/pause button

            const float playButtonSize = 50;

            var playButtonRect = new Rect(
                _screenEdgePadding,
                Screen.height - animationControlsAreaHeight
                    + (animationControlsAreaHeight - playButtonSize) / 2,
                playButtonSize,
                playButtonSize);

            if (GUI.Button(playButtonRect, "", _styles.PlayButton) && selectedClip != null)
                selectedClip.speed = selectedClip.speed == 0f ? 1f : 0f;

            var origColor = GUI.color;
            GUI.color = selectedClip != null ? Color.black : Color.gray;

            const float playIconMargin = 10;
            var playIconRect = new Rect(
                playButtonRect.x + playIconMargin,
                playButtonRect.y + playIconMargin,
                playButtonRect.width - 2 * playIconMargin,
                playButtonRect.width - 2 * playIconMargin);

            GUI.DrawTexture(playIconRect,
                selectedClip != null && selectedClip.speed > 0f ? _pauseIcon : _playIcon,
                ScaleMode.ScaleToFit);

            GUI.color = origColor;

            // drop-down menu for selecting animation clip

            const float buttonWidth = 300;
            var buttonHeight = _styles.DropDownButton.CalcSize(
                new GUIContent("Dummy Text")).y;

            var buttonRect = new Rect(
                Screen.width - _screenEdgePadding - buttonWidth,
                Screen.height - animationControlsAreaHeight
                    + (animationControlsAreaHeight - buttonHeight) / 2,
                buttonWidth, buttonHeight);

            var prevSelectedIndex = _dropDownState.selectedIndex;

            _dropDownState = GuiEx.DropDownMenu(
                buttonRect,
                clipNames,
                _dropDownState,
                _dropDownIcon,
                _styles.DropDownButton,
                _styles.DropDownListBackground,
                _styles.DropDownListForeground,
                _styles.DropDownListItem);

            // if a new animation clip has been selected, stop the
            // current clip and start playing the new one

            if (_dropDownState.selectedIndex != prevSelectedIndex)
            {
                anim.Stop();

                if (selectedClip != null)
                {
                    selectedClip.speed = 1f;
                    selectedClip.time = 0f;
                }

                // Restore the static pose before we start playing the
                // new animation clip. This is necessary because
                // an animation clip will not necessarily reset
                // all position/rotation/scale values for
                // all game objects in the model. Any unspecified
                // transform values are assumed to come from the
                // default static pose.

                anim.Play(clipKeys[STATIC_POSE_INDEX]);
                anim.Sample();

                // start playing the new clip

                var newClipKey = clipKeys[_dropDownState.selectedIndex];
                var newClip = anim[newClipKey];

                newClip.time = 0f;
                newClip.speed = 1f;

                anim.Play(newClipKey);
            }

            // timeline slider

            const float sliderMargin = 20;
            const float sliderHeight = 10;

            var sliderX1 = playButtonRect.x + playButtonRect.width + sliderMargin;
            var sliderX2 = buttonRect.x - sliderMargin;

            var sliderRect = new Rect(
                sliderX1,
                Screen.height - animationControlsAreaHeight
                    + (animationControlsAreaHeight - sliderHeight) / 2,
                sliderX2 - sliderX1,
                sliderHeight);

            var time = 0f;
            var length = 0f;
            if (selectedClip != null)
            {
                length = selectedClip.length;
                time = selectedClip.time % length;
            }

            var prevTime = time;
            time = GUI.HorizontalSlider(sliderRect, time, 0f, length);

            // if the user clicked on the slider, set the time to
            // the clicked location and pause playback
            if (selectedClip != null && time != prevTime)
            {
                selectedClip.time = time;
                selectedClip.speed = 0f;
            }
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