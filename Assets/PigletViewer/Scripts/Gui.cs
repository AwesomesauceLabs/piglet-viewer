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
    public class Gui : Singleton<Gui>
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
        }

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

            Reset();
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
        /// Reset all elements of the GUI to their default states.
        /// </summary>
        public void Reset()
        {
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

            Texture2D roundedRectWhite = Resources.Load<Texture2D>("RoundedRectWhite");
            Texture2D roundedRectLightGray = Resources.Load<Texture2D>("RoundedRectLightGray");
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
        }

        /// <summary>
        /// Render the GUI for the viewer (e.g. progress log,
        /// "Spin X"/"Spin Y" sliders).
        /// </summary>
        public void DrawGUI()
        {
            // set font color to black
            GUI.contentColor = Color.black;

            // In the WebGL build, the import log is shown
            // in the left panel as part of the main web
            // page.
            //
            // On Windows, the import log is drawn on top
            // of the window, in the upper left hand corner.

#if !UNITY_WEBGL || UNITY_EDITOR
            float padding = 25;

            GUILayout.BeginArea(new Rect(
                padding, padding,
                Screen.width - 2 * padding,
                Screen.height - 2 * padding));

            // progress log messages
            foreach (var line in _progressLog)
                GUILayout.Label(line, _styles.Text);

            GUILayout.EndArea();
#endif

            // footer message and "Spin X" / "Spin Y" sliders

            float footerAreaHeight = 100;

            float labelWidth = 100;
            float sliderWidth = 100;
            float sliderAreaWidth = 2 * labelWidth + 2 * sliderWidth;

            Rect footerAreaRect = new Rect(
                0, Screen.height - footerAreaHeight,
                Screen.width, footerAreaHeight);

            GUILayout.BeginArea(footerAreaRect);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(FooterMessage, _styles.FooterText);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // We don't show the "Spin X" / "Spin Y" sliders
            // on Android because they are tiny and difficult to
            // interact with.

            if (Application.platform != RuntimePlatform.Android)
            {
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

                GUILayout.FlexibleSpace();
            }

            GUILayout.EndArea();

            DialogBoxOnGui();
        }

        /// <summary>
        /// Drag a dialog box, using IMGUI functions, which is automatically
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