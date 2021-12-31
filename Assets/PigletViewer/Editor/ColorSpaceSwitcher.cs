using UnityEditor;
using UnityEngine;

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
        }

        public static void SwitchToGamma()
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
        }
    }
}
