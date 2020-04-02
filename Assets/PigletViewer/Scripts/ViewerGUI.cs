using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityGLTF;

/// <summary>
/// Draws the UI elements (e.g. progress messages, checkboxes)
/// on top of the model viewer window.
/// </summary>
public class ViewerGUI : Singleton<ViewerGUI>
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
    /// The list of progress messages generated for the
    /// current glTF import.
    /// </summary>
    public List<string> Log;

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
    }

    private Styles _styles;
    
    public ViewerGUI()
    {
        _styles = null;
        
        Reset();
    }

    public void ResetSpin()
    {
        SpinX = 0;
        SpinY = 0;
    }

    public void ResetLog()
    {
        Log = new List<string>();
    }

    public void ResetFooterMessage()
    {
        FooterMessage = DefaultFooterMessage;
    }

    public void Reset()
    {
        ResetSpin();
        ResetLog();
        ResetFooterMessage();
    }

    /// <summary>
    /// Initialize GUIStyles used to render the GUI
    /// (e.g. font sizes, text alignment).
    /// </summary>
    private void InitStyles()
    {
        if (_styles != null)
            return;

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
            foreach (var line in Log)
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
