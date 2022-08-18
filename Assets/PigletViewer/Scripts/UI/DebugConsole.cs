using UnityEngine;

public class DebugConsole : MonoBehaviour
{
    private Vector2 _scrollPosition = Vector2.zero;
    private string _log = "";

    private class GuiStyles
    {
        public GUIStyle Box;

        public GuiStyles()
        {
            var roundedRectLightGray = Resources.Load<Texture2D>("RoundedRectLightGray");

            Box = new GUIStyle(GUI.skin.box);
            Box.normal.background = roundedRectLightGray;
            Box.alignment = TextAnchor.UpperLeft;
        }
    }

    private GuiStyles _styles;

    private void OnEnable()
    {
        Application.logMessageReceived += OnLogMessage;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogMessage;
    }

    private void OnLogMessage(string message, string stacktrace, LogType type)
    {
        _log += message;
        _log += "\n";
    }

    private void OnGUI()
    {
        if (_styles == null)
            _styles = new GuiStyles();

        GUI.contentColor = Color.black;

        GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));

        GUILayout.Space(Screen.height / 3.0f);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        GUILayout.Box(_log, _styles.Box);
        GUILayout.EndScrollView();

        GUILayout.EndArea();
    }
}
