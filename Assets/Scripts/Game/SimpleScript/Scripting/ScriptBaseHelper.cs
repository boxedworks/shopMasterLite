
using System;
using System.Collections.Generic;
using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using Assets.Scripts.Game.SimpleScript.Entities.Item;
using Assets.Scripts.Game.UI;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript.Scripting
{

  public class ScriptBaseHelper
  {

    //
    public static ScriptBaseHelper s_Singleton;

    //
    Dictionary<string, SystemFunction> _systemFunctions;
    public static Dictionary<string, SystemFunction> s_SystemFunctions { get { return s_Singleton._systemFunctions; } }

    // Conditional operators
    string[] _conditionalOperators;
    public static string[] s_ConditionalOperators { get { return s_Singleton._conditionalOperators; } }

    //
    public ScriptBaseHelper()
    {
      s_Singleton = this;

      InitializeSystemFunctions();

      _conditionalOperators = new string[] { "==", "!=", "<", ">", ">=", "<=" };
    }

    public void RegisterSystemFunction(string name, Func<ScriptBase, string, string[], SystemFunctionReturnData> function)
    {
      _systemFunctions.Add(name, new SystemFunction()
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
          var returnData = parameters.Length == 1 ? parameters[0] : "null";
          //Debug.Log($"exit() called with return data: {returnData}");

          // Remove script
          ScriptBaseController.RemoveScript(script, returnData);

          return SystemFunctionReturnData.Success(-1);
        }
      );

      // Destroy
      RegisterSystemFunction(
        "destroy",
        (ScriptBase script, string accessor, string[] parameters) =>
        {

          // Validate accessor
          if (accessor != "")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length > 0)
          {
            return SystemFunctionReturnData.InvalidParameters(0);
          }

          // Destroy entity
          script.RemoveScriptOnExit();
          script._AttachedEntity.TryMove((-100, 0, 0), false, false);

          return SystemFunctionReturnData.Success(0);
        }
      );

      // Move
      RegisterSystemFunction(
        "move",
        (ScriptBase script, string accessor, string[] parameters) =>
        {

          // Validate accessor
          if (ScriptEntityHelper.IsValidVariableEntity(accessor))
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
                ScriptEntity entity = ScriptEntityHelper.GetEntityByIdOrStatement(entityData);
                if (entity == null)
                {
                  return SystemFunctionReturnData.NullReference();
                }

                // Move entity to position
                var position_x = parameters[1];
                var position_y = parameters[2];
                var position_z = parameters[3];
                var solidCheck = true;
                var animate = true;

                var success = entity.TryMove(
                  (int.Parse(position_x), int.Parse(position_y), int.Parse(position_z)),
                  solidCheck,
                  animate
                );

                if (!success)
                {
                  return SystemFunctionReturnData.Custom("Failed to move entity; target position may be blocked");
                }

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
                return SystemFunctionReturnData.Success("null", 0);
              }

              // Return found entity
              else
              {
                return SystemFunctionReturnData.Success(ScriptEntityHelper.GetEntityStatement(entity), 0);
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
              var items = script._AttachedEntity._EntityData.ItemStorage.Items;
              ScriptItem item = null;
              if (items != null && itemSlot < items.Count)
              {
                item = ScriptItemController.GetItem(items[itemSlot].Id);
              }
              if (item == null)
              {
                return SystemFunctionReturnData.Custom($"Item not found at index: {itemSlot}");
              }

              // Return found entity
              else
              {
                return SystemFunctionReturnData.Success(ScriptEntityHelper.GetItemStatement(item), 0);
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
          if (!int.TryParse(parameters[0], out var ticks))
          {
            return SystemFunctionReturnData.InvalidParameterType(0, "int");
          }
          if (ticks < 1)
          {
            return SystemFunctionReturnData.Custom("Tick cooldown must be at least 1");
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
          var entity = ScriptEntityHelper.GetEntityByIdOrStatement(entityData);
          if (entity == null)
          {
            return SystemFunctionReturnData.NullReference();
          }

          var itemId = int.Parse(parameters[1]);

          // Give item
          var item = ScriptItemController.GiveItem(entity, itemId);
          if (item == null)
          {
            return SystemFunctionReturnData.Custom("Inventory full");
          }

          // Create visual indicator for the given item
          ScriptItemController.s_ItemVisualIndicatorManager.CreateIndicator(entity, item._ItemTypeData, script._AttachedEntity._TilePositionVector3);

          //
          Terminal.s_Singleton.LogMessage($"Gave item ID {item._ItemTypeData.Name} to entity ID {entityData}");
          return SystemFunctionReturnData.Success(0);
        }
      );

      // Spawn
      RegisterSystemFunction(
        "spawn",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate accessor
          if (accessor != "")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 0)
          {
            return SystemFunctionReturnData.InvalidParameters(0);
          }

          // Teleport entity to position
          var entity = script._AttachedEntity;
          if (!entity.TryMove((0, 0, 0), true, false))
          {
            entity.Destroy();
            Terminal.s_Singleton.LogMessage($"Failed to spawn entity because spawn location is occupied");
            return SystemFunctionReturnData.Custom("Error spawning entity");
          }

          entity._ScriptSpawned = true;

          //
          return SystemFunctionReturnData.Success();
        }
      );

      // Set sprite
      RegisterSystemFunction(
        "setSprite",
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
          var entity = ScriptEntityHelper.GetEntityByIdOrStatement(entityData);
          if (entity == null)
          {
            return SystemFunctionReturnData.NullReference();
          }

          // Get sprite path
          var spritePath = ScriptEntityHelper.GetStringVariable(parameters[1]);

          // Set sprite
          var hasError = !entity.SetSprite(spritePath);
          if (hasError)
          {
            return SystemFunctionReturnData.Custom("Error setting sprite; check console for details");
          }

          //
          return SystemFunctionReturnData.Success(0);
        }
      );

      // Shake entity
      RegisterSystemFunction(
        "animate",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate accessor
          if (accessor != "_")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 3)
          {
            return SystemFunctionReturnData.InvalidParameters(3);
          }

          // Get entity by id
          var entityData = parameters[0];
          var entity = ScriptEntityHelper.GetEntityByIdOrStatement(entityData);
          if (entity == null)
          {
            return SystemFunctionReturnData.NullReference();
          }

          // Get animation params
          var animation = Enum.Parse<ScriptEntityAnimation.AnimationType>(ScriptEntityHelper.GetStringVariable(parameters[1]));
          var animationTime = float.Parse(parameters[2]);

          // Animate
          entity.Animate(animation, animationTime);

          //
          return SystemFunctionReturnData.Success(0);
        }
      );

      // Animate
      RegisterSystemFunction(
        "animateOverride",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate accessor
          if (accessor != "_")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 3)
          {
            return SystemFunctionReturnData.InvalidParameters(3);
          }

          // Get entity by id
          var entityData = parameters[0];
          var entity = ScriptEntityHelper.GetEntityByIdOrStatement(entityData);
          if (entity == null)
          {
            return SystemFunctionReturnData.NullReference();
          }

          // Get animation params
          var animation = Enum.Parse<ScriptEntityAnimation.AnimationType>(ScriptEntityHelper.GetStringVariable(parameters[1]));
          var animationTime = float.Parse(parameters[2]);

          // Animate
          entity.SetAnimationOverride(animation, animationTime);

          //
          return SystemFunctionReturnData.Success(0);
        }
      );

      // Play sfx
      RegisterSystemFunction(
        "sfx",
        (ScriptBase script, string accessor, string[] parameters) =>
        {
          // Validate accessor
          if (accessor != "_")
          {
            return SystemFunctionReturnData.InvalidFunction();
          }

          // Validate parameters
          if (parameters.Length != 4)
          {
            return SystemFunctionReturnData.InvalidParameters(4);
          }

          // Get entity by id
          var entityData = parameters[0];
          var entity = ScriptEntityHelper.GetEntityByIdOrStatement(entityData);
          if (entity == null)
          {
            return SystemFunctionReturnData.NullReference();
          }

          // Get sfx params
          var sfxFolderName = Enum.Parse<SfxController.AudioObjectType>(ScriptEntityHelper.GetStringVariable(parameters[1]));
          var sfxName = -1;
          if (sfxFolderName == SfxController.AudioObjectType.Character)
            sfxName = (int)Enum.Parse<SfxController.CharacterSfx>(ScriptEntityHelper.GetStringVariable(parameters[2]));
          else if (sfxFolderName == SfxController.AudioObjectType.Rock)
            sfxName = (int)Enum.Parse<SfxController.RockSfx>(ScriptEntityHelper.GetStringVariable(parameters[2]));
          else if (sfxFolderName == SfxController.AudioObjectType.PlayerController)
            sfxName = (int)Enum.Parse<SfxController.PlayerControllerSfx>(ScriptEntityHelper.GetStringVariable(parameters[2]));
          if (sfxName == -1)
            return SystemFunctionReturnData.Custom("Sfx not found");

          var volume = float.Parse(parameters[3]);

          // Play sfx
          SfxController.PlaySfxAt(entity._TilePositionVector3, sfxFolderName, sfxName, volume);

          //
          return SystemFunctionReturnData.Success(0);
        }
      );
    }
  }

}