using System.Collections.Generic;
using UnityEngine;

using System.Linq;

using System.IO;

using StringMath;
using System;

using CustomUI;

namespace SimpleScript
{

  public class ScriptManager
  {

    public static ScriptManager s_Singleton;
    static int s_scriptId;

    Dictionary<int, ScriptBase> _scripts;

    string[] _conditionalOperators = new string[] { "==", "!=", "<", ">", ">=", "<=" };
    static string[] s_conditionalOperators { get { return s_Singleton._conditionalOperators; } }

    public static void Initialize()
    {
      s_Singleton = new ScriptManager();

      ScriptEntity.ScriptEntityHelper.Init();
      s_Singleton.InitializeSystemFunctions();
    }

    public enum ScriptType
    {
      NONE,

      PLAYER,

      ENTITY,
      ITEM
    }

    public struct ScriptLoadData
    {
      public string PathTo;
      public ScriptType ScriptType;

      public string Headers;
    }
    public ScriptBase AttachScriptTo(ScriptEntity entity, string scriptRaw, int ownerId)
    {
      // Check empty script
      if (scriptRaw.Trim().Length == 0)
      {
        Debug.LogError("Cannot attach empty script");
        return null;
      }

      //
      _scripts ??= new();

      var scriptId = s_scriptId++;
      var newScript = new ScriptBase(entity, scriptRaw)
      {
        _Id = scriptId,
        _OwnerId = ownerId
      };

      _scripts.Add(scriptId, newScript);
      return newScript;
    }

    static void RemoveScript(int scriptId, string returnData = null)
    {
      // Delete script
      var script = s_Singleton._scripts[scriptId];
      script.RemoveScript();
      s_Singleton._scripts.Remove(scriptId);

      // Check for parent scripts
      var parentScript = script._ParentScript;
      if (parentScript != null)
      {
        parentScript._ExternalReturnData = returnData;
        parentScript.Enable();
        var forceTick = returnData?.StartsWith("!E") ?? false;
        parentScript.Tick(forceTick);
      }
    }
    //
    static void RemoveScript(ScriptBase script, string returnData = null)
    {
      RemoveScript(script._Id, returnData);
    }

    const string _SCRIPTING_PATH = @"Scripting";
    const string _SCRIPTING_PATH_PLAYER = _SCRIPTING_PATH + "/Player";
    const string _SCRIPTING_PATH_ENTITY = _SCRIPTING_PATH + "/System/Entity";
    const string _SCRIPTING_PATH_ITEM = _SCRIPTING_PATH + "/System/Item";
    public static string LoadScript(string dir, string scriptPath)
    {
      scriptPath = $@"{dir}/{scriptPath}";
      if (File.Exists(scriptPath))
        return File.ReadAllText(scriptPath);

      Debug.LogError("Script not found: " + scriptPath);
      return "";
    }
    public static string[] LoadScripts(string dir)
    {
      return Directory.GetFiles(dir);
    }

    public static string LoadPlayerScript(string scriptPath)
    {
      return LoadScript(_SCRIPTING_PATH_PLAYER, $"{scriptPath}.script");
    }

    public static string LoadSystemEntityScript(string scriptPath)
    {
      return LoadScript(_SCRIPTING_PATH_ENTITY, $"{scriptPath}");
    }
    public static string[] GetSystemEntityScripts()
    {
      return LoadScripts(_SCRIPTING_PATH_ENTITY);
    }

    public static string LoadSystemItemScript(string scriptPath)
    {
      return LoadScript(_SCRIPTING_PATH_ITEM, $"{scriptPath}");
    }
    public static string[] GetSystemItemScripts()
    {
      return LoadScripts(_SCRIPTING_PATH_ITEM);
    }

    // Tick update all scripts
    public static void TickScripts()
    {
      var scripts = s_Singleton._scripts;
      if (scripts != null)
      {
        var keys = new List<int>(scripts.Keys);
        foreach (var key in keys)
        {
          if (!scripts.ContainsKey(key))
          {
            Debug.LogWarning($"Trying to tick non-existant script[{key}]!");
            continue;
          }

          var script = scripts[key];
          script.Tick();
        }
      }
    }

    public class ScriptBase
    {
      public int _Id, _OwnerId;
      string _codeRaw;
      bool _isEnabled, _breakLoop;
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

      //
      bool _isValid;
      public void RemoveScript()
      {
        _isValid = false;
      }

      // Lines in the script
      string[] _lines;
      string _line, _lineOriginal;

      // Current line index
      int _lineIndex,

        // The depth of the current line being read
        _lineDepth,

        // The depth of the line that can be read
        _logicDepth,

        // Last tick ran
        _lastTick;

      Dictionary<string, string> _variables;

      string _externalReturnData;
      public string _ExternalReturnData { set { _externalReturnData = value; } get { return _externalReturnData; } }
      string _externalReturnStatement;
      string _externalLine;

      ScriptEntity _attachedEntity;
      public ScriptEntity _AttachedEntity { get { return _attachedEntity; } }

      ScriptBase _parentScript;
      public ScriptBase _ParentScript { get { return _parentScript; } }

      public ScriptBase(ScriptEntity entity, string codeRaw)
      {
        _attachedEntity = entity;
        _codeRaw = codeRaw;

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
        _variables = new Dictionary<string, string>
        {
          { "this", GetEntityStatement(_attachedEntity) }
        };

        //
        _externalReturnData = _externalReturnStatement = _externalLine = null;
      }

      // Parse simplescript
      public void Tick(bool forceTick = false)
      {

        Debug.Log($"Attempting to tick script: {_attachedEntity._EntityTypeData.Name}");

        // Check can tick
        if (!_isEnabled || !_isValid) return;
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

          // Gather line
          _line = _lines[_lineIndex++].Trim();
          _lineOriginal = _line;

          // Check external return statement
          if (_externalReturnData != null)
          {
            //Debug.Log($"External return data found.. replacing: {_externalReturnData} [{_externalReturnStatement}] ........... {line}");

            // Check error
            if (_ExternalReturnData.StartsWith("!E"))
            {
              var error = _ExternalReturnData[2..].Trim();
              logError(error);
              break;
            }

            // Substitute return data into statement
            if (_externalReturnStatement != null)
            {
              int statementPos = _externalLine.IndexOf(_externalReturnStatement);
              _line = _externalLine.Remove(statementPos, _externalReturnStatement.Length).Insert(statementPos, _externalReturnData);
            }

            // Clear external return data
            _externalReturnData = _externalReturnStatement = _externalLine = null;
          }

          // Check blank line
          if (_line.Length == 0) continue;
          Debug.Log("Read line: " + _line);

          // Check comment
          if (_line.StartsWith(@"//") || _line.StartsWith("#") || _line.StartsWith("$"))
          {
            continue;
          }

          // Check depth
          if (_line == "end")
          {

            if (_logicDepth == 0 && _lineDepth == 0)
            {
              logError($"Unexpected 'end'");
              return;
            }

            // Exit logic
            if (_lineDepth == _logicDepth)
              _logicDepth--;

            _lineDepth--;
            continue;
          }

          else if (_line == "else")
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
              _logicDepth--;

            continue;
          }

          if (_lineDepth != _logicDepth) continue;

          //
          if (tokensAlloted-- == 0)
          {
            logError($"Logic tokens drained");
            break;
          }

          // New variable assignment
          if (_line.StartsWith("var ") && _line.Contains("="))
          {
            var lineSplit = _line.Split("=");
            if (lineSplit.Length > 1)
            {
              var variable = lineSplit[0][4..].Trim();
              if (_variables.ContainsKey(variable))
              {
                logError($"Variable '{variable}' already defined");
                return;
              }

              var value = HandleStatement(lineSplit[1].Trim(), true);
              if (_breakLoop) break;
              _variables.Add(variable, value);
              continue;
            }
            else
            {
              logError($"Invalid variable definition syntax");
              return;
            }
          }

          // Existing variable assignment
          if (!_line.Contains("==") && _line.Contains("="))
          {
            var lineSplit = _line.Split("=");
            if (lineSplit.Length > 1)
            {
              var variable = lineSplit[0].Trim();
              if (!_variables.ContainsKey(variable))
              {
                logError($"Variable '{variable}' not defined");
                return;

              }

              var value = HandleStatement(lineSplit[1].Trim(), true);
              if (_breakLoop) break;
              _variables[variable] = value;
              continue;
            }
            else
            {
              logError($"Invalid variable assignment syntax");
              return;
            }
          }

          // Check for logic
          if (_line.StartsWith("if"))
          {

            var lineLogic = _line[2..].Trim();
            var logic = HandleLogic(lineLogic);
            if (_breakLoop) break;

            // If true, go inside of if statement +1 depth
            _lineDepth++;
            if (logic)
              _logicDepth++;
            continue;
          }

          HandleStatement(_line, false);

          //
          if (_breakLoop) break;
        }

        // Apply tick cooldown
        _attachedEntity._TickCooldown = Mathf.Clamp(_attachedEntity._TickCooldown, _tickCooldown, 100);
      }

      string CheckSubstitueVariable(string accessor)
      {
        foreach (var pair in _variables)
        {
          if (accessor == pair.Key)
          {
            var gotVariable = CheckSubstitueVariable(pair.Value);
            Debug.Log($"Substituted variable: {accessor} => {gotVariable}");
            return gotVariable;
          }
        }
        return accessor;
      }

      // Evaluate parameters
      string EvaluateParameter(string parameter)
      {
        //Debug.Log($"Evaluating parameter: {parameter}");
        parameter = CheckSubstitueVariable(parameter);
        if ((parameter.Contains("(") && parameter.Contains(")")) || parameter.Contains("."))
        {
          var parameterSave = parameter;
          parameter = HandleStatement(parameter, true);
          Debug.Log($"Evaluated param: {parameterSave} => {parameter}");
        }
        return parameter;
      }

      // Evaluate one logical expression
      bool GetLogic(string val0, string val1, string operator_)
      {

        if (!s_conditionalOperators.Contains(operator_))
        {
          logError($"Invalid conditional operator");
          return false;
        }
        //Debug.Log($"Checking logic: {val0} {operator_} {val1}");

        val0 = EvaluateParameter(val0);
        val1 = EvaluateParameter(val1);

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
        // Loop through words, checking parenthesis
        var logicSplit = lineLogic.Split(" ");
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
          var evaluate = GetLogic(logicVal0, logicVal1, logicOperator);
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

      string HandleStatement(string statement, bool parameterCheck)
      {
        // Split statement into traversable list marked as accessor or function; substitute variables
        //Debug.Log($"Handling statement: {statement} (Param check: {parameterCheck})");
        List<(string, int)> statementData = new();
        var returnStatement = "";

        // Traverse letter by letter
        var currentWord = "";
        var wordType = -1;
        var functionCounter = 0;
        Debug.Log($"Handling statement: {statement}");
        for (var i = 0; i < statement.Length; i++)
        {
          var letter = statement[i];

          // Check function
          if (letter == '(')
            functionCounter++;
          else if (letter == ')')
            functionCounter--;

          // Check type
          if (letter == '.' || letter == ':' || i == statement.Length - 1)
          {

            var wordTypeSave = wordType;

            // Check last word
            if (i == statement.Length - 1)
            {
              currentWord += letter;
              i++;
            }

            // Check next word type
            var resetWord = true;
            if (letter == '.')
            {

              // Check function parameters
              if (functionCounter > 0)
                resetWord = false;
              else
                wordType = 0;
            }
            else if (letter == ':')
            {
              wordType = 1;
            }

            if (resetWord)
            {
              // Store data type
              if (statementData.Count == 0)
                wordTypeSave = currentWord.Contains('(') ? 1 : 0;

              if (wordTypeSave == 0)
              {
                var currentWordLength = currentWord.Length;
                currentWord = CheckSubstitueVariable(currentWord);
                if (currentWord.Contains('.') || currentWord.Contains(':'))
                {
                  statement = currentWord + statement.Remove(i - currentWordLength, currentWordLength);
                  i -= currentWordLength + 1;
                  currentWord = "";
                  continue;
                }
              }

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

              Debug.Log($"Checking variable {word}");

              var accessorLastSave = accessorLast;
              accessorLast = word;

              // If first accessor, check valid
              if (i == 0)
              {

                // Check server authenticated
                if (word == "_" && _OwnerId != -1)
                {
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

                // Check string
                if (word.StartsWith('"') && word.EndsWith('"'))
                {
                  if (parameterCheck)
                    returnStatement = word;
                  continue;
                }

                // Check entity variable
                if (IsValidVariableEntity(word))
                {
                  currentTarget = new ScriptTarget(GetEntityByStatement(word));
                  currentTargetDepth = i;

                  if (parameterCheck)
                    returnStatement = word;
                  continue;
                }

                // Check item variable
                if (IsValidVariableItem(word))
                {
                  currentTarget = new ScriptTarget(GetItemByStatement(word));
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
              if (IsValidVariableEntity(returnStatement))
              {
                var entityGot = GetEntityByStatement(returnStatement);
                if (entityGot.HasEntityVariable_Int(word))
                {
                  returnStatement = $"{entityGot.GetEntityVariable_Int(word)}";

                  validAccessors = new()
                      {
                        word
                      };
                }
              }

              // Validate
              if (!(validAccessors?.Contains(word) ?? false))
              {
                logError($"Null object reference ({accessorLastSave} => {word})");
                break;
              }

              break;

            // Function
            case 1:

              Debug.Log($"Checking function {word}");

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
                  for (var u = 0; u < parameters.Length; u++)
                  {
                    var nextChar = parameters[u];

                    // Check depth
                    if (nextChar == '(' || nextChar == '[')
                      parameterDepth++;
                    if (nextChar == ')' || nextChar == ']')
                      parameterDepth--;

                    //
                    if (nextChar == ',')
                      if (parameterDepth == 0)
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

                /// TODO fix this....
                string EvalutateP(string p)
                {
                  // Check for logic in parameter; if logic exists, evaluate and return result as boolean string
                  if (s_conditionalOperators.Any(op => p.Contains(op)))
                  {
                    return HandleLogic(p) ? "true" : "false";
                  }

                  // Else, just evaluate parameter normally
                  return HandleStatement(p.Trim(), true);
                }

                //
                for (var u = 0; u < functionParameters.Count; u++)
                {

                  // Arithmetic
                  var evaluated = false;
                  foreach (var arithmeticOp in new char[] { '+', '-', '*', '/' })
                    if (functionParameters[u].Contains(arithmeticOp))
                    {
                      evaluated = true;

                      var statementSplit = functionParameters[u].Split(arithmeticOp);

                      for (var y = 0; y < statementSplit.Length; y++)
                        statementSplit[y] = EvalutateP(statementSplit[y]);
                      functionParameters[u] = ((float)string.Join(arithmeticOp, statementSplit).Eval()) + "";
                    }

                  // No arthimetic
                  if (!evaluated)
                    functionParameters[u] = EvalutateP(functionParameters[u]);
                }
              }

              Debug.Log($"Checking method {functionName} with parameters: {string.Join(", ", functionParameters)} .. {_breakLoop}");

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
                    isEntityFunction = isValidFunction = ScriptEntity.ScriptEntityHelper.HasFunction(currentTarget._ScriptEntity, functionName);
                    break;
                  case ScriptTarget.TargetType.ITEM:
                    isItemFunction = isValidFunction = ItemManager.HasFunction(currentTarget._Item, functionName);
                    break;
                }
              }

              // Check system function
              if (!isValidFunction)
                foreach (var systemFunction in s_Singleton._systemFunctions)
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
                  logError($"Null-reference exception NULL:{functionName})");
                break;
              }

              // Validate function # parameters (entity/item only)
              if (isEntityFunction || isItemFunction)
              {
                var numValidParameters = isEntityFunction ?
                  ScriptEntity.ScriptEntityHelper.s_FunctionRepository.GetFunctionParameterCount(functionName) :
                  ItemManager.s_FunctionRepository.GetFunctionParameterCount(functionName);

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

              // Entity function
              var serverAuthenticated = _OwnerId == -1;
              if (isEntityFunction)
              {

                // Check distance
                var maxDistance = 1;
                var distance = Math.Abs(
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
                    validFacing = targetPosition.Equals((_attachedEntity._TilePosition.x + 1, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z));
                    break;
                  case 3:
                    validFacing = targetPosition.Equals((_attachedEntity._TilePosition.x - 1, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z));
                    break;
                }
                if (!validFacing)
                {
                  logError($"Not facing target");
                  break;
                }

                // Fire function
                Debug.Log($"Firing entity function: {currentTarget._Type}:{functionName}");

                // Attach to entity interacting with
                var entityScript = currentTarget._ScriptEntity.LoadAndAttachScript(new ScriptLoadData()
                {
                  PathTo = $"{currentTarget._Type.ToLower()}.{functionName}",
                  ScriptType = ScriptType.ENTITY
                });
                entityScript._parentScript = this;
                entityScript._variables.Add("_entity", GetEntityStatement(_attachedEntity));

                // Add script parameters
                for (var u = 0; u < functionParameters.Count; u++)
                {
                  var newParameter = functionParameters[u];
                  entityScript._variables.Add($"_param{u}", newParameter);
                }

                // Check for return statement
                if (parameterCheck)
                {
                  _externalReturnStatement = statement;
                  _externalLine = _line;

                  // Keep line index the same to re-fire function until it returns data
                  _lineIndex--;
                }

                // Wait for script to complete
                _breakLoop = true;
                _isEnabled = false;

                // Tick script now!
                entityScript.Tick();

                break;
              }

              // Item function
              else if (isItemFunction)
              {

                // Fire function
                Debug.Log($"Firing item function: {currentTarget._Type}:{functionName}");

                // Attach to entity wielding item
                var itemScript = _attachedEntity.LoadAndAttachScript(new ScriptLoadData()
                {
                  PathTo = $"{currentTarget._Type.ToLower()}.{functionName}",
                  ScriptType = ScriptType.ITEM
                });
                itemScript._parentScript = this;

                // Add script parameters
                for (var u = 0; u < functionParameters.Count; u++)
                {
                  var newParameter = functionParameters[u];
                  itemScript._variables.Add($"_param{u}", newParameter);
                }

                // Check for return statement
                if (parameterCheck)
                {
                  _externalReturnStatement = statement;
                  _externalLine = _line;

                  // Keep line index the same to re-fire function until it returns data
                  _lineIndex--;
                }

                // Wait for script to complete
                _breakLoop = true;
                _isEnabled = false;

                // Tick script now!
                itemScript.Tick();

                break;
              }

              // System functions
              else if (isSystemFunction)
              {
                Debug.Log($"Firing system function: {accessorLastSave}:{functionName}");
                var systemFunction = s_Singleton._systemFunctions[functionName];
                var systemReturnData = systemFunction.Execute(this, accessorLastSave, functionParameters.ToArray());
                var returnData = systemReturnData.Data;

                var systemTickCooldown = systemReturnData.TickCooldown;
                _tickCooldown = systemTickCooldown > -1 ? systemTickCooldown : 0;

                // Check script removed
                if (!s_Singleton._scripts.ContainsKey(_Id))
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

      // Log error
      void logError(string error)
      {

        // Log
        var errorString = $"Line [{_lineIndex}]: [{_lineOriginal}] [{error}]";
        Debug.LogError(errorString);
        _attachedEntity.AppendLog($"<color=red>{errorString}</color>");

        // Exit loop and remove script
        _breakLoop = true;
        ScriptManager.RemoveScript(this, $"!E {error}");
      }

    }

    //
    Dictionary<string, SystemFunction> _systemFunctions;
    public class SystemFunction
    {
      public string _Name;

      // Script base, accesor, parameters
      public Func<ScriptBase, string, string[], SystemFunctionReturnData> _Function;

      public SystemFunctionReturnData Execute(ScriptBase script, string accessor, string[] parameters)
      {
        return _Function(script, accessor, parameters);
      }
    }

    public class SystemFunctionReturnData
    {
      public string Data;
      public int TickCooldown;

      public SystemFunctionReturnData()
      {
        TickCooldown = 1;
      }

      public static SystemFunctionReturnData Success(int tickCooldown = 1)
      {
        return new SystemFunctionReturnData() { TickCooldown = tickCooldown };
      }
      public static SystemFunctionReturnData Success(string data, int tickCooldown = 1)
      {
        return new SystemFunctionReturnData() { Data = data, TickCooldown = tickCooldown };
      }

      // Errors
      public static SystemFunctionReturnData Error(string errorCode)
      {
        return new SystemFunctionReturnData() { Data = $"!E{errorCode}" };
      }
      public static SystemFunctionReturnData Error(string errorCode, params object[] args)
      {
        return new SystemFunctionReturnData() { Data = $"!E{errorCode} {string.Join(" ", args)}" };
      }

      public static SystemFunctionReturnData InvalidFunction()
      {
        return Error("1000");
      }
      public static SystemFunctionReturnData InvalidParameters(int expected)
      {
        return Error("1001", expected);
      }
      public static SystemFunctionReturnData NullReference()
      {
        return Error("1002");
      }
      public static SystemFunctionReturnData NotImplemented()
      {
        return Error("9999");
      }
      public static SystemFunctionReturnData Custom(string customError)
      {
        return Error("9000", customError);
      }
    }

    public static void RegisterSystemFunction(string name, Func<ScriptBase, string, string[], SystemFunctionReturnData> function)
    {
      s_Singleton._systemFunctions.Add(name, new SystemFunction()
      {
        _Name = name,
        _Function = function
      });
    }

    void InitializeSystemFunctions()
    {
      _systemFunctions = new();

      // Log function
      RegisterSystemFunction(
        "log",
        (ScriptBase script, string accessor, string[] parameters) =>
        {

          // Validate accessor
          if (accessor != "")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 1)
          {
            return SystemFunctionReturnData.InvalidParameters(1);
          }

          var logMessage = parameters[0];
          Debug.Log(logMessage);
          script._AttachedEntity.AppendLog(logMessage);

          return SystemFunctionReturnData.Success(0);
        }
      );

      // Exit; remove script
      RegisterSystemFunction(
        "exit",
        (ScriptBase script, string accessor, string[] parameters) =>
        {

          // Validate accessor
          if (accessor != "")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length > 1)
          {
            return SystemFunctionReturnData.InvalidParameters(1);
          }

          // Check return statement
          var returnData = parameters.Length == 1 ? parameters[0] : null;
          Debug.Log($"Script exit called with return data: {returnData}");

          // Remove script
          RemoveScript(script, returnData);

          return SystemFunctionReturnData.Success(-1);
        }
      );

      // Move
      RegisterSystemFunction(
        "move",
        (ScriptBase script, string accessor, string[] parameters) =>
        {

          // Validate accessor
          if (IsValidVariableEntity(accessor))
          {
            return SystemFunctionReturnData.NotImplemented();
          }
          else
            switch (accessor)
            {
              // Entity move; move(0)
              case "":

                // Validate parameters
                if (parameters.Length != 1)
                {
                  return SystemFunctionReturnData.InvalidParameters(1);
                }

                // Move entity in direction
                var command = int.Parse(parameters[0]);
                var commandTarget = script._AttachedEntity;
                commandTarget.ReceiveCommand(
                  command switch
                  {
                    0 => ScriptEntity.EntityCommand.MoveUp,
                    1 => ScriptEntity.EntityCommand.MoveDown,
                    2 => ScriptEntity.EntityCommand.MoveLeft,
                    3 => ScriptEntity.EntityCommand.MoveRight,

                    _ => ScriptEntity.EntityCommand.None
                  },
                  script._OwnerId
                );

                return SystemFunctionReturnData.Success(-1);

              // System-level move; move(4, 0, 0, 1)
              case "_":

                // Validate parameters
                if (parameters.Length != 4)
                {
                  return SystemFunctionReturnData.InvalidParameters(4);
                }

                // Get entity by variable or id
                var entityData = parameters[0];
                ScriptEntity entity = GetEntityByIdOrStatement(entityData);
                if (entity == null)
                {
                  return SystemFunctionReturnData.NullReference();
                }

                // Move entity to position
                var position_x = parameters[1];
                var position_y = parameters[2];
                var position_z = parameters[3];
                entity.TryMove((int.Parse(position_x), int.Parse(position_z), int.Parse(position_y)), true);

                return SystemFunctionReturnData.Success(0);

              // Invalid accessor
              default:
                return SystemFunctionReturnData.InvalidFunction();
            }
        }
      );

      // Move simplifiers
      RegisterSystemFunction(
        "up",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate parameters
          if (parameters.Length != 0)
          {
            return SystemFunctionReturnData.InvalidParameters(0);
          }

          // Move up
          var systemFunction = _systemFunctions["move"];
          return systemFunction.Execute(script, accessor, new string[] { "0" });
        }
      );
      RegisterSystemFunction(
        "down",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate parameters
          if (parameters.Length != 0)
          {
            return SystemFunctionReturnData.InvalidParameters(0);
          }

          // Move down
          var systemFunction = _systemFunctions["move"];
          return systemFunction.Execute(script, accessor, new string[] { "1" });
        }
      );
      RegisterSystemFunction(
        "left",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate parameters
          if (parameters.Length != 0)
          {
            return SystemFunctionReturnData.InvalidParameters(0);
          }

          // Move left
          var systemFunction = _systemFunctions["move"];
          return systemFunction.Execute(script, accessor, new string[] { "2" });
        }
      );
      RegisterSystemFunction(
        "right",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate parameters
          if (parameters.Length != 0)
          {
            return SystemFunctionReturnData.InvalidParameters(0);
          }

          // Move right
          var systemFunction = _systemFunctions["move"];
          return systemFunction.Execute(script, accessor, new string[] { "3" });
        }
      );

      // Get entity
      RegisterSystemFunction(
        "get",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate accessor
          switch (accessor)
          {

            // Get in front of entity
            case "":
              // Validate parameters
              if (parameters.Length != 0)
              {
                return SystemFunctionReturnData.InvalidParameters(0);
              }

              // Get entity in front of current entity based on direction
              var direction = script._AttachedEntity._EntityData.Direction;
              var usePosition = script._AttachedEntity._TilePosition;
              switch (direction)
              {
                case 0: usePosition.Item3 += 1; break;
                case 1: usePosition.Item3 -= 1; break;
                case 2: usePosition.Item1 -= 1; break;
                case 3: usePosition.Item1 += 1; break;
              }

              var entity = ScriptEntity.GetEntity(usePosition);
              if (entity == null)
              {
                return SystemFunctionReturnData.NullReference();
              }

              // Return found entity
              else
              {
                return SystemFunctionReturnData.Success(GetEntityStatement(entity), 0);
              }

            // Inventory
            case "items":

              // Validate parameters
              if (parameters.Length != 1)
              {
                return SystemFunctionReturnData.InvalidParameters(1);
              }

              var itemSlot = int.Parse(parameters[0]);

              // Get item
              var items = script._AttachedEntity._EntityData.ItemStorage;
              Item item = null;
              if (items != null && itemSlot < items.Count)
              {
                item = items[itemSlot];
              }
              if (item == null)
              {
                return SystemFunctionReturnData.Custom($"Item not found at index: {itemSlot}");
              }

              // Return found entity
              else
              {
                return SystemFunctionReturnData.Success(GetItemStatement(item), 0);
              }

            // System
            case "_":

              // Validate parameters
              if (parameters.Length != 1)
              {
                return SystemFunctionReturnData.InvalidParameters(1);
              }

              // Get entity by id
              var entityId = int.Parse(parameters[0]);
              entity = ScriptEntity.GetEntity(entityId);
              if (entity == null)
              {
                return SystemFunctionReturnData.NullReference();
              }

              // Return found entity
              else
              {
                return SystemFunctionReturnData.Success(GetEntityStatement(entity), 0);
              }

            // Invalid accessor
            default:
              return SystemFunctionReturnData.InvalidFunction();
          }
        }
      );

      // Sleep
      RegisterSystemFunction(
        "sleep",
        (ScriptBase script, string accessor, string[] parameters) =>
        {

          // Validate accessor
          if (accessor != "")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 1)
          {
            return SystemFunctionReturnData.InvalidParameters(1);
          }

          // Validation error
          if (!int.TryParse(parameters[0], out var ticks) || ticks < 0)
          {
            throw new NotImplementedException();
          }

          return SystemFunctionReturnData.Success(ticks);
        }
      );

      // Give item
      RegisterSystemFunction(
        "giveItem",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate accessor
          if (accessor != "_")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 2)
          {
            return SystemFunctionReturnData.InvalidParameters(2);
          }

          // Get entity by id
          var entityData = parameters[0];
          var entity = GetEntityByIdOrStatement(entityData);
          if (entity == null)
          {
            return SystemFunctionReturnData.NullReference();
          }

          var itemId = int.Parse(parameters[1]);

          // Give item
          var item = ItemManager.GiveItem(entity, itemId);
          if (item == null)
          {
            return SystemFunctionReturnData.Custom("Inventory full");
          }
          Terminal.s_Singleton.LogMessage($"Gave item ID {item._ItemTypeData.Name} to entity ID {entityData}");

          //
          return SystemFunctionReturnData.Success(0);
        }
      );

      // Shake entity
      RegisterSystemFunction(
        "shake",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate accessor
          if (accessor != "_")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 2)
          {
            return SystemFunctionReturnData.InvalidParameters(2);
          }

          // Get entity by id
          var entityData = parameters[0];
          var entity = GetEntityByIdOrStatement(entityData);
          if (entity == null)
          {
            return SystemFunctionReturnData.NullReference();
          }

          // Get shake params
          var shakeTime = float.Parse(parameters[1]);

          // Shake
          entity.Shake(shakeTime);

          //
          return SystemFunctionReturnData.Success(0);
        }
      );
    }

    // Function for validating entity variable
    public static bool IsValidVariableEntity(string variable)
    {
      return variable.StartsWith("$Entity[") && variable.EndsWith("]");
    }

    // Function for getting entity from variable
    static ScriptEntity GetEntityByStatement(string statement_)
    {
      statement_ = statement_.Trim();
      if (IsValidVariableEntity(statement_))
        return ScriptEntity.GetEntity(int.Parse(statement_.Split("$Entity[")[1][..^1]));
      return null;
    }
    static ScriptEntity GetEntityById(int id)
    {
      return ScriptEntity.GetEntity(id);
    }
    static ScriptEntity GetEntityByIdOrStatement(string idOrStatement)
    {
      if (IsValidVariableEntity(idOrStatement))
        return GetEntityByStatement(idOrStatement);
      else if (int.TryParse(idOrStatement, out var id))
        return GetEntityById(id);
      return null;
    }

    static string GetEntityStatement(ScriptEntity entity)
    {
      return $"$Entity[{entity._EntityData.Id}]";
    }

    // Function for validating item variable
    public static bool IsValidVariableItem(string variable)
    {
      return variable.StartsWith("$Item[") && variable.EndsWith("]");
    }

    // Function for getting entity from variable
    static Item GetItemByStatement(string statement_)
    {
      statement_ = statement_.Trim();
      if (IsValidVariableItem(statement_))
        return ItemManager.GetItem(int.Parse(statement_.Split("$Item[")[1][..^1]));
      return null;
    }
    static Item GetItemById(int id)
    {
      return ItemManager.GetItem(id);
    }
    static Item GetItemByIdOrStatement(string idOrStatement)
    {
      if (IsValidVariableItem(idOrStatement))
        return GetItemByStatement(idOrStatement);
      else if (int.TryParse(idOrStatement, out var id))
        return GetItemById(id);
      return null;
    }

    static string GetItemStatement(Item item)
    {
      return $"$Item[{item._ItemData.Id}]";
    }

    // Variable applied to entities and can be referened in scripts (ex: health)
    [Serializable]
    public class EntityVariable
    {
      public string Name;
    }
    [Serializable]
    public class EntityVariable_Int : EntityVariable
    {
      public int Value;
    }
  }

}