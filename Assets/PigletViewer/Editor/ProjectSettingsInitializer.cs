using UnityEditor;

namespace PigletViewer
{
    /// <summary>
    /// Methods to initialize Unity project settings when
    /// creating Unity projects from the command line (for
    /// automated builds and testing).
    /// </summary>
    public class ProjectSettingsInitializer
    {
        public static void InitProjectSettings()
        {
            // Note: In order to reliably automate builds/tests
            // on some platforms (e.g. OSX), it is necessary to initialize
            // the Product Name field to a known/standard value, since that
            // is what Unity uses for the name of the output
            // executable/apk/bundle.

            PlayerSettings.productName = "piglet-viewer";
        }
    }
}