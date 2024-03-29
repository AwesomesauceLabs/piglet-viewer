﻿#if UNITY_WEBGL
using System;
using System.IO;
using System.Runtime.InteropServices;
using Piglet;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// Implements PigletViewer behaviour that is specific to the WebGL platform,
    /// such as invoking Javascript methods from C#.
    /// </summary>
    public class WebGlGameManager : MonoBehaviour
    {
        /// <summary>
        /// Unity callback that is invoked before the first frame update.
        /// </summary>
        void Start()
        {
            Gui.Instance.DefaultFooterMessage
                = "drag .glb/.zip onto window to view";
            Gui.Instance.ResetFooterMessage();

            // run javascript startup tasks (e.g. register event
            // handlers for drag-and-drop)
            JsLib.Init();

            // Set up callbacks for logging progress messages during
            // glTF imports.
            //
            // On the WebGL platform, progress messages are rendered as HTML
            // as part of the containing web page (outside of the Unity WebGL canvas),
            // whereas on Windows/Android the progress messages are rendered
            // directly on top of the view using IMGUI methods.
            ProgressLog.Instance.AddLineCallback = JsLib.AddProgressLogLine;
            ProgressLog.Instance.UpdateLineCallback = JsLib.UpdateProgressLogLine;
            ProgressLog.Instance.ResetLogCallback = JsLib.ResetProgressLog;

            // Set max bone weights per vertex to 4 (default is 2).
            // This prevents skinned glTF models that use more than 2
            // bone weights per vertex from exploding.

#if UNITY_2019_2_OR_NEWER
            QualitySettings.skinWeights = SkinWeights.FourBones;
#else
            QualitySettings.blendWeights = BlendWeights.FourBones;
#endif
        }

        /// <summary>
        /// Import a glTF file that has been selected using the "Choose File"
        /// button on the web page.
        ///
        /// Reading the user's chosen glTF file is a bit tricky in the case of
        /// WebGL. Although the javascript code is allowed
        /// to read the contents of the user-selected glTF file,
        /// it is not provided with the path to the file on the local
        /// filesystem, nor is it allowed to read any files on the local
        /// filesystem. Instead, we must read the byte content of the file
        /// into memory on the javascript side and then copy those bytes
        /// over to the C# side.
        /// </summary>
        /// <param name="filename">
        /// The basename of the input file (not the absolute path!).
        /// This filename is used as a key for accessing the byte
        /// content of the file, which has been read in on the javascript
        /// side.
        /// </param>
        public void ImportFileWebGl(string filename)
        {
            var size = JsLib.GetFileSize(filename);
            var jsData = JsLib.GetFileData(filename);

            var data = new byte[size];
            Marshal.Copy(jsData, data, 0, size);

            JsLib.FreeFileData(filename);

            GameManager.Instance.StartImport(data, filename);
        }

        /// <summary>
        /// Import a model from a .glb/.zip URL that is dragged
        /// onto the HTML canvas.  This wrapper method around
        /// GameManager.StartImport is necessary because there
        /// are multiple versions of GameManager.StartImport
        /// with different arguments, and SendMessage
        /// doesn't correctly handle method overloading.
        /// See: https://answers.unity.com/questions/285988/sendmessage-and-method-overload-dont-get-well-toge.html
        /// </summary>
        public void ImportUrlWebGl(string url)
        {
            GameManager.Instance.StartImport(url);
        }
    }
}

#endif
