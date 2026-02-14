using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomUI
{
  public class Terminal
  {
    public static Terminal s_Singleton { get; private set; }

    // UI components
    RectTransform _terminalPanel;
    TMPro.TextMeshProUGUI _tmpTerminal;
    TMPro.TMP_InputField _tmpInput;

    // Terminal contents
    string _terminalHistory;
    int _terminalHistoryLines, _terminalHistoryMaxLines = 13;
    string _currentInput;

    public static bool IsFocused { get { return EventSystem.current.currentSelectedGameObject == s_Singleton._tmpInput.gameObject; } }

    public Terminal()
    {
      s_Singleton = this;

      // Gather UI elements
      _terminalPanel = GameObject.Find("Terminal").GetComponent<RectTransform>();
      _tmpTerminal = _terminalPanel.GetComponentInChildren<TMPro.TextMeshProUGUI>();
      _tmpInput = _terminalPanel.GetComponentInChildren<TMPro.TMP_InputField>();

      _terminalHistory = _currentInput = "";

      Render();

      //
      _tmpInput.onSubmit.AddListener((string input) =>
      {
        HandleCommand(input);

        _currentInput = _tmpInput.text = "";
        Focus();
      });
    }

    //
    void HandleCommand(string command)
    {
      LogMessage($"<color=red>Unknown command: {command}</color>");
    }

    // Log a message and render the terminal
    public void LogMessage(string message)
    {

      // Check history exceeds max lines, if so remove the first line
      if (_terminalHistoryLines > _terminalHistoryMaxLines - 1)
      {
        var teminalHistoryLinesArray = _terminalHistory.Split('\n');
        _terminalHistory = string.Join("\n", teminalHistoryLinesArray, 1, teminalHistoryLinesArray.Length - 1);
      }
      else
      {
        _terminalHistoryLines++;
      }

      // Add new message to history
      _terminalHistory += message + "\n";

      Render();
    }

    void Render()
    {
      _tmpTerminal.text = $"{_terminalHistory}> {_currentInput}";
    }

    void Focus()
    {
      EventSystem.current.SetSelectedGameObject(_tmpInput.gameObject);
      _tmpInput.ActivateInputField();
    }
  }
}