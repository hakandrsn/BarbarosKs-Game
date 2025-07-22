// Filename: InGameDebugConsole.cs
using UnityEngine;
using System.Collections.Generic;

public class InGameDebugConsole : MonoBehaviour
{
    private struct Log
    {
        public string message;
        public string stackTrace;
        public LogType type;
    }

    private readonly List<Log> _logs = new List<Log>();
    private Vector2 _scrollPosition;
    private bool _show = true;
    private bool _collapse;

    private static readonly Dictionary<LogType, Color> _logTypeColors = new Dictionary<LogType, Color>
    {
        { LogType.Assert, Color.white },
        { LogType.Error, Color.red },
        { LogType.Exception, Color.red },
        { LogType.Log, Color.white },
        { LogType.Warning, Color.yellow },
    };

    private const int _maxLogs = 100;
    private const string _windowTitle = "In-Game Console";
    private readonly GUIContent _clearLabel = new GUIContent("Clear", "Clear the contents of the console.");
    private readonly GUIContent _collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");

    private readonly Rect _titleBarRect = new Rect(0, 0, 10000, 20);
    private Rect _windowRect = new Rect(20, 20, Screen.width - 40, Screen.height - 40);

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void OnGUI()
    {
        if (!_show)
        {
            return;
        }

        _windowRect = GUILayout.Window(123456, _windowRect, DrawConsoleWindow, _windowTitle);
    }

    private void DrawConsoleWindow(int windowID)
    {
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        for (int i = 0; i < _logs.Count; i++)
        {
            var log = _logs[i];
            GUI.contentColor = _logTypeColors[log.type];
            GUILayout.Label(log.message);
        }

        GUILayout.EndScrollView();
        GUI.contentColor = Color.white;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button(_clearLabel))
        {
            _logs.Clear();
        }

        _collapse = GUILayout.Toggle(_collapse, _collapseLabel, GUILayout.ExpandWidth(false));

        GUILayout.EndHorizontal();
        GUI.DragWindow(_titleBarRect);
    }

    private void HandleLog(string message, string stackTrace, LogType type)
    {
        _logs.Add(new Log
        {
            message = message,
            stackTrace = stackTrace,
            type = type,
        });

        while (_logs.Count > _maxLogs)
        {
            _logs.RemoveAt(0);
        }
    }
}