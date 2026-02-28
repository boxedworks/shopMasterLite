using UnityEngine;
using UnityEngine.EventSystems;

using SimpleScript;
namespace CustomUI
{
  class EditorPanel
  {

    Transform _panel;
    TMPro.TextMeshProUGUI _lineNumberText;
    TMPro.TMP_InputField _displayText, _inputField;

    ScriptManager.ScriptBase _attachedScript;
    ScriptEntity _attachedEntity { get { return _attachedScript != null ? _attachedScript._AttachedEntity : null; } }

    public bool _IsFocused { get { return EventSystem.current.currentSelectedGameObject == _inputField.gameObject; } }

    int _lineIndex;
    bool _hasError;

    public EditorPanel()
    {
      _panel = GameObject.Find("Editor").transform;

      _displayText = _panel.GetChild(1).GetChild(0).GetChild(0).GetComponent<TMPro.TMP_InputField>();
      _lineNumberText = _panel.GetChild(1).GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
      _inputField = _panel.GetChild(1).GetChild(0).GetChild(2).GetComponent<TMPro.TMP_InputField>();

      _displayText.textComponent.textWrappingMode = _inputField.textComponent.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

      _inputField.onValueChanged.AddListener(UpdateUI);

      _inputField.onSubmit.AddListener((string input) =>
      {
        EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
        _inputField.ActivateInputField();
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
        var selectedEntity = PlayerController.s_Singleton._SelectedEntity;
        if (selectedEntity != null)
        {
          if (selectedEntity._AttachedScripts != null)
          {
            var newScript = selectedEntity._AttachedScripts[0];
            AttachScript(newScript);
          }
        }
      }
    }

    void UpdateUI(string formatText = null)
    {
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
        if (i == _lineIndex - 1)
        {
          var lineNumberTextColor = _attachedScript._HasError ? "red" : "lightblue";
          lineNumberText_ = $"<color={lineNumberTextColor}>{lineNumberText_}</color>";
        }
        lineNumberText += $"{lineNumberText_}\n";

        // var currentLine = lineSplit[i];
        // if (currentLine.Length > lineWidth)
        // {
        //   var extraLines = currentLine.Length / lineWidth;
        //   for (var j = 0; j < extraLines; j++)
        //   {
        //     lineNumberText += "\n";
        //   }
        // }
      }
      _lineNumberText.text = lineNumberText;

      // Try to format text
      var objectColor = "yellow";
      var methodRegex = new System.Text.RegularExpressions.Regex(@"\b((var)|(if)|(else)|(end))\b");
      formatText = methodRegex.Replace(formatText, $"<color={objectColor}>$1</color>");

      var methodColor = "green";
      methodRegex = new System.Text.RegularExpressions.Regex(@"(?![if])(:\w+)|(\w+)(\()");
      formatText = methodRegex.Replace(formatText, $"<color={methodColor}>$1$2</color>$3");

      _displayText.text = formatText;
    }

    //
    public void AttachScript(ScriptManager.ScriptBase script)
    {
      _attachedScript = script;
      _lineIndex = script._LineIndex;
      _hasError = _attachedScript._HasError;

      _inputField.text = script._CodeRaw;
      UpdateUI();
    }

  }
}