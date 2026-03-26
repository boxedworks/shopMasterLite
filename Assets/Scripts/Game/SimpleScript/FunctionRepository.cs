using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript
{

  public class FunctionRepository
  {

    // Holds all function data
    [System.Serializable]
    public class FunctionData
    {
      public int Id;

      public string Name;
      public int ParameterCount;

      public string Description;
    }
    Dictionary<string, FunctionData> _functionData;
    Dictionary<int, string> _functionMapping;
    Dictionary<string, List<string>> _functionsByType;

    //
    public FunctionRepository()
    {
    }

    public void Load(bool isEntityScripts)
    {
      _functionData = new();
      _functionMapping = new();

      // Load functions from disk
      _functionsByType = new();
      var scripts = isEntityScripts ? ScriptManager.GetSystemEntityScripts() : ScriptManager.GetSystemItemScripts();
      foreach (var script in scripts)
      {
        var fileName = script.Split(@"\")[^1];
        var scriptData = fileName.Split(".");

        var typeName = scriptData[0];
        var functionName = scriptData[1];

        // Load number of params
        var scriptRaw = isEntityScripts ? ScriptManager.LoadSystemEntityScript(fileName) : ScriptManager.LoadSystemItemScript(fileName);
        var numParams = 0;
        foreach (var line in scriptRaw.Split("\n"))
        {
          var lineUse = line.Trim();
          if (lineUse.StartsWith("$SetNumParams(") && lineUse.EndsWith(")"))
          {
            numParams = int.Parse(line.Split("$SetNumParams(")[^1][..^2]);
          }
        }

        // Add function data
        //Debug.Log($"Loaded script [Entity script = {isEntityScripts}] {fileName} with {numParams} params.. [{typeName}]");
        AddFunction(fileName, "System loaded...", numParams);
        if (!_functionsByType.ContainsKey(typeName))
          _functionsByType.Add(typeName, new());
        _functionsByType[typeName].Add(functionName);
      }
    }

    void AddFunction(string name, string description, int parameterCount)
    {
      var functionId = _functionData.Count;
      _functionMapping.Add(functionId, name);
      _functionData.Add(name, new FunctionData()
      {
        Id = functionId,

        Name = name,
        ParameterCount = parameterCount,

        Description = description,
      });
    }

    // Get all functions for a given type
    public List<string> GetFunctionsByType(string typeName)
    {
      if (_functionsByType.ContainsKey(typeName))
        return _functionsByType[typeName];
      return new List<string>();
    }

    //
    public int GetFunctionId(string functionName)
    {
      if (!_functionData.ContainsKey(functionName))
      {
        Debug.LogWarning($"No function found with name {functionName}");
        return -1;
      }
      return _functionData[functionName].Id;
    }
    public string GetFunctionName(int functionId)
    {
      return _functionMapping[functionId];
    }

    public FunctionData GetFunctionData(string functionName)
    {
      if (!_functionData.ContainsKey(functionName))
      {
        Debug.LogWarning($"No function found with name {functionName}");
        return null;
      }
      return _functionData[functionName];
    }
    public FunctionData GetFunctionData(int functionId)
    {
      return GetFunctionData(GetFunctionName(functionId));
    }

    public int GetFunctionParameterCount(string functionName)
    {
      return GetFunctionData(functionName).ParameterCount;
    }
  }

}