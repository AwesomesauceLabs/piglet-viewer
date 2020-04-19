#if UNITY_WEBGL
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Implements PigletViewer behaviour that is specific to the WebGL platform,
/// such as invoking Javascript methods from C#.
/// </summary>
public class WebGlViewerBehaviour : MonoBehaviour
{
    /// <summary>
    /// Unity callback that is invoked before the first frame update.
    /// </summary>
    void Start()
    {
        ViewerGUI.Instance.DefaultFooterMessage
            = "drag .glb/.zip onto window to view";
        ViewerGUI.Instance.ResetFooterMessage();
        
        // run javascript startup tasks (e.g. register event
        // handlers for drag-and-drop)
        PigletViewerJsLib.Init();
        
        // load default model (Piglet mascot) 
        GameManager.Instance.StartImport(Path.Combine(
            Application.streamingAssetsPath, "piglet-1.0.0.glb"));
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
        var size = PigletViewerJsLib.GetFileSize(filename);
        var jsData = PigletViewerJsLib.GetFileData(filename);

        var data = new byte[size];
        Marshal.Copy(jsData, data, 0, size);

        PigletViewerJsLib.FreeFileData(filename);

        GameManager.Instance.StartImport(data, filename);
    }
}
#endif
