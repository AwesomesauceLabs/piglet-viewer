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
    
    public ViewerGUI()
    {
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
    
    /// <summary>
    /// Draws GUI elements (e.g. progress messages, checkboxes)
    /// onto the model viewer window.
    /// </summary>
    public void OnGUI()
    {
        // set font color to black
        GUI.contentColor = Color.black;

        var labelStyle = GUI.skin.GetStyle("Label");
        labelStyle.alignment = TextAnchor.MiddleLeft;
        labelStyle.fontStyle = FontStyle.Normal;
        labelStyle.fontSize = 18;

        float padding = 20;
        
        GUILayout.BeginArea(new Rect(
            padding, padding,
            Screen.width - 2 * padding,
            Screen.height - 2 * padding));

        float rowHeight = 20;
        float labelWidth = 100;
        float sliderWidth = 100;
        
        GUILayout.BeginVertical(GUILayout.Height(rowHeight));
            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Spin X", labelStyle,
                        GUILayout.Width(labelWidth));
                    GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    SpinX = GUILayout.HorizontalSlider(
                        SpinX, 0, 1, GUILayout.Width(sliderWidth));
                    GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        GUILayout.EndVertical();
        
        GUILayout.BeginVertical(GUILayout.Height(rowHeight));
            GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Spin Y", labelStyle,
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
        GUILayout.EndVertical();
        
        GUILayout.Space(30);
        
        // progress log messages
        
        foreach (var line in Log)
            GUILayout.Label(line, labelStyle);
        
        GUILayout.EndArea();
        
        // status message along the bottom of the window
        
        var footerStyle = GUI.skin.GetStyle("Label");
        footerStyle.alignment = TextAnchor.MiddleCenter;
        footerStyle.fontStyle = FontStyle.Italic;
        footerStyle.fontSize = 22;

        GUI.Label(new Rect(0, Screen.height - 100, Screen.width, 100),
            FooterMessage, footerStyle);
    }
}
