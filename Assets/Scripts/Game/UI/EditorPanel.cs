using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using System.Linq;

using Assets.Scripts.Game.SimpleScript;
namespace CustomUI
{
  public class EditorPanel
  {

    Transform _panel;
    Transform _header { get { return _panel.GetChild(0); } }
    Transform _taskbar { get { return _panel.GetChild(1); } }
    Transform _body { get { return _panel.GetChild(2); } }

    Button _buttonNew { get { return _taskbar.GetChild(0).GetComponent<Button>(); } }
    Button _buttonRun { get { return _taskbar.GetChild(1).GetComponent<Button>(); } }

    RectTransform _currentLineBar;
    TMPro.TextMeshProUGUI _lineNumberText;
    TMPro.TMP_InputField _displayText, _inputField;

    ScriptManager.ScriptBase _attachedScript;
    ScriptEntity _attachedEntity;

    public bool _IsFocused { get { return EventSystem.current.currentSelectedGameObject == _inputField.gameObject; } }

    int _lineIndex, _lastCaretPosition;
    bool _hasError;

    bool _isNewScript;

    public EditorPanel()
    {
      _panel = GameObject.Find("Editor").transform;

      _currentLineBar = _body.GetChild(0).GetChild(0).GetComponent<RectTransform>();
      _displayText = _body.GetChild(0).GetChild(1).GetComponent<TMPro.TMP_InputField>();
      _lineNumberText = _body.GetChild(0).GetChild(2).GetComponent<TMPro.TextMeshProUGUI>();
      _inputField = _body.GetChild(0).GetChild(3).GetComponent<TMPro.TMP_InputField>();

      _displayText.textComponent.textWrappingMode = _inputField.textComponent.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

      _inputField.onValueChanged.AddListener(UpdateUI);

      _inputField.onSubmit.AddListener((string input) =>
      {
        EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
        _inputField.ActivateInputField();
      });

      SetButtonRunActive(true);
      _buttonNew.onClick.AddListener(() =>
      {
        Terminal.HandleCommand("script new");
      });
    }

    //
    public void Update()
    {
      // Keep input and display text synced
      var displayTextText = _displayText.textComponent.transform as RectTransform;
      var inputFieldText = _inputField.textComponent.transform as RectTransform;
      displayTextText.localPosition = inputFieldText.localPosition;

      // Update editor using attached script
      if (_attachedScript != null)
      {

        // If current script invalid, check for new script
        if (!_attachedScript._IsValid)
        {
          if (_attachedEntity._AttachedScripts != null)
          {
            var newScript = _attachedEntity._AttachedScripts[0];
            if (newScript._IsValid)
            {
              AttachScript(newScript);
              return;
            }
          }
        }

        // Check for line index update
        var currentLineIndex = _attachedScript._LineIndex;
        var hasError = _attachedScript._HasError;
        if (currentLineIndex != _lineIndex || hasError != _hasError)
        {
          _lineIndex = currentLineIndex;
          _hasError = hasError;
          UpdateUI();
        }
      }

      // Try to attach script if not attached to selected entity
      else
      {
        var selectedEntity = _attachedEntity ?? PlayerController.s_Singleton._SelectedEntity;
        if (selectedEntity != null)
        {
          if (selectedEntity._AttachedScripts != null)
          {
            var newScript = selectedEntity._AttachedScripts[0];
            AttachScript(newScript);
            return;
          }
        }

        // Check for caret position change
        var caretPositionChanged = _lastCaretPosition != _inputField.caretPosition;
        if (caretPositionChanged)
        {
          _lastCaretPosition = _inputField.caretPosition;
          UpdateUI();
        }
      }

    }

    void UpdateUI(string formatText = null)
    {
      var lineIndex = _attachedScript != null ? _lineIndex : GetLineNumberFromCharIndex(_inputField.caretPosition + 1) + 1;
      if (formatText == null)
      {
        formatText = _inputField.text;
      }

      // var lineWidth = 51;
      // var lineHeight = 18;
      var lineSplit = formatText.Split('\n');
      var numLines = lineSplit.Length;

      var lineNumberText = "";
      for (var i = 0; i < numLines; i++)
      {
        var lineNumberText_ = (i + 1).ToString();
        if (i == lineIndex - 1)
        {
          var lineNumberTextColor = (_attachedScript?._HasError ?? false) ? "red" : "lightblue";
          lineNumberText_ = $"<color={lineNumberTextColor}>{lineNumberText_}</color>";

          UpdateLineBar(lineIndex - 1, _attachedScript?._HasError ?? false);
        }
        lineNumberText += $"{lineNumberText_}\n";
      }
      _lineNumberText.text = lineNumberText;

      // Try to format text
      var objectColor = "yellow";
      var methodRegex = new System.Text.RegularExpressions.Regex(@"\b((var)|(if)|(else)|(end)|(while)|(for)|(continue)|(break))\b");
      formatText = methodRegex.Replace(formatText, $"<color={objectColor}>$1</color>");

      methodRegex = new System.Text.RegularExpressions.Regex(@";");
      formatText = methodRegex.Replace(formatText, $"<color={objectColor}>$0</color>");

      var methodColor = "green";
      methodRegex = new System.Text.RegularExpressions.Regex(@"(?![if])(:\w+)|(\w+)(\()");
      formatText = methodRegex.Replace(formatText, $"<color={methodColor}>$1$2</color>$3");

      var commentColor = "grey";
      var commentRegex = new System.Text.RegularExpressions.Regex(@"(\s*(#|\/\/|\$).*)");
      formatText = commentRegex.Replace(formatText, $"<color={commentColor}>$1</color>");

      _displayText.text = formatText;
    }

    //
    void UpdateLineBar(int lineIndex, bool hasError)
    {
      var lineBarBase = -12.3f;
      _currentLineBar.anchoredPosition = new Vector2(_currentLineBar.anchoredPosition.x, lineBarBase - lineIndex * 20f);

      var lineBarColor = hasError ? new Color(1f, 0f, 0f, 0.04313726f) : new Color(0.3490566f, 0.3490566f, 0.3490566f, 0.09803922f);
      _currentLineBar.GetComponent<Image>().color = lineBarColor;
    }


    // Iterate through lines to find which line contains the charIndex
    private int GetLineNumberFromCharIndex(int charIndex)
    {
      var textInfo = _inputField.textComponent.textInfo;
      for (var i = 0; i < textInfo.lineCount; i++)
      {
        var lineInfo = textInfo.lineInfo[i];
        if (charIndex >= lineInfo.firstCharacterIndex && charIndex <= lineInfo.lastCharacterIndex + 1)
        {
          return i;
        }
      }
      return 0;
    }

    //
    public void AttachScript(ScriptManager.ScriptBase script)
    {
      OnAttachScript(script._AttachedEntity);

      _isNewScript = false;

      _attachedScript = script;
      _attachedEntity = script._AttachedEntity;
      _lineIndex = script._LineIndex;
      _hasError = _attachedScript._HasError;

      _inputField.text = script._CodeRaw;
      UpdateUI();
    }

    //
    public void SetNewScript(ScriptEntity entity)
    {
      OnAttachScript(entity);

      _isNewScript = true;

      _attachedScript = null;
      _attachedEntity = entity;
      _lineIndex = 0;
      _hasError = false;

      _inputField.text = "";

      UpdateUI();
    }

    //
    public void RunNewScript()
    {
      if (_isNewScript)
      {
        var newScript = _attachedEntity.LoadAndAttachRawScript(_inputField.text);
        if (newScript == null)
          return;

        SetButtonRunActive(false);
      }
    }

    //
    void OnAttachScript(ScriptEntity newEntity)
    {
      if (_isNewScript)
      {
        if (newEntity._EntityData.Id == _attachedEntity._EntityData.Id)
          return;
        if (!_attachedEntity._ScriptSpawned)
          _attachedEntity.Destroy();
        _isNewScript = false;
      }

      // Set buttons
      SetButtonRunActive(true);
    }

    //
    void SetButtonRunActive(bool active)
    {
      _buttonRun.onClick.RemoveAllListeners();
      if (active)
        _buttonRun.onClick.AddListener(() =>
        {
          RunNewScript();
        });
    }

  }
}