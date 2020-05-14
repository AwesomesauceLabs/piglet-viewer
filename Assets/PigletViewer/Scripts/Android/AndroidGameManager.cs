using System.IO;
using UnityEngine;

namespace PigletViewer
{
    /// <summary>
    /// Implements PigletViewer behaviour that is specific to the Android platform,
    /// such as reading "intent URIs" (Android lingo for file associations) and
    /// sending the application to the background/foreground.
    /// </summary>
    public class AndroidGameManager : MonoBehaviour
    {
        private string _intentUri;

        /// <summary>
        /// Unity callback that is invoked before the first frame update.
        /// </summary>
        void Start()
        {
            _intentUri = GetAndroidIntentUri();

            if (string.IsNullOrEmpty(_intentUri))
                _intentUri = Path.Combine(Application.streamingAssetsPath, "piggleston.glb");

            GameManager.Instance.StartImport(_intentUri);
        }

        /// <summary>
        /// Get the URI that was used to launch or resume PigletViewer (if any).
        /// This is usually the result of opening a .gltf/.glb from an
        /// Android file browser.
        /// </summary>
        private string GetAndroidIntentUri()
        {
            AndroidJavaClass player
                = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity
                = player.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent
                = currentActivity.Call<AndroidJavaObject>("getIntent");

            return intent.Call<string> ("getDataString");
        }

        /// <summary>
        /// Unity callback that is invoked when the application gains
        /// or loses focus.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            // if Unity Player is losing focus, rather than gaining focus
            if (!hasFocus)
                return;

            string uri = GetAndroidIntentUri();

            // Check that the intent URI has changed.  This
            // prevents the app from reloading the current model
            // every time the user switches focus to the
            // app.

            if (string.IsNullOrEmpty(uri) || uri == _intentUri)
                return;

            _intentUri = uri;

            GameManager.Instance.StartImport(_intentUri);
        }

        /// <summary>
        /// Unity callback that is invoked once per frame.
        /// </summary>
        public void Update()
        {
            // Move the PigletViewer app to the background when the
            // user presses the Android "Back" button.
            //
            // See: https://answers.unity.com/questions/25535/android-back-button-event.html
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                AndroidJavaClass player
                    = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity
                    = player.GetStatic<AndroidJavaObject>("currentActivity");

                activity.Call<bool>("moveTaskToBack", true);
            }
        }
    }
}