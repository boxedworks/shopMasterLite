using System.Collections.Generic;
using UnityEngine;

using System.IO;
using Assets.Scripts.Game.SimpleScript.Entities.Entity;

namespace Assets.Scripts.Game.SimpleScript.Scripting
{

  public class ScriptBaseController
  {

    public static ScriptBaseController s_Singleton;
    static int s_scriptId;

    Dictionary<int, ScriptBase> _scripts;

    public static void Initialize()
    {
      s_Singleton = new ScriptBaseController();

      new ScriptBaseHelper();
      new ScriptEntityHelper();
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
    public ScriptBase AttachScriptRawTo(ScriptEntity entity, string scriptRaw, int ownerId)
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

    public static ScriptBase GetScript(int id)
    {
      var scripts = s_Singleton._scripts;
      return scripts[id];
    }

    public static bool HasScript(int id)
    {
      var scripts = s_Singleton._scripts;
      return scripts != null && scripts.ContainsKey(id);
    }

    // Remove script
    static void RemoveScript(int scriptId, string returnData = null)
    {
      // Delete script
      var script = GetScript(scriptId);
      if (!script._IsValid)
      {
        Debug.LogWarning($"Trying to remove invalid script[{scriptId}]!");
        return;
      }
      script.OnRemoveScript();
      RemoveScript(scriptId);

      // Check for parent scripts
      var parentScript = script._ParentScript;
      if (parentScript != null)
      {
        parentScript._ExternalReturnData = returnData;
        parentScript.StopWaitingFor();
        var forceTick = returnData?.StartsWith("!E") ?? false;
        parentScript.Tick(forceTick);
      }
    }

    //
    public static void RemoveScript(ScriptBase script, string returnData = null)
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




  }

}