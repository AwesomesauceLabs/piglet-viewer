using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityGLTF;

/// <summary>
/// Draws the UI elements (e.g. progress messages, checkboxes)
/// on top of the model viewer window.
/// </summary>
public class ViewerGUI
{
    /// <summary>
    /// Speed to auto-spin model left-to-right.
    /// </summary>
    public float SpinX;
    
    /// <summary>
    /// Speed to auto-spin model down-to-up.
    /// </summary>
    public float SpinY;

    public string FooterMessage;
    
    /// <summary>
    /// The list of progress messages generated for the
    /// current glTF import.
    /// </summary>
    public List<string> Log;

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
        FooterMessage = null;
    }

    public void Reset()
    {
        ResetSpin();
        ResetLog();
        ResetFooterMessage();
    }

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
        _styles.SliderLabel.fontSize = 18;
        
        _styles.FooterText = new GUIStyle(GUI.skin.label);
        _styles.FooterText.alignment = TextAnchor.MiddleCenter;
        _styles.FooterText.fontStyle = FontStyle.Italic;
        _styles.FooterText.fontSize = 24;
    }

    /// <summary>
    /// Draws GUI elements (e.g. progress messages, checkboxes)
    /// onto the model viewer window.
    /// </summary>
    public void OnGUI()
    {
        InitStyles();
        
        // set font color to black
        GUI.contentColor = Color.black;

        float padding = 25;

        GUILayout.BeginArea(new Rect(
            padding, padding,
            Screen.width - 2 * padding,
            Screen.height - 2 * padding));
        
            // progress log messages
            foreach (var line in Log)
               GUILayout.Label(line, _styles.Text);
        
        GUILayout.EndArea();

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

        GUILayout.EndArea();
        
    }
}
