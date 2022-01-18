using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PigletViewer
{
    /// <summary>
    /// Methods to change the current rendering mode to either
    /// Linear or Gamma.
    /// </summary>
    public class BackendSwitcher
    {
        public static void SwitchToIL2CPP()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        }
    }
}
