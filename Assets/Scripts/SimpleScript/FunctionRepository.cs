using System.Collections.Generic;
using UnityEngine;

namespace SimpleScript
{

  public class FunctionRepository
  {

    // Holds all function data
    [System.Serializable]
    public struct FunctionData
    {
      public int Id;

      public string Name;
      public int ParameterCount;

      public string Description;
    }
    Dictionary<string, FunctionData> _functionData;
    Dictionary<int, string> _functionMapping;

    //
    public FunctionRepository()
    {
    }

    public Dictionary<string, List<string>> Load(bool isEntityScripts)
    {
      _functionData = new();
      _functionMapping = new();

      // Load functions from disk
      Dictionary<string, List<string>> functionsByType = new();
      var scripts = isEntityScripts ? ScriptManager.GetSystemEntityScripts() : ScriptManager.GetSystemItemScripts();
      foreach (var script in scripts)
      {
        var fileName = script.Split(@"\")[^1];
        var scriptData = fileName.Split(".");

        var objectName = scriptData[0];
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
        Debug.Log($"Loaded script [Entity script = {isEntityScripts}] {fileName} with {numParams} params");
        AddFunction(functionName, "System loaded...", numParams);
        if (!functionsByType.ContainsKey(objectName))
          functionsByType.Add(objectName, new());
        functionsByType[objectName].Add(functionName);
      }

      return functionsByType;
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

    //
    public int GetFunctionId(string functionName)
    {
      return _functionData[functionName].Id;
    }
    public string GetFunctionName(int functionId)
    {
      return _functionMapping[functionId];
    }

    public FunctionData GetFunctionData(string functionName)
    {
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