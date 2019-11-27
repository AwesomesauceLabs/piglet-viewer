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
    /// Data used to generated import progress message for
    /// a particular type of glTF entity (e.g. meshes).
    /// </summary>
    struct ProgressLine
    {
        /// <summary>
        /// The type of glTF entity (e.g. textures, meshes).
        /// </summary>
        public GLTFImporter.Type Type;
        /// <summary>
        /// Number of glTF entities that have been imported
        /// so far of this type.
        /// </summary>
        public int Current;
        /// <summary>
        /// The total number of glTF entities that will be imported
        /// for this type.
        /// </summary>
        public int Total;
        /// <summary>
        /// The total milliseconds that have been spent
        /// importing glTF entities of this type.
        /// </summary>
        public float Milliseconds;
    }

    /// <summary>
    /// The list of progress messages generated for the
    /// current glTF import.
    /// </summary>
    private List<ProgressLine> _progressLines;

    public ViewerGUI()
    {
        _progressLines = new List<ProgressLine>();
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
        labelStyle.fontSize = 24;

        foreach (var line in _progressLines)
        {
            string label = string.Format(
                "Loading {0}s... [{1}/{2}] ({3} ms)",
                line.Type.ToString().ToLower(),
                line.Current, line.Total, line.Milliseconds);
            
            GUILayout.Label(label, labelStyle);
        }
    }

    /// <summary>
    /// Callback that is invoked each time a new glTF entity
    /// (e.g. a texture, a mesh) is successfully imported.
    /// </summary>
    /// <param name="type">type of glTF entity that was imported</param>
    /// <param name="current">number of entities of this type that have been imported so far</param>
    /// <param name="total">total number of entities of this type to be imported</param>
    /// <param name="milliseconds">time in milliseconds used to import this entity</param>
    /// <returns></returns>
    public bool OnProgress(GLTFImporter.Type type, int current,
        int total, float milliseconds)
    {
        ProgressLine line = new ProgressLine
        {
            Type = type,
            Current = current,
            Total = total,
            Milliseconds = milliseconds
        };
        
        var tailIndex = _progressLines.Count - 1;
        if (tailIndex < 0 || _progressLines[tailIndex].Type != type)
        {
            _progressLines.Add(line);
        }
        else
        {
            line.Milliseconds += _progressLines[tailIndex].Milliseconds;
            _progressLines[tailIndex] = line;
        }
        
        return true;
    }
}
