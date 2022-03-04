using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PigletViewer
{
    /// <summary>
    /// Methods to change the current rendering mode to either
    /// Linear or Gamma.
    /// </summary>
    public class ColorSpaceSwitcher
    {
        public static void SwitchToLinear()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // Note: We do not need to call `SetUseDefaultGraphicsAPIs` for
            // iOS, since Metal is the only choice.

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, true);

            // On Android, using linear rendering mode requires
            // setting the Graphics API to OpenGLES3.

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new [] { GraphicsDeviceType.OpenGLES3 });

            // On WebGL, using linear rendering mode requires
            // setting the Graphics API to OpenGLES3, which corresponds
            // to the "WebGL 2.0" option in the UI.

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new [] { GraphicsDeviceType.OpenGLES3 });
        }

        public static void SwitchToGamma()
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            // Enable "Auto Graphics API" on all platforms.
            //
            // Note: We do not need to call `SetUseDefaultGraphicsAPIs` for
            // iOS, since Metal is the only choice.

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);
        }
    }
}
