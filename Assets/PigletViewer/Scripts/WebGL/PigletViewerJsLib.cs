#if UNITY_WEBGL
using System;
using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Javascript methods that are callable from C#.
///
/// The definitions for these functions are in PigletViewer.jslib.
/// </summary>
public static class PigletViewerJsLib
{
    /// <summary>
    /// Performs one-time Javascript initialization
    /// (e.g. setting up event handlers).
    /// </summary>
    [DllImport("__Internal")]
    public static extern void Init();

    /// <summary>
    /// Get the size of a file in bytes.
    /// </summary>
    /// <param name="filename">
    /// The basename for the file that was last selected
    /// by the user.
    /// </param>
    /// <returns></returns>
    [DllImport("__Internal")]
    public static extern int GetFileSize(string filename);

    /// <summary>
    /// Get the contents of a file as byte array.
    /// </summary>
    /// <param name="filename">
    /// The basename for the file that was last selected
    /// by the user.
    /// </param>
    /// <returns>
    /// A native pointer to a byte array containing the
    /// contents of the file.
    /// </returns>
    [DllImport("__Internal")]
    public static extern IntPtr GetFileData(string filename);

    /// <summary>
    /// Free the contents of a file from Javascript memory.
    /// </summary>
    /// <param name="filename">
    /// The basename for the file that was last selected
    /// by the user.
    /// </param>
    [DllImport("__Internal")]
    public static extern void FreeFileData(string filename);

    /// <summary>
    /// Append a line to the Import Log, located in the left panel
    /// of the web page.
    /// </summary>
    [DllImport("__Internal")]
    public static extern void AppendLogLine(string line);

    /// <summary>
    /// Replace the last line of the Import Log, located in the
    /// left panel of the web page.
    /// </summary>
    [DllImport("__Internal")]
    public static extern void UpdateTailLogLine(string line);

    /// <summary>
    /// Return a localhost URL through which the given data
    /// (byte[] array) can be read.  This method is a wrapper
    /// around the Javascript method `URL.createObjectURL`.
    /// </summary>
    [DllImport("__Internal")]
    public static extern string CreateObjectUrl(byte[] data, int size);

    /// <summary>
    /// Load a texture from in-memory PNG/JPG data using
    /// browser-side scripting (i.e. Javascript/WebGL).  Using
    /// the browser to load a texture is preferable to standard
    /// Unity texture-loading methods because it is faster
    /// and can run in parallel with the main Unity thread.
    /// Once the texture has been loaded by the browser, the
    /// Javascript calls back into the Unity C# code
    /// using the provided `textureId`, so that a
    /// Texture2D object can be created that links to the native
    /// WebGL texture (see Texture2D.CreateExternalTexture).
    /// </summary>
    /// <param name="data">raw PNG/JPG image data</param>
    /// <param name="size">the size of the data array, in bytes</param>
    /// <param name="textureId">
    /// Uniquely identifies the Unity Texture2D object that corresponds
    /// the native WebGL texture.
    /// </param>
    [DllImport("__Internal")]
    public static extern void LoadTexture(byte[] data, int size, int textureId);
}
#endif
