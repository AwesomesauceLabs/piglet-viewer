#if UNITY_WEBGL
using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace PigletViewer
{
    /// <summary>
    /// Javascript methods that are callable from C#.
    ///
    /// The definitions for these functions are in PigletViewer.jslib.
    /// </summary>
    public static class JsLib
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
    }
}

#endif
