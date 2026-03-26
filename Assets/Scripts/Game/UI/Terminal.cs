using UnityEngine;
using UnityEngine.EventSystems;

using System.Linq;

using Assets.Scripts.Game.SimpleScript;
using UnityEngine.InputSystem;

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

    // Command history (index 0 = most recent)
    System.Collections.Generic.List<string> _commandHistory = new();
    const int _commandHistoryMaxCount = 9;
    int _commandHistoryIndex = -1; // -1 = not browsing history
    string _pendingInput = ""; // input saved before browsing history

    public static bool _IsFocused { get { return EventSystem.current.currentSelectedGameObject == s_Singleton._tmpInput.gameObject; } }

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

        _commandHistoryIndex = -1;
        _pendingInput = "";
        _currentInput = _tmpInput.text = "";
        Focus();
      });
    }

    //
    public void Update()
    {
      if (!_IsFocused) return;

      if (Keyboard.current.upArrowKey.wasPressedThisFrame)
      {
        if (_commandHistory.Count == 0) return;
        if (_commandHistoryIndex == -1)
          _pendingInput = _tmpInput.text;
        _commandHistoryIndex = Mathf.Min(_commandHistoryIndex + 1, _commandHistory.Count - 1);
        _tmpInput.text = _commandHistory[_commandHistoryIndex];
        _tmpInput.caretPosition = _tmpInput.text.Length;
      }
      else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
      {
        if (_commandHistoryIndex == -1) return;
        _commandHistoryIndex--;
        _tmpInput.text = _commandHistoryIndex == -1
          ? _pendingInput
          : _commandHistory[_commandHistoryIndex];
        _tmpInput.caretPosition = _tmpInput.text.Length;
      }
    }

    //
    public static void HandleCommand(string command)
    {
      command = command.Trim();

      // Record non-empty commands in history
      if (!string.IsNullOrEmpty(command))
      {
        var hist = s_Singleton._commandHistory;
        if (hist.Count == 0 || hist[0] != command)
          hist.Insert(0, command);
        if (hist.Count > _commandHistoryMaxCount)
          hist.RemoveAt(hist.Count - 1);
      }
      switch (command)
      {
        case "script new":

          // Create new player entity
          var playerEntity = new ScriptEntity(0, new Vector3(-20, 0, 0), 0);
          playerEntity._EntityData.ItemStorage = Enumerable.Repeat<Item.ItemData>(null, 4).ToList();
          playerEntity._ScriptSpawned = false;

          UIElements.s_Singleton._EditorPanel.SetNewScript(playerEntity);
          break;

        case "script run":

          UIElements.s_Singleton._EditorPanel.RunNewScript();
          break;

        case "remove":

          var selectedEntity = PlayerController.s_Singleton._SelectedEntity;
          if (selectedEntity != null)
            ScriptEntity.DestroyEntity(selectedEntity);
          break;

        case "save":
          ScriptEntityHelper.SaveGame();
          break;

        default:
          s_Singleton.LogMessage($"<color=red>Unknown command: {command}</color>");
          break;
      }
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