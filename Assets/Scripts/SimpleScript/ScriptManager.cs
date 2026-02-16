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

    public static void Initialize()
    {
      s_Singleton = new ScriptManager();

      ScriptEntity.ScriptEntityHelper.Init();
      s_Singleton.InitializeSystemFunctions();
    }

    public struct ScriptLoadData
    {
      public string PathTo;
      public bool IsServerScript;

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
        parentScript.Tick();
      }
    }
    //
    static void RemoveScript(ScriptBase script, string returnData = null)
    {
      RemoveScript(script._Id, returnData);
    }

    const string _SCRIPTING_PATH = @"Scripting/";
    public static string LoadScript(string scriptPath)
    {
      scriptPath = $@"{_SCRIPTING_PATH}{scriptPath}.script";
      if (File.Exists(scriptPath))
        return File.ReadAllText(scriptPath);

      Debug.LogError("Script not found: " + scriptPath);
      return "";
    }

    public static string LoadServerScript(string scriptPath)
    {
      scriptPath = $@"{_SCRIPTING_PATH}Server/{scriptPath}";
      if (File.Exists(scriptPath))
        return File.ReadAllText(scriptPath);

      return "";
    }
    public static string[] GetServerScripts()
    {
      return Directory.GetFiles($@"{_SCRIPTING_PATH}Server/");
    }

    // Tick update all scripts
    public static void TickScripts()
    {
      var scripts = s_Singleton._scripts;
      if (scripts != null)
      {
        var scriptIds = new List<int>(scripts.Keys);
        for (var i = scriptIds.Count - 1; i >= 0; i--)
        {
          var script = scripts[scriptIds[i]];
          script.Tick();
        }
      }
    }

    public class ScriptBase
    {
      public int _Id, _OwnerId;
      string _codeRaw;
      bool _isEnabled;
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
      public void Tick()
      {

        //Debug.Log($"Attempting to tick script: {_attachedEntity._EntityTypeData.Name}");

        // Check can tick
        if (!_isEnabled || !_isValid) return;
        if (!_attachedEntity._CanTick) return;

        var currentTick = GameController.s_CurrentTick;
        if (_lastTick == currentTick)
        {
          Debug.LogWarning($"Attempting to tick [{_attachedEntity._EntityTypeData.Name}] more than 1nce per tick");
          return;
        }
        _lastTick = currentTick;

        //Debug.Log($"Ticking script [{_Id}] for entity [{_attachedEntity._EntityData.Id}]");

        var breakLoop = false;
        var tokensAlloted = 10;
        var tickCooldown = 1;
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
          var line = _lines[_lineIndex++].Trim();
          var lineOriginal = line;

          // Check external return statement
          if (_externalReturnData != null)
          {
            //Debug.Log($"External return data found.. replacing: {_externalReturnData} [{_externalReturnStatement}] ........... {line}");

            // Check error
            if (_ExternalReturnData.StartsWith("E! "))
            {
              var error = _ExternalReturnData[3..];
              logError(error);
              break;
            }

            // Substitute return data into statement
            if (_externalReturnStatement != null)
            {
              int statementPos = _externalLine.IndexOf(_externalReturnStatement);
              line = _externalLine.Remove(statementPos, _externalReturnStatement.Length).Insert(statementPos, _externalReturnData);
            }

            // Clear external return data
            _externalReturnData = _externalReturnStatement = _externalLine = null;
          }

          // Log error
          void logError(string error)
          {

            // Log
            var errorString = $"Line [{_lineIndex}]: [{lineOriginal}] [{error}]";
            Debug.LogError(errorString);
            _attachedEntity.AppendLog($"<color=red>{errorString}</color>");

            // Exit loop and remove script
            breakLoop = true;
            ScriptManager.RemoveScript(this, $"E! {error}");
          }

          // Check blank line
          if (line.Length == 0) continue;
          Debug.Log("Read line: " + line);

          // Check comment
          if (line.StartsWith(@"//") || line.StartsWith("#") || line.StartsWith("$"))
          {
            continue;
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
              _logicDepth--;

            _lineDepth--;
            continue;
          }

          else if (line == "else")
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
          if (line.StartsWith("var ") && line.Contains("="))
          {
            var lineSplit = line.Split("=");
            if (lineSplit.Length > 1)
            {
              var variable = lineSplit[0][4..].Trim();
              if (_variables.ContainsKey(variable))
              {
                logError($"Variable '{variable}' already defined");
                return;
              }

              var value = HandleStatement(lineSplit[1].Trim(), true);
              if (breakLoop) break;
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
          if (!line.Contains("==") && line.Contains("="))
          {
            var lineSplit = line.Split("=");
            if (lineSplit.Length > 1)
            {
              var variable = lineSplit[0].Trim();
              if (!_variables.ContainsKey(variable))
              {
                logError($"Variable '{variable}' not defined");
                return;

              }

              var value = HandleStatement(lineSplit[1].Trim(), true);
              if (breakLoop) break;
              _variables[variable] = value;
              continue;
            }
            else
            {
              logError($"Invalid variable assignment syntax");
              return;
            }
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

          // Check for logic
          if (line.StartsWith("if"))
          {

            var lineLogic = line[2..].Trim();

            // Handle one logical expression
            bool GetLogic(string val0, string val1, string operator_)
            {

              if (!new string[] { "==", "!=", "<", ">", ">=", "<=" }.Contains(operator_))
              {
                logError($"Invalid conditional operator");
                return false;
              }
              Debug.Log($"Checking logic: {val0} {operator_} {val1}");

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
              if (breakLoop) break;

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

            if (breakLoop) break;

            // If true, go inside of if statement +1 depth
            _lineDepth++;
            if (masterLogic)
              _logicDepth++;
            continue;
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
                  currentWord += letter;

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
                    currentWord = CheckSubstitueVariable(currentWord);
                    if (currentWord.Contains('.') || currentWord.Contains(':'))
                    {
                      statement = statement.Insert(i, currentWord);
                      i--;
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
            ScriptEntity currentTarget = null;
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

                    // Return self
                    // if (word == "this")
                    // {
                    //   currentTarget = _attachedEntity;
                    //   currentTargetDepth = i;

                    //   if (parameterCheck)
                    //     returnStatement = GetEntityStatement(currentTarget);
                    //   break;
                    // }

                    // Check number
                    if (int.TryParse(word, out _))
                    {
                      if (parameterCheck)
                        returnStatement = word;
                      continue;
                    }

                    // Check entity variable
                    if (IsValidVariableEntity(word))
                    {
                      currentTarget = GetEntityByStatement(word);
                      currentTargetDepth = i;

                      if (parameterCheck)
                        returnStatement = word;
                      continue;
                    }

                    // Check all valid first accessors
                    if (new string[] { "_" }.Contains(word))
                    {
                      continue;
                    }

                    logError($"Null object reference ({word})");
                    break;
                  }

                  // Check valid accessor based on last accessor
                  string[] validAccessors = null;
                  if (IsValidVariableEntity(returnStatement))
                  {
                    var entityGot = GetEntityByStatement(returnStatement);
                    if (entityGot.HasEntityVariable_Int(word))
                    {
                      returnStatement = $"{entityGot.GetEntityVariable_Int(word)}";
                      validAccessors = new string[] { word };
                    }
                  }
                  // else
                  //   switch (accessorLastSave)
                  //   {

                  //     case "script":

                  //       validAccessors = new string[] { "Entity" };
                  //       break;

                  //   }

                  // Validate
                  if (!(validAccessors?.Contains(word) ?? false))
                  {
                    logError($"Null object reference ({accessorLastSave} => {word})");
                    break;
                  }

                  // Handle
                  // switch (word)
                  // {

                  //   case "Entity":

                  //     currentTarget = _attachedEntity;
                  //     currentTargetDepth = i;

                  //     if (parameterCheck)
                  //       returnStatement = GetEntityStatement(currentTarget);
                  //     break;

                  // }

                  break;

                // Function
                case 1:

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
                            statementSplit[y] = EvaluateParameter(statementSplit[y].Trim());
                          functionParameters[u] = ((int)string.Join(arithmeticOp, statementSplit).Eval()) + "";
                        }

                      // No arthimetic
                      if (!evaluated)
                        functionParameters[u] = EvaluateParameter(functionParameters[u]);
                    }
                  }

                  if (breakLoop) break;

                  // Validate function exists
                  var isValidFunction = false;
                  var isEntityFunction = false;
                  var isSystemFunction = false;

                  // Entity function
                  if (currentTarget != null)
                  {
                    isEntityFunction = isValidFunction = ScriptEntity.ScriptEntityHelper.HasFunction(currentTarget, functionName/*, currentTarget._EntityData.Id == _attachedEntity._EntityData.Id*/);
                  }

                  // System function
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
                      logError($"Referencing non-existant function {currentTarget._EntityTypeData.Name}:{functionName})");
                    else
                      logError($"Null-reference exception NULL:{functionName})");
                    break;
                  }

                  // Validate function # parameters (entity only)
                  if (isEntityFunction)
                  {
                    var numValidParameters = ScriptEntity.ScriptEntityHelper.GetFunctionParameterCount(functionName);

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
                    var validFacing = false;
                    switch (direction)
                    {
                      case 0:
                        validFacing = currentTarget._TilePosition.Equals((_attachedEntity._TilePosition.x, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z + 1));
                        break;
                      case 1:
                        validFacing = currentTarget._TilePosition.Equals((_attachedEntity._TilePosition.x, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z - 1));
                        break;
                      case 2:
                        validFacing = currentTarget._TilePosition.Equals((_attachedEntity._TilePosition.x + 1, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z));
                        break;
                      case 3:
                        validFacing = currentTarget._TilePosition.Equals((_attachedEntity._TilePosition.x - 1, _attachedEntity._TilePosition.y, _attachedEntity._TilePosition.z));
                        break;
                    }
                    if (!validFacing)
                    {
                      logError($"Not facing target");
                      break;
                    }

                    // Fire function
                    Debug.Log($"Firing entity function: {currentTarget._EntityTypeData.Name}:{functionName}");

                    var entityScript = currentTarget.LoadAndAttachScript(new ScriptLoadData()
                    {
                      PathTo = $"{currentTarget._EntityTypeData.Name.ToLower()}.{functionName}",
                      IsServerScript = true
                    });
                    entityScript._parentScript = this;
                    entityScript._variables.Add("_entity", $"_:get({_attachedEntity._EntityData.Id})");

                    // Check for variable
                    if (parameterCheck)
                    {
                      _externalReturnStatement = statement;
                      _externalLine = line;
                    }

                    // If looking for return statement, keep line index the same to re-fire function until it returns data
                    if (parameterCheck)
                      _lineIndex--;

                    // Wait for script to complete
                    breakLoop = true;
                    _isEnabled = false;

                    // Tick script now!
                    entityScript.Tick();

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
                    tickCooldown = systemTickCooldown > -1 ? systemTickCooldown : 0;

                    // Check script removed
                    if (!s_Singleton._scripts.ContainsKey(_Id))
                    {
                      breakLoop = true;
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
                      breakLoop = true;
                      break;
                    }
                  }

                  // Unity functions
                  else
                    switch (functionName)
                    {

                      case "GetById":

                        if (!serverAuthenticated)
                        {
                          logError("Not authenticated!");
                          break;
                        }

                        var useId = int.Parse(functionParameters[0]);
                        if (parameterCheck)
                          returnStatement = $"$Entity[{useId}]";
                        else
                        {
                          currentTarget = ScriptEntity.GetEntity(useId);
                          currentTargetDepth = i;
                        }

                        break;

                      // Editor; place
                      case "Place":

                        if (!serverAuthenticated)
                        {
                          logError("Not authenticated!");
                          break;
                        }

                        var entityType = int.Parse(functionParameters[0]);
                        var entityPosition = new Vector3(int.Parse(functionParameters[1]), int.Parse(functionParameters[3]), int.Parse(functionParameters[2])); // xzy ... swapped from usual xyz

                        new ScriptEntity(entityType, entityPosition, -1);
                        break;
                    }


                  break;
              }

              if (breakLoop) break;
            }

            return returnStatement;
          }
          HandleStatement(line, false);

          //
          if (breakLoop) break;
        }

        // Apply tick cooldown
        _attachedEntity._TickCooldown = Mathf.Clamp(_attachedEntity._TickCooldown, tickCooldown, 100);
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
          if (accessor == "")
          {

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
          }

          else if (accessor == "_")
          {
            // Validate parameters
            if (parameters.Length != 1)
            {
              return SystemFunctionReturnData.InvalidParameters(1);
            }

            // Get entity by id
            var entityId = parameters[0];
            var entity = ScriptEntity.GetEntity(int.Parse(entityId));
            if (entity == null)
            {
              return SystemFunctionReturnData.NullReference();
            }

            // Return found entity
            else
            {
              return SystemFunctionReturnData.Success(GetEntityStatement(entity), 0);
            }
          }

          // Invalid accessor
          else
          {
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