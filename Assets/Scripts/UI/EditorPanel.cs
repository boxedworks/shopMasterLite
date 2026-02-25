using UnityEngine;
using UnityEngine.EventSystems;

namespace CustomUI
{
  class EditorPanel
  {

    Transform _panel;
    TMPro.TextMeshProUGUI _lineNumberText;
    TMPro.TMP_InputField _displayText, _inputField;

    public EditorPanel()
    {
      _panel = GameObject.Find("Editor").transform;

      _displayText = _panel.GetChild(1).GetChild(0).GetChild(0).GetComponent<TMPro.TMP_InputField>();
      _lineNumberText = _panel.GetChild(1).GetChild(0).GetChild(1).GetComponent<TMPro.TextMeshProUGUI>();
      _inputField = _panel.GetChild(1).GetChild(0).GetChild(2).GetComponent<TMPro.TMP_InputField>();

      _displayText.textComponent.textWrappingMode = _inputField.textComponent.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

      _inputField.onValueChanged.AddListener((string newText) =>
      {
        var formatText = newText;

        // var lineWidth = 51;
        // var lineHeight = 18;
        var lineSplit = formatText.Split('\n');
        var numLines = lineSplit.Length;

        var lineNumberText = "";
        for (var i = 0; i < numLines; i++)
        {
          lineNumberText += (i + 1).ToString() + "\n";

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
        var objectColor = "red";
        var methodRegex = new System.Text.RegularExpressions.Regex(@"((^|\s)var\s)");
        formatText = methodRegex.Replace(formatText, $"<color={objectColor}>$1</color>");
        methodRegex = new System.Text.RegularExpressions.Regex(@"((^|\s)if)([\s(])");
        formatText = methodRegex.Replace(formatText, $"<color={objectColor}>$1</color>$3");
        methodRegex = new System.Text.RegularExpressions.Regex(@"(\selse\s)");
        formatText = methodRegex.Replace(formatText, $"<color={objectColor}>$1</color>");

        var methodColor = "green";
        methodRegex = new System.Text.RegularExpressions.Regex(@"(?![if])(:\w+|\w+)\(");
        formatText = methodRegex.Replace(formatText, $"<color={methodColor}>$1</color>(");

        _displayText.text = formatText;
      });

      _inputField.onSubmit.AddListener((string input) =>
      {
        EventSystem.current.SetSelectedGameObject(_inputField.gameObject);
        _inputField.ActivateInputField();
      });
    }

    //
    public void Update()
    {
      var displayTextText = _displayText.textComponent.transform as RectTransform;
      var inputFieldText = _inputField.textComponent.transform as RectTransform;
      displayTextText.localPosition = inputFieldText.localPosition;
    }

  }
}