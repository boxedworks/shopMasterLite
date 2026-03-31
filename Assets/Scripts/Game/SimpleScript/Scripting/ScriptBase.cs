
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Assets.Scripts.StringMath;
using System.Text.RegularExpressions;
using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using Assets.Scripts.Game.SimpleScript.Entities.Item;
using Assets.Scripts.Game.SimpleScript.Scripting.Logic;

namespace Assets.Scripts.Game.SimpleScript.Scripting
{
  public class ScriptBase
  {
    public int _Id, _OwnerId;
    string _name;
    public string _Name { get { return _name; } set { _name = value; } }
    string _codeRaw;
    public string _CodeRaw { get { return _codeRaw; } }
    public bool _IsEnabled { get { return _isEnabled; } }
    public bool _IsWaitingFor { get { return _isWaitingFor; } }
    bool _isEnabled, _breakLoop, _isWaitingFor;
    int _tickCooldown;
    public void Enable()
    {
      if (_isEnabled)
      {
        Debug.LogWarning($"Script [{_Id}] is already enabled!");
      }

      _isEnabled = true;
    }
    public void Disable()
    {
      if (!_isEnabled)
      {
        Debug.LogWarning($"Script [{_Id}] is already disabled!");
      }

      _isEnabled = false;
    }

    public void StopWaitingFor()
    {
      if (!_isWaitingFor)
      {
        Debug.LogWarning($"Script [{_Id}] is not waiting for anything!");
      }

      _isWaitingFor = false;
    }

    //
    bool _isValid;
    public bool _IsValid { get { return _isValid; } }
    bool _removeScriptOnExit;
    public void RemoveScriptOnExit()
    {
      _removeScriptOnExit = true;
    }
    public void OnRemoveScript()
    {
      _attachedEntity.DetachScript(this);

      _isValid = false;

      // Check destroy
      if (_removeScriptOnExit)
      {
        _attachedEntity.Destroy();

      }
    }

    // Lines in the script
    string[] _lines;
    string _line, _lineOriginal;

    //
    string _error;
    public string _Error { get { return _error; } }
    public bool _HasError { get { return _error != null; } }

    // Current line index
    int _lineIndex,

      // The depth of the current line being read
      _lineDepth,

      // The depth of the line that can be read
      _logicDepth,

      // Last tick ran
      _lastTick;

    public int _LineIndex { get { return _lineIndex; } }

    Dictionary<string, Variable> _variables;

    // Loop
    Stack<LoopData> _loopStack;
    LoopData _lastLoop;

    //
    string _externalReturnData;
    public string _ExternalReturnData { set { _externalReturnData = value; } get { return _externalReturnData; } }
    string _externalReturnStatement;
    string _externalReturnLine;

    ScriptEntity _attachedEntity;
    public ScriptEntity _AttachedEntity { get { return _attachedEntity; } }

    ScriptBase _parentScript;
    public ScriptBase _ParentScript { get { return _parentScript; } }

    int _entityFunctionId;
    FunctionData _functionData { get { return _entityFunctionId == -1 ? null : ScriptEntityHelper.s_FunctionRepository.GetFunctionData(_entityFunctionId); } }

    public ScriptBase(ScriptEntity entity, string codeRaw)
    {
      _attachedEntity = entity;
      _codeRaw = codeRaw;
      _entityFunctionId = -1;

      // Split raw into lines
      _lines = _codeRaw.Split("\n");
      Init();

      //
      _isEnabled = true;
      _isValid = true;
    }

    //
    void Init()
    {
      _lineIndex = _lineDepth = _logicDepth = 0;

      // Add default variables
      {
        _variables = new Dictionary<string, Variable>
        {
          { "this", new Variable(_logicDepth) { _Value = ScriptEntityHelper.GetEntityStatement(_attachedEntity) } }
        };
      }

      //
      _externalReturnData = _externalReturnStatement = _externalReturnLine = null;
    }

    // Parse simplescript
    public void Tick(bool forceTick = false)
    {

      //Debug.Log($"Attempting to tick script: {_attachedEntity._EntityTypeData.Name}");

      // Check can tick
      if (!_isEnabled || !_isValid || _isWaitingFor) return;
      if (!_attachedEntity._CanTick) return;

      var currentTick = GameController.s_CurrentTick;
      if (_lastTick == currentTick)
      {
        Debug.LogWarning($"Attempting to tick [{_attachedEntity._EntityTypeData.Name}] more than 1nce per tick");
        if (!forceTick)
          return;
      }
      _lastTick = currentTick;

      //Debug.Log($"Ticking script [{_Id}] for entity [{_attachedEntity._EntityData.Id}]");

      _breakLoop = false;
      var tokensAlloted = 10;
      _tickCooldown = 1;
      for (; ; )
      {

        // Check end of script
        if (_lineIndex == _lines.Length)
        {
          //Debug.LogWarning("EOS");
          //return;
          Init(); // Loop
        }

        // Check external return statement
        var handledExternalReturn = false;
        if (_externalReturnData != null && _externalReturnData != "null")
        {
          Debug.Log($"External return data found.. replacing: {_externalReturnData} [{_externalReturnStatement}] ........... {_line}");

          // Check error
          if (_externalReturnData.StartsWith("!E"))
          {
            var error = _externalReturnData[2..].Trim();
            logError(error);
            break;
          }

          // Substitute return data into statement
          if (_externalReturnStatement != null)
          {
            int statementPos = _externalReturnLine.IndexOf(_externalReturnStatement);
            _line = _externalReturnLine.Remove(statementPos, _externalReturnStatement.Length).Insert(statementPos, _externalReturnData);
            Debug.Log($"Replaced: {_line}");

            handledExternalReturn = true;
          }

          // Clear external return data
          _externalReturnData = _externalReturnStatement = _externalReturnLine = null;
        }

        // Gather line normally
        if (!handledExternalReturn)
        {
          _line = _lines[_lineIndex++].Trim();
        }
        _lineOriginal = _line;


        //
        HandleLine(_line);

        //
        if (_breakLoop) break;
      }

      // Apply tick cooldown
      _attachedEntity._TickCooldown = Mathf.Clamp(_attachedEntity._TickCooldown, _tickCooldown, 100);
    }

    //
    void HandleLine(string line)
    {
      // Check blank line
      if (line.Length == 0) return;

      //Debug.Log($"Handling line: {line}");

      // Check comment
      if (line.StartsWith(@"//") || line.StartsWith("#"))
      {
        return;
      }

      // Check meta-data
      if (line.StartsWith("$"))
      {

        // Set entity item size
        if (line.StartsWith("$SetItemCount(") && line.EndsWith(")"))
        {
          var size = int.Parse(line[14..^1]);
          _attachedEntity._EntityData.ItemStorage = new ScriptItemStorage
          {
            Items = Enumerable.Repeat<ScriptItemData>(null, size).ToList()
          };
          return;
        }
      }

      // Check depth
      if (line == "end")
      {

        if (_logicDepth == 0 && _lineDepth == 0)
        {
          logError($"Unexpected 'end'");
          return;
        }

        // Exit logic
        if (_lineDepth == _logicDepth)
        {
          _logicDepth--;
          ScopeVariables();

          // Check loop
          if (_loopStack?.Count > 0)
          {
            var loop = _loopStack.Peek();
            if (loop._LogicDepth == _logicDepth)
            {
              _lineIndex = loop._LineIndexStart - 1;

              loop._LoopCounter++;
              _lastLoop = loop;
              _loopStack.Pop();

              if (_loopStack.Count == 0)
                _loopStack = null;
            }
          }
        }

        _lineDepth--;
        return;
      }

      if (line == "else")
      {

        if (_lineDepth == 0)
        {
          logError($"Unexpected 'else'");
          return;
        }

        // Enter else logic
        if (_lineDepth == _logicDepth + 1)
          _logicDepth++;

        // Else acts as end
        else if (_lineDepth == _logicDepth)
        {
          _logicDepth--;
          ScopeVariables();
        }

        return;
      }

      if (line == "continue")
      {
        if (_lineDepth == 0)
        {
          logError($"Unexpected 'continue'");
          return;
        }

        // Exit logic
        if (_lineDepth == _logicDepth)
        {
          if (_loopStack == null)
          {
            logError($"Unexpected 'continue'");
            return;
          }

          var loop = _loopStack.Peek();
          _lineIndex = loop._LineIndexStart - 1;
          _logicDepth = _lineDepth = loop._LogicDepth;
          ScopeVariables();

          loop._LoopCounter++;
          _lastLoop = loop;
          _loopStack.Pop();

          if (_loopStack.Count == 0)
            _loopStack = null;
        }

        return;
      }

      if (line == "break")
      {
        if (_lineDepth == 0)
        {
          logError($"Unexpected 'break'");
          return;
        }

        // Exit logic
        if (_lineDepth == _logicDepth)
        {
          if (_loopStack == null)
          {
            logError($"Unexpected 'break'");
            return;
          }

          var loop = _loopStack.Peek();
          _lineIndex = loop._LineIndexStart - 1;
          _logicDepth = _lineDepth = loop._LogicDepth;

          loop._LoopCounter++;
          loop._Break = true;
          _lastLoop = loop;
          _loopStack.Pop();

          if (_loopStack.Count == 0)
            _loopStack = null;
        }

        return;
      }

      // Check for logic
      if (line.StartsWith("if"))
      {

        if (_lineDepth == _logicDepth)
        {
          var lineLogic = line[2..].Trim();
          var logic = HandleLogic(lineLogic);
          if (_breakLoop) return;

          // If true, go inside of if statement +1 depth
          if (logic)
            _logicDepth++;
        }
        _lineDepth++;

        return;
      }

      // Check for waitFor
      if (line.StartsWith("waitFor"))
      {
        if (_lineDepth == _logicDepth)
        {
          var lineLogic = line[7..].Trim();
          var logic = HandleLogic(lineLogic);
          if (_breakLoop) return;

          if (!logic)
          {
            _lineIndex--;
            _breakLoop = true;
            return;
          }
        }

        return;
      }

      // Check for while loop
      if (line.StartsWith("while"))
      {
        if (_lineDepth == _logicDepth)
        {
          var lastLoopSave = _lastLoop;
          if (_lastLoop?._LineIndexStart == _lineIndex)
          {
            _lastLoop = null;
          }

          var loopBreak = lastLoopSave?._Break ?? false;

          var lineLogic = line[5..].Trim();
          var logic = !loopBreak && HandleLogic(lineLogic);
          if (_breakLoop) return;

          if (logic)
          {
            _loopStack ??= new();
            _loopStack.Push(lastLoopSave ?? new LoopData()
            {
              _Type = LoopData.LoopType.WHILE,
              _LineIndexStart = _lineIndex,
              _LogicDepth = _logicDepth
            });

            _logicDepth++;
          }
        }
        _lineDepth++;

        return;
      }

      // Check for for loop
      if (line.StartsWith("for"))
      {
        if (_lineDepth == _logicDepth)
        {
          LoopData lastLoopSave = _lastLoop;
          if (_lastLoop?._LineIndexStart == _lineIndex)
          {
            _lastLoop = null;
          }

          var loopBreak = lastLoopSave?._Break ?? false;

          int loopCounter = lastLoopSave?._LoopCounter ?? 0;

          var forLoopData = line[3..].Trim();
          if (forLoopData.StartsWith("(") && forLoopData.EndsWith(")"))
            forLoopData = forLoopData[1..^1].Trim();

          var forLoopParts = forLoopData.Split(";");
          if (forLoopParts.Length != 3)
          {
            logError($"Invalid for loop syntax");
            return;
          }
          var forLoopInit = forLoopParts[0].Trim();
          var forLoopLogic = forLoopParts[1].Trim();
          var forLoopIter = forLoopParts[2].Trim();

          //Debug.Log($"Checking for loop: {line} .. {loopCounter} ({forLoopInit}; {forLoopLogic}; {forLoopIter})");

          if (loopCounter == 0)
          {
            _lineDepth++;
            _logicDepth++;
            HandleLine(forLoopInit);
          }
          else if (!loopBreak)
            HandleLine(forLoopIter);

          var logic = !loopBreak && HandleLogic(forLoopLogic);
          if (_breakLoop) return;

          if (logic)
          {
            _loopStack ??= new();
            _loopStack.Push(lastLoopSave ?? new LoopData()
            {
              _Type = LoopData.LoopType.FOR,
              _LineIndexStart = _lineIndex,
              _LogicDepth = _logicDepth
            });

            _logicDepth++;
          }
          else
          {
            _lineDepth--;
            _logicDepth--;
            ScopeVariables();
          }
        }
        _lineDepth++;

        return;
      }

      if (_lineDepth != _logicDepth) return;

      //
      // if (tokensAlloted-- == 0)
      // {
      //   logError($"Logic tokens drained");
      //   break;
      // }

      // New variable assignment
      if (line.StartsWith("var "))
      {
        if (line.Contains("="))
        {
          var lineSplit = line[4..].Split("=");
          if (lineSplit.Length > 1)
          {
            var variable = lineSplit[0].Trim();
            var value = string.Join("=", lineSplit[1..]).Trim();
            SetVariable(variable, value, true);
            return;
          }
        }
        else
        {
          logError($"Invalid variable definition syntax");
          return;
        }
      }

      // Existing variable assignment
      if (!line.Contains("==") && line.Contains("="))
      {
        var lineSplit = line.Split("=");
        if (lineSplit.Length > 1)
        {
          var variable = lineSplit[0].Trim();
          var value = string.Join("=", lineSplit[1..]).Trim();

          // Local variable
          SetVariable(variable, value, false);
          return;
        }
        else
        {
          logError($"Invalid variable assignment syntax");
          return;
        }
      }

      // Else, handle statement normally
      HandleStatement(line, false);
    }

    //
    string CheckSubstitueVariable(string accessor)
    {
      foreach (var pair in _variables)
      {
        if (accessor == pair.Key)
        {
          var gotVariable = CheckSubstitueVariable(pair.Value._Value);
          //Debug.Log($"Substituted variable: {accessor} => {gotVariable}");
          return gotVariable;
        }
      }
      return accessor;
    }

    // Evaluate parameters
    string EvaluateParameter(string parameter)
    {
      //Debug.Log($"Evaluating parameter: {parameter}");

      if (parameter.StartsWith("(") && parameter.EndsWith(")"))
        parameter = parameter[1..^1];

      if (parameter.Contains("++") || parameter.Contains("--"))
        return HandleStatement(parameter, true);

      parameter = CheckSubstitueVariable(parameter);

      if ((parameter.Contains("(") && parameter.Contains(")")) || parameter.Contains(".") || parameter.Contains("++") || parameter.Contains("--"))
        return HandleStatement(parameter, true);

      return parameter;
    }

    //
    bool GetLogicStatement(string val)
    {
      val = EvaluateParameter(val);
      if (val == "true" || val == "1") return true;
      if (val == "false" || val == "0") return false;

      logError($"Invalid logic statement: {val}");
      return false;
    }

    // Evaluate one logical expression
    bool GetLogicComparison(string val0, string val1, string operator_)
    {

      if (!ScriptBaseHelper.s_ConditionalOperators.Contains(operator_))
      {
        logError($"Invalid conditional operator");
        return false;
      }
      //Debug.Log($"Checking logic: {val0} {operator_} {val1}");

      val0 = EvaluateParameter(val0);
      val1 = EvaluateParameter(val1);
      //Debug.Log($"Checking logic: {val0} {operator_} {val1}");

      // Evaluate condition
      var returnValue = false;
      switch (operator_)
      {
        case "==":
          returnValue = val0 == val1;
          break;
        case "!=":
          returnValue = val0 != val1;
          break;

        case "<":
          returnValue = int.Parse(val0) < int.Parse(val1);
          break;
        case "<=":
          returnValue = int.Parse(val0) <= int.Parse(val1);
          break;
        case ">":
          returnValue = int.Parse(val0) > int.Parse(val1);
          break;
        case ">=":
          returnValue = int.Parse(val0) >= int.Parse(val1);
          break;
      }

      //Debug.Log($"Comparing {val0} {operator_} {val1} : {returnValue}");
      return returnValue;
    }

    //
    bool HandleLogic(string lineLogic)
    {

      //Debug.Log($"Handling logic: {lineLogic}");

      /// Link type
      // 0 = AND
      // 1 = OR
      var logicSplit = lineLogic.Split(" ");

      // Check single logic
      if (logicSplit.Length == 1)
      {
        return GetLogicStatement(logicSplit[0]);
      }

      // Loop through words, checking parenthesis
      var logicDepth = 0;
      var lastLogicDepth = -1;
      var logicType = -1;
      var masterLogic = false;
      var logicInit = false;
      Dictionary<int, (bool, int)> logicStore = new();
      for (var i = 0; i < logicSplit.Length; i++)
      {

        var logicVal0 = logicSplit[i];

        // Check logic operands
        if (logicVal0 == "and")
        {
          logicType = 0;
          continue;
        }
        if (logicVal0 == "or")
        {
          logicType = 1;
          continue;
        }

        // Check single-word conditions; variables + true/false
        // Check ! sign before parenthsis to flip value

        // Open parenthesis
        while (logicVal0.StartsWith("("))
        {
          logicDepth++;
          logicVal0 = logicVal0[1..].Trim();
        }

        var logicOperator = logicSplit[++i];
        var logicVal1 = logicSplit[++i];

        // Close parenthesis
        while (logicVal1.EndsWith(")"))
        {
          logicDepth--;
          logicVal1 = logicVal1[..^1].Trim();
        }

        // Evaluate
        var evaluate = GetLogicComparison(logicVal0, logicVal1, logicOperator);
        if (_breakLoop) break;

        // Base master value
        if (!logicInit)
        {
          logicInit = true;
          masterLogic = evaluate;
        }

        // Add logic
        else
        {

          // If parenthesis depth has changed
          if (logicDepth != lastLogicDepth)
          {

            // If going deeper into parenthesis, save old logic value
            if (logicDepth > lastLogicDepth)
            {
              logicStore.Add(lastLogicDepth, (masterLogic, logicType));
              masterLogic = evaluate;
            }

            // Else if leaving parenthesis, get final evaluation of parenthesis
            else
            {
              switch (logicType)
              {
                case 0:
                  masterLogic = masterLogic && evaluate;
                  break;
                case 1:
                  masterLogic = masterLogic || evaluate;
                  break;
              }

              // If resuming logic, resume and combine from parenthesis
              if (logicStore.ContainsKey(logicDepth))
              {
                var returnLogic = logicStore[logicDepth];
                logicStore.Remove(logicDepth);

                var oldLogic = returnLogic.Item1;
                logicType = returnLogic.Item2;

                switch (logicType)
                {
                  case 0:
                    masterLogic = oldLogic && masterLogic;
                    break;
                  case 1:
                    masterLogic = oldLogic || masterLogic;
                    break;
                }
              }

              // Else, just continue into as a new or original depth
            }
          }

          // Normal logic addition
          else
          {
            switch (logicType)
            {
              case 0:
                masterLogic = masterLogic && evaluate;
                break;
              case 1:
                masterLogic = masterLogic || evaluate;
                break;
            }
          }

        }
        lastLogicDepth = logicDepth;

      }

      return masterLogic;
    }

    void SetVariable(string variable, string value, bool newAssignment)
    {
      // Check entity variable
      var isEntityVariable = false;
      if (variable.Contains("."))
      {
        var variableSplit = variable.Split(".");

        var accessorEntity = variableSplit[0];
        variable = variableSplit[1];
        if (accessorEntity == "this")
        {
          isEntityVariable = true;
        }
      }

      //
      if (!isEntityVariable)
      {
        if (_variables.ContainsKey(variable))
        {
          if (newAssignment)
          {
            logError($"Variable '{variable}' already defined");
            return;
          }
        }
        else
        {
          if (!newAssignment)
          {
            logError($"Variable '{variable}' not defined");
            return;
          }
        }
      }

      value = EvaluateArithmetic(value);
      if (_breakLoop) return;
      if (isEntityVariable)
      {
        //Debug.Log($"Setting entity variable: {variable} => {value}");
        _attachedEntity.SetEntityVariable(variable, value);
        return;
      }
      if (newAssignment)
        _variables.Add(variable, new Variable(_logicDepth) { _Value = value });
      else
        _variables[variable]._Value = value;
      return;
    }

    string HandleStatement(string statement, bool parameterCheck)
    {
      //Debug.Log($"Handling statement: {statement} (Param check: {parameterCheck})");

      // Check increment/decrement
      if (statement.Length > 2)
      {
        if (statement.StartsWith("++") || statement.EndsWith("++"))
        {
          var variable = statement.StartsWith("++") ? statement[2..].Trim() : statement[..^2].Trim();
          var value = EvaluateParameter(variable);
          if (int.TryParse(value, out int intValue))
          {
            intValue++;
            SetVariable(variable, intValue.ToString(), false);
            return (statement.StartsWith("++") ? intValue : intValue - 1).ToString();
          }
          else
          {
            logError($"Invalid increment operator usage on non-integer variable '{variable}'");
            return null;
          }
        }
        else if (statement.StartsWith("--") || statement.EndsWith("--"))
        {
          var variable = statement.StartsWith("--") ? statement[2..].Trim() : statement[..^2].Trim();
          var value = EvaluateParameter(variable);
          if (int.TryParse(value, out int intValue))
          {
            intValue--;
            SetVariable(variable, intValue.ToString(), false);
            return (statement.StartsWith("--") ? intValue : intValue + 1).ToString();
          }
          else
          {
            logError($"Invalid decrement operator usage on non-integer variable '{variable}'");
            return null;
          }
        }
      }

      // Split statement into traversable list marked as accessor or function; substitute variables
      List<(string, int)> statementData = new();
      var returnStatement = "";

      // Traverse letter by letter
      var currentWord = "";
      var wordType = 0;
      var functionCounter = 0;
      var insideString = false;
      for (var i = 0; i < statement.Length; i++)
      {
        var letter = statement[i];

        // Check function
        if (!insideString)
        {
          if (letter == '(')
          {
            wordType = 1;
            functionCounter++;
          }
          else if (letter == ')')
            functionCounter--;
        }

        // Check type
        bool isLastLetter = i == statement.Length - 1;
        if (letter == '.' || letter == ':' || letter == '"' || isLastLetter)
        {

          var wordTypeSave = wordType;

          // Check last word
          if (isLastLetter)
          {
            currentWord += letter;
            i++;
          }

          // Check next word type
          var resetWord = true;

          // Variable
          if (letter == '.')
          {

            // Check function parameters
            if (functionCounter > 0 || insideString)
              resetWord = false;
            else
              wordType = 0;
          }

          // Function
          else if (letter == ':')
          {
            if (insideString)
              resetWord = false;
            else
              wordType = 1;
          }

          // String
          else if (letter == '"')
          {
            insideString = !insideString;
            if (functionCounter > 0)
            {
              resetWord = false;
            }
            else
            {
              if (insideString)
              {
                wordType = 0;
              }

              if (!isLastLetter)
                resetWord = false;
            }
          }

          if (resetWord)
          {
            // Store data type
            if (wordTypeSave == 0 && !currentWord.Contains('"'))
            {
              var currentWordLength = currentWord.Length;
              if (statementData.Count == 0)
                currentWord = CheckSubstitueVariable(currentWord);
              if (currentWord.Contains('.') || currentWord.Contains(':'))
              {
                statement = currentWord + statement.Remove(i - currentWordLength, currentWordLength);
                i -= currentWordLength + 1;
                currentWord = "";
                continue;
              }
            }

            //Debug.Log($"Got statement word: {currentWord} .. type: {wordTypeSave}");
            statementData.Add((currentWord, wordTypeSave));

            // Reset for next word
            currentWord = "";
            continue;
          }
        }

        currentWord += letter;
      }

      // Traverse through statement objects until end
      ScriptTarget currentTarget = null;
      var currentTargetDepth = -1;
      var accessorLast = "";
      for (var i = 0; i < statementData.Count; i++)
      {

        var statementSection = statementData[i];
        var word = statementSection.Item1;
        switch (statementSection.Item2)
        {

          // Variable
          case 0:

            //Debug.Log($"Checking variable {word}");

            var accessorLastSave = accessorLast;
            accessorLast = word;

            // If first accessor, check valid
            if (i == 0)
            {

              // Check system authenticated
              if (word == "_" && _OwnerId != -1)
              {
#if UNITY_EDITOR
                Debug.LogWarning($"Authenticated access to system method");
                continue;
#endif
                logError($"Invalid authentication");
                break;
              }

              // Check numbers
              if (int.TryParse(word, out _))
              {
                if (parameterCheck)
                  returnStatement = word;
                continue;
              }

              // Check simple string
              if (ScriptEntityHelper.IsStringVariable(word))
              {

                // Substitue string variables
                while (word.Contains('{') && word.Contains('}'))
                {
                  // Use regex {(.*?)} to find all variables in the string and substitute them
                  var regex = new Regex(@"{(.*?)}");
                  var matches = regex.Matches(word);
                  foreach (Match match in matches)
                  {
                    //Debug.Log($"Found variable in string: {match.Groups[1].Value}");
                    var variableInString = match.Groups[1].Value;
                    var variableValue = EvaluateParameter(variableInString);
                    word = word.Replace($"{{{variableInString}}}", variableValue);
                    //Debug.Log($"Substituted variable in string: {variableInString} => {variableValue} .. new string: {word}");

                    break;
                  }
                }

                if (parameterCheck)
                  returnStatement = word;
                continue;
              }

              // Advanced string parsing
              // if (word.Contains("\""))
              // {
              //   var stringParse = ParseString(word);
              //   continue;
              // }

              // Check entity variable
              if (ScriptEntityHelper.IsValidVariableEntity(word))
              {
                currentTarget = new ScriptTarget(ScriptEntityHelper.GetEntityByStatement(word));
                currentTargetDepth = i;

                if (parameterCheck)
                  returnStatement = word;
                continue;
              }

              // Check item variable
              if (ScriptEntityHelper.IsValidVariableItem(word))
              {
                currentTarget = new ScriptTarget(ScriptEntityHelper.GetItemByStatement(word));
                currentTargetDepth = i;

                if (parameterCheck)
                  returnStatement = word;
                continue;
              }

              // Check item storage
              if (_attachedEntity._HasStorage && word == "items")
              {
                continue;
              }

              // Check all valid first accessors
              if (new string[] { "_", }.Contains(word))
              {
                continue;
              }

              logError($"Null object reference ({word})");
              break;
            }

            // Check float
            if (i == 1)
            {
              var floatString = $"{accessorLastSave}.{word}";
              if (float.TryParse(floatString, out _))
              {
                if (parameterCheck)
                  returnStatement = floatString;
                continue;
              }
            }

            // Check valid accessor based on last accessor
            List<string> validAccessors = null;
            if (ScriptEntityHelper.IsValidTargetVariable(returnStatement))
            {
              var targetGot = ScriptEntityHelper.GetTargetByStatement(returnStatement);
              //Debug.Log($"Is valid variable entity: {returnStatement} .. checking for accessor {word}");
              if (targetGot.HasEntityVariable(word))
              {
                returnStatement = $"{targetGot.GetEntityVariable(word)}";

                validAccessors = new()
                  {
                    word
                  };
              }
            }

            // Check inventory variables
            if (accessorLastSave == "items")
            {

              var target = new ScriptTarget(_attachedEntity);
              var storage = target._ScriptEntity._Storage;
              switch (word)
              {

                // Check size
                case "size":
                  returnStatement = (storage?.Count).ToString();
                  continue;

                // Check count
                case "count":
                  returnStatement = (storage?.Count(x => x != null)).ToString();
                  continue;

                // Check is full/empty
                case "isFull":
                  returnStatement = (storage?.Count(x => x != null) == storage?.Count).ToString().ToLower();
                  continue;
                case "isEmpty":
                  returnStatement = (storage?.Count(x => x != null) == 0).ToString().ToLower();
                  continue;
              }
            }

            // Validate
            if (!(validAccessors?.Contains(word) ?? false))
            {
              returnStatement = "null";
              continue;
              //logError($"Null object reference [{accessorLastSave}] => [{word}]");
              //break;
            }

            break;

          // Function
          case 1:

            //Debug.Log($"Checking function {word}");

            accessorLastSave = accessorLast;
            accessorLast = "";

            // Get function and parameters
            var functionParameters = new List<string>();
            var functionName = "";
            if (word.Contains("(") && word.EndsWith(")"))
            {
              var functionData = new List<string>(word.Split("("));

              functionName = functionData[0];
              functionData.RemoveAt(0);
              var parameters = string.Join("(", functionData)[..^1];

              // Simple single parameter
              if (!parameters.Contains(","))
              {
                var param = parameters.Trim();
                if (param.Length > 0)
                  functionParameters.Add(param);
              }

              // List of parameters or complex parameter(s)
              else
              {
                var parameterDepth = 0;
                var parameterGot = "";
                insideString = false;
                for (var u = 0; u < parameters.Length; u++)
                {
                  var nextChar = parameters[u];

                  // Check depth
                  if (!insideString)
                  {
                    if (nextChar == '(' || nextChar == '[')
                      parameterDepth++;
                    if (nextChar == ')' || nextChar == ']')
                      parameterDepth--;
                  }

                  // String
                  if (nextChar == '"')
                    insideString = !insideString;

                  //
                  if (nextChar == ',')
                    if (parameterDepth == 0 && !insideString)
                    {
                      parameterGot = parameterGot.Trim();
                      if (parameterGot.Length > 0)
                        functionParameters.Add(parameterGot);
                      parameterGot = "";
                      continue;
                    }

                  //
                  parameterGot += nextChar;
                }
                parameterGot = parameterGot.Trim();
                if (parameterGot.Length > 0)
                  functionParameters.Add(parameterGot);
              }

              //
              for (var u = 0; u < functionParameters.Count; u++)
                functionParameters[u] = EvaluateArithmetic(functionParameters[u]);
            }

            //Debug.Log($"Checking method {functionName} with parameters: {string.Join(", ", functionParameters)} .. returnStatement: {returnStatement} .. ctt: {currentTarget?._Type} .. paramCheck: {parameterCheck}");

            if (_breakLoop) break;

            // Validate function exists
            var isValidFunction = false;
            var isEntityFunction = false;
            var isSystemFunction = false;
            var isItemFunction = false;

            // Check entity function
            if (currentTarget != null)
            {
              switch (currentTarget._TargetType)
              {
                case ScriptTarget.TargetType.SCRIPT_ENTITY:
                  isEntityFunction = isValidFunction = ScriptEntityHelper.HasFunction(currentTarget._ScriptEntity, functionName);
                  break;
                case ScriptTarget.TargetType.ITEM:
                  isItemFunction = isValidFunction = ScriptItemController.HasFunction(currentTarget._Item, functionName);
                  break;
              }
            }

            // Check system function
            if (!isValidFunction)
              foreach (var systemFunction in ScriptBaseHelper.s_SystemFunctions)
              {
                if (systemFunction.Key == functionName)
                {
                  isSystemFunction = isValidFunction = true;
                  break;
                }
              }

            if (!isValidFunction)
            {
              if (currentTarget != null)
                logError($"Referencing non-existant function {currentTarget._Type}:{functionName})");
              else
                logError($"Null-reference exception NULL:{functionName}())");
              break;
            }

            // Validate function # parameters (entity/item only)
            if (isEntityFunction || isItemFunction)
            {
              var numValidParameters = isEntityFunction ?
                ScriptEntityHelper.GetFunctionParameterCount(currentTarget._ScriptEntity, functionName) :
                ScriptItemController.GetFunctionParameterCount(currentTarget._Item, functionName);

              if (numValidParameters == -1)
              {
                logError($"Referencing non-defined function {functionName})");
                break;
              }
              if (functionParameters.Count != numValidParameters)
              {
                logError($"Invalid number of parameters got for function [{functionName}] {functionParameters.Count}, {numValidParameters} expected");
                break;
              }
            }

            // Check spawned
            if (!_attachedEntity._ScriptSpawned)
              if (!(isSystemFunction && (functionName == "spawn" || functionName == "exit" || functionName == "log" || accessorLastSave == "_")))
              {
                logError($"Entity cannot perform actions before spawning");
                _attachedEntity.Destroy();
                break;
              }

            // Entity function
            var serverAuthenticated = _OwnerId == -1;
            if (isEntityFunction)
            {

              // Check distance
              var maxDistance = 1;
              var distance = Mathf.Abs(
                currentTarget._TilePosition.x - _attachedEntity._TilePosition.x +
                currentTarget._TilePosition.y - _attachedEntity._TilePosition.y +
                currentTarget._TilePosition.z - _attachedEntity._TilePosition.z
              );
              if (distance > maxDistance)
              {
                logError($"Target out of range ({distance} > {maxDistance})");
                break;
              }

              // Check facing entity
              var direction = _attachedEntity._Direction;
              var targetPosition = currentTarget._TilePosition;
              var validFacing = false;
              switch (direction)
              {
                case 0:
                  validFacing = targetPosition.Equals((_attachedEntity._TilePosition.x, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z + 1));
                  break;
                case 1:
                  validFacing = targetPosition.Equals((_attachedEntity._TilePosition.x, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z - 1));
                  break;
                case 2:
                  validFacing = targetPosition.Equals((_attachedEntity._TilePosition.x - 1, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z));
                  break;
                case 3:
                  validFacing = targetPosition.Equals((_attachedEntity._TilePosition.x + 1, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z));
                  break;
              }
              if (!validFacing)
              {
                //Debug.LogError($"Not facing target: {targetPosition} .. entity position: {_attachedEntity._TilePosition} .. direction: {direction}");
                logError($"Not facing target");
                break;
              }

              // Fire function
              //Debug.Log($"Firing entity function: {currentTarget._Type}:{functionName}");

              // Attach to entity interacting with
              var entityScript = currentTarget._ScriptEntity.LoadAndAttachScript(new ScriptBaseController.ScriptLoadData()
              {
                PathTo = $"{currentTarget._Type.ToLower()}.{functionName}",
                ScriptType = ScriptBaseController.ScriptType.ENTITY
              });
              entityScript._parentScript = this;
              entityScript._variables.Add("_entity", new Variable(_logicDepth) { _Value = ScriptEntityHelper.GetEntityStatement(_attachedEntity) });

              // Add script parameters
              for (var u = 0; u < functionParameters.Count; u++)
              {
                var newParameter = functionParameters[u];
                entityScript._variables.Add($"_param{u}", new Variable(_logicDepth) { _Value = newParameter });
              }

              // Check for return statement
              if (parameterCheck)
              {
                _externalReturnStatement = statement;
                _externalReturnLine = _line;

                // Keep line index the same to re-fire function until it returns data
                _lineIndex--;
              }

              // Wait for script to complete
              _breakLoop = true;
              _isWaitingFor = true;

              // Tick script now!
              entityScript.Tick();

              break;
            }

            // Item function
            else if (isItemFunction)
            {

              // Fire function
              //Debug.Log($"Firing item function: {currentTarget._Type}:{functionName}");

              // Attach to entity wielding item
              var itemScript = _attachedEntity.LoadAndAttachScript(new ScriptBaseController.ScriptLoadData()
              {
                PathTo = $"{currentTarget._Type.ToLower()}.{functionName}",
                ScriptType = ScriptBaseController.ScriptType.ITEM
              });
              itemScript._parentScript = this;
              itemScript._variables["this"]._Value = ScriptEntityHelper.GetItemStatement(currentTarget._Item);

              // Add script parameters
              for (var u = 0; u < functionParameters.Count; u++)
              {
                var newParameter = functionParameters[u];
                itemScript._variables.Add($"_param{u}", new Variable(_logicDepth) { _Value = newParameter });
              }

              // Check for return statement
              if (parameterCheck)
              {
                _externalReturnStatement = statement;
                _externalReturnLine = _line;

                // Keep line index the same to re-fire function until it returns data
                _lineIndex--;
              }

              // Wait for script to complete
              _breakLoop = true;
              _isWaitingFor = true;

              // Tick script now!
              itemScript.Tick();

              break;
            }

            // System functions
            else if (isSystemFunction)
            {
              //Debug.Log($"Firing system function: {accessorLastSave}:{functionName}");
              var systemFunction = ScriptBaseHelper.s_SystemFunctions[functionName];
              var systemReturnData = systemFunction.Execute(this, accessorLastSave, functionParameters.ToArray());
              var returnData = systemReturnData.Data;

              var systemTickCooldown = systemReturnData.TickCooldown;
              _tickCooldown = systemTickCooldown > -1 ? systemTickCooldown : 0;

              // Check script removed
              if (!ScriptBaseController.HasScript(_Id))
              {
                _breakLoop = true;
                break;
              }

              if (returnData != null)
              {

                // Handle system function errors
                if (returnData.StartsWith("!E"))
                {
                  var errorData = returnData.Split("!E")[1].Split(" ");
                  var errorCode = errorData[0];
                  switch (errorCode)
                  {
                    case "1000":
                      logError($"Function {accessorLastSave}.{functionName} not defined");
                      break;
                    case "1001":
                      var numValidParameters = int.Parse(errorData[1]);
                      logError($"Invalid number of parameters [{functionParameters.Count}] got for function [{functionName}], {numValidParameters} expected");
                      break;
                    case "1002":
                      logError($"Null reference in [{functionName}]");
                      break;
                    case "1003":
                      var parameterIndex = int.Parse(errorData[1]);
                      var parameter = functionParameters[parameterIndex];
                      var expectedType = errorData[2];
                      logError($"Invalid parameter type for parameter [{parameter}] in function [{functionName}]: expected [{expectedType}]");
                      break;

                    case "9000":
                      var customError = string.Join(" ", errorData[1..]);
                      logError($"{customError}");
                      break;
                    case "9999":
                      logError($"Not implemented: [{functionName}]");
                      break;

                    default:
                      logError($"Unknown error: {errorCode}");
                      break;
                  }
                }

                if (parameterCheck)
                  returnStatement = returnData;

                var newTarget = ScriptTarget.TryGetScriptTarget(returnData);
                if (newTarget != null)
                  currentTarget = newTarget;
              }

              //
              if (systemTickCooldown != 0)
              {
                _breakLoop = true;
                break;
              }
            }

            break;
        }

        if (_breakLoop) break;
      }

      return returnStatement;
    }

    //
    string EvaluateArithmetic(string statement)
    {
      //Debug.Log($"Evaluating arithmetic: {statement}");

      string EvalutateP(string p)
      {
        // Check for logic in parameter; if logic exists, evaluate and return result as boolean string
        p = p.Trim();
        if (ScriptBaseHelper.s_ConditionalOperators.Any(op => p.Contains(op)))
        {
          return HandleLogic(p) ? "true" : "false";
        }

        // Else, just evaluate parameter normally
        return HandleStatement(p, true);
      }

      var evaluated = false;

      if (!statement.Contains('"'))
      {
        foreach (var arithmeticOp in new char[] { '-', '+', '*', '/' })
          if (statement.Contains(arithmeticOp))
          {
            evaluated = true;

            var statementSplit = statement.Split(arithmeticOp);

            var ss = statement;
            for (var y = 0; y < statementSplit.Length; y++)
              statementSplit[y] = EvalutateP(statementSplit[y]);
            statement = string.Join(arithmeticOp, statementSplit);
          }
      }

      // String math
      if (evaluated)
        statement = $"{(float)statement.Eval()}";

      // No arithmetic
      else
        statement = EvalutateP(statement);

      return statement;
    }

    // Remove out of scope variables based on logic depth
    void ScopeVariables()
    {
      _variables
        .Where(pair => !pair.Value.IsInScope(_logicDepth))
        .ToList()
        .ForEach(pair =>
        {
          _variables.Remove(pair.Key);
          //Debug.Log($"Removed variable {pair.Key} due to logic depth");
        });
    }

    // Log error
    void logError(string error)
    {

      // Log
      var errorString = $"{_Name ?? "custom script"} [{_lineIndex}]: [{_lineOriginal}] [{error}]";
      Debug.LogError(errorString);
      _attachedEntity.AppendLog($"<color=red>{errorString}</color>");

      // Exit loop and remove script
      _breakLoop = true;
      _error = error;
      ScriptBaseController.RemoveScript(this, $"!E {error}");
    }

  }
}