using System.Collections.Generic;
using UnityEngine;

using CustomUI;
using System.Linq;

namespace SimpleScript
{

  // A basic, selectable entity
  public class ScriptEntity
  {

    // Tick rates
    const int _TICKS_PER_MOVEMENT = 8,
    _TICKS_PER_MOVEMENT_RUN = 4;

    //
    public static Dictionary<int, ScriptEntity> s_ScriptEntities;
    public static Dictionary<(int, int, int), List<ScriptEntity>> s_ScriptEntitiesMapped;

    // Holds saveable entity data
    public EntityData _EntityData;
    public EntityTypeData _EntityTypeData { get { return ScriptEntityHelper.GetEntityTypeData(this); } }
    public int _EntityType { get { return _EntityTypeData.Id; } }
    public (int x, int y, int z) _TilePosition { get { return (_EntityData.X, _EntityData.Y, _EntityData.Z); } }
    public int _Direction { get { return _EntityData.Direction; } }
    public List<Item> _Storage { get { return _EntityData.ItemStorage; } }
    public bool _HasStorage { get { return (_Storage?.Count ?? 0) > 0; } }
    bool _spawned;

    Transform transform;

    // Handles tick-updating of entity
    Queue<EntityCommand> _entityCommandQueue;
    bool _hasEntityCommands { get { return (_entityCommandQueue?.Count ?? 0) > 0; } }
    int _tickCooldown;
    public int _TickCooldown { get { return _tickCooldown; } set { _tickCooldown = value; } }
    public bool _CanTick { get { return _tickCooldown == 0 && !_hasEntityCommands; } }

    // Holds local entity variables; ex: health
    Dictionary<string, int> _entityVariableMappings;

    //
    bool _isSolid { get { return _EntityTypeData.Solid; } }

    // Holds entity's commands in queue; can only do 1 action per tick
    public enum EntityCommand
    {
      None,

      MoveUp,
      MoveDown,
      MoveLeft,
      MoveRight,
    }

    // Initialize a script entity using a type, position, and ownerId
    static int s_ScriptEntityId;
    public ScriptEntity(int entityDataType, Vector3 position, int ownerId)
    {

      // Create network data
      _EntityData = new EntityData
      {
        Id = s_ScriptEntityId++,
        TypeId = entityDataType,

        OwnerId = ownerId,

        //ItemStorage = new(),

        EntityVariables_Int = new(),

        X = (int)position.x,
        Y = (int)position.y,
        Z = (int)position.z
      };
      AddScriptEntity(this);

      // Create entity variables per type
      _EntityData.EntityVariables_Int.Add(new ScriptManager.EntityVariable_Int()
      {
        Name = "x",
        Value = _EntityData.X
      });
      _EntityData.EntityVariables_Int.Add(new ScriptManager.EntityVariable_Int()
      {
        Name = "z",
        Value = _EntityData.Z
      });
      _EntityData.EntityVariables_Int.Add(new ScriptManager.EntityVariable_Int()
      {
        Name = "y",
        Value = _EntityData.Y
      });
      switch (_EntityTypeData.Name)
      {
        case "Character":
          _EntityData.EntityVariables_Int.Add(new ScriptManager.EntityVariable_Int()
          {
            Name = "Health",
            Value = 10
          });
          break;
      }
      InitializeEntityVariables();

      // Load model from entity type
      LoadModel();

      // Set position
      TryMove(_TilePosition, false);
      TrySetDirection(_Direction);
    }

    // Initialize a script entity using loaded entity data
    public ScriptEntity(EntityData entityData)
    {
      // Create network data
      _EntityData = entityData;
      if (_EntityData.Id >= s_ScriptEntityId)
        s_ScriptEntityId = _EntityData.Id + 1;
      AddScriptEntity(this);
      InitializeEntityVariables();

      // Load model from entity type
      LoadModel();

      // Set position
      TryMove(_TilePosition, false);
      TrySetDirection(_Direction);
    }

    // Remove entity
    public void Destroy()
    {
      //RemoveScriptEntity(this);
      Debug.Log("Not implemented: ScriptEntity.Destroy()");
    }
    public static void DestroyEntity(ScriptEntity entity)
    {
      entity.Destroy();
    }

    // Get entity by id or position
    public static ScriptEntity GetEntity(int id)
    {
      return s_ScriptEntities.ContainsKey(id) ? s_ScriptEntities[id] : null;
    }
    public static ScriptEntity GetEntity((int x, int y, int z) pos, int zIndex = 0)
    {
      return TileHasEntities(pos) ? GetTileEntities(pos)[zIndex] : null;
    }
    public static ScriptEntity GetEntityByType((int x, int y, int z) pos, int entityType)
    {
      var numEntities = GetTileEntityCount(pos);
      if (numEntities == 0)
        return null;

      foreach (var entity in GetTileEntities(pos))
      {
        if (entity._EntityData.TypeId == entityType)
          return entity;
      }

      return null;
    }
    public static ScriptEntity GetEntityAny()
    {
      var keys = new List<int>(s_ScriptEntities.Keys);
      if (keys.Count == 0) return null;
      return s_ScriptEntities[keys[0]];
    }

    //
    static List<ScriptEntity> GetTileEntities((int, int, int) pos)
    {
      return s_ScriptEntitiesMapped[pos];
    }

    //
    static bool TileHasEntities((int, int, int) pos)
    {
      return s_ScriptEntitiesMapped.ContainsKey(pos);
    }
    static int GetTileEntityCount((int, int, int) pos)
    {
      return !TileHasEntities(pos) ? 0 : s_ScriptEntitiesMapped[pos].Count;
    }
    static bool TileHasSolidEntity((int, int, int) pos)
    {
      if (!TileHasEntities(pos))
        return false;

      foreach (var entity in GetTileEntities(pos))
        if (entity._isSolid) return true;
      return false;
    }

    // Map an entity by position
    public static void AddScriptEntity(ScriptEntity entity)
    {
      s_ScriptEntities.Add(entity._EntityData.Id, entity);

      var pos = entity._TilePosition;
      if (TileHasEntities(pos))
        GetTileEntities(pos).Add(entity);
      else
        s_ScriptEntitiesMapped.Add(pos, new() { entity });

      entity._spawned = true;
    }
    public static void RemoveScriptEntity(ScriptEntity entity)
    {
      s_ScriptEntities.Remove(entity._EntityData.Id);

      var pos = entity._TilePosition;
      if (GetTileEntityCount(pos) > 1)
        GetTileEntities(pos).Remove(entity);
      else
        s_ScriptEntitiesMapped.Remove(pos);

      entity._spawned = false;
    }

    // Update all entities
    public static void UpdateScriptEntities()
    {
      var keys = new List<int>(s_ScriptEntities.Keys);
      foreach (var key in keys)
      {
        if (!s_ScriptEntities.ContainsKey(key))
        {
          Debug.LogWarning($"Trying to update non-existant entity[{key}]!");
          continue;
        }

        var entity = s_ScriptEntities[key];
        entity.Update();
      }
    }

    // Tick update all entities
    public static void TickScriptEntities()
    {
      var keys = new List<int>(s_ScriptEntities.Keys);
      foreach (var key in keys)
      {
        if (!s_ScriptEntities.ContainsKey(key))
        {
          Debug.LogWarning($"Trying to tick non-existant entity[{key}]!");
          continue;
        }

        var entity = s_ScriptEntities[key];
        entity.Tick();
      }
    }

    // Load model from entity type
    Transform _billboard;
    void LoadModel()
    {
      var modelName = _EntityTypeData.Name;

      var displayModel = new GameObject
      {
        name = modelName
      };
      transform = displayModel.transform;

      _billboard = new GameObject().transform;
      var sprite = new GameObject();
      sprite.transform.parent = _billboard;

      var spriteRenderer = sprite.AddComponent<SpriteRenderer>();
      spriteRenderer.sprite = Resources.Load<Sprite>($"Images/Entities/{modelName}");

      // Set initial position
      transform.position = new Vector3(_EntityData.X, _EntityData.Y, _EntityData.Z);
    }

    //
    public class Animation
    {

      ScriptEntity _entity;
      Transform _billboard { get { return _entity._billboard; } }

      public enum AnimationType
      {
        None,

        Move,
        Attack,
        Jump,

        Shake,
      }
      AnimationType _animationType;

      //
      float _animationTime, _animationDuration;
      Vector3 _animationStartPos;

      public Animation(ScriptEntity entity, AnimationType animationType, float duration)
      {
        _entity = entity;
        _animationType = animationType;

        _animationStartPos = _billboard.position;

        _animationDuration = duration;
        _animationTime = 0f;
      }

      //
      public void Update()
      {

        // Check if animation finished
        if (_animationTime > _animationDuration)
        {
          _entity._currentAnimation = null;
          return;
        }
        _animationTime += Time.deltaTime;
        var isAnimationComplete = _animationTime > _animationDuration;

        var animationTimeNormalized = _animationTime / _animationDuration;
        var endPos = _entity.transform.position;

        // Apply animation effect based on type
        if (isAnimationComplete)
          switch (_animationType)
          {
            case AnimationType.Move:
            case AnimationType.Jump:
              _billboard.position = endPos;
              break;

            case AnimationType.Attack:
            case AnimationType.Shake:
              var sprite = _billboard.GetChild(0);
              sprite.localPosition = Vector3.zero;
              break;
          }
        else
          switch (_animationType)
          {
            case AnimationType.Move:
              _billboard.position = Vector3.Lerp(_animationStartPos, endPos, animationTimeNormalized);
              break;
            case AnimationType.Jump:
              var position = Vector3.Lerp(_animationStartPos, endPos, animationTimeNormalized);
              var jumpHeight = 0.5f;
              position.y += Mathf.Sin(animationTimeNormalized * Mathf.PI) * jumpHeight;
              _billboard.position = position;
              break;
            case AnimationType.Attack:
              var sprite = _billboard.GetChild(0);
              // Use sin to make sprite move halfway into tile in front then back
              _animationStartPos = Vector3.zero;
              endPos = sprite.localPosition + sprite.transform.forward * 0.5f;
              position = Vector3.Lerp(_animationStartPos, endPos, Mathf.Sin(animationTimeNormalized * Mathf.PI));
              sprite.localPosition = position;
              break;

            case AnimationType.Shake:
              sprite = _billboard.GetChild(0);
              var shakeIntensity = 1f;
              var shakeDisplacement = Random.insideUnitSphere * shakeIntensity * 0.1f;
              sprite.localPosition = shakeDisplacement;
              break;
          }
      }

    }
    Animation _currentAnimation;
    Animation.AnimationType _animationOverride;

    void Update()
    {
      var lookAt = Quaternion.LookRotation(GameResources._MainCamera.transform.position - _billboard.position);
      _billboard.rotation = lookAt;
      _billboard.localRotation = Quaternion.Euler(_billboard.localRotation.eulerAngles.x, GameResources._MainCamera.transform.localRotation.eulerAngles.y + 180f, _billboard.localRotation.eulerAngles.z);

      // Play animation if exists
      if (_currentAnimation != null)
      {
        _currentAnimation.Update();
      }
    }

    //
    void SetAnimation(Animation.AnimationType animationType, float duration)
    {
      if (_currentAnimation != null)
      {
        Debug.LogWarning($"Trying to set animation [{animationType}] while already playing animation on entity[{_EntityData.Id}]!");
        return;
      }
      if (_animationOverride != Animation.AnimationType.None)
      {
        Debug.Log($"Animation [{animationType}] overridden by [{_animationOverride}] on entity[{_EntityData.Id}]!");
        animationType = _animationOverride;
      }
      _currentAnimation = new Animation(this, animationType, duration);
    }
    public void SetAnimationOverride(Animation.AnimationType animationType)
    {
      _animationOverride = animationType;
    }

    //
    public void Shake(float time)
    {
      SetAnimation(Animation.AnimationType.Shake, time);
    }

    // Increment any scripts on the entity per tick
    void Tick()
    {
      if (_tickCooldown > 0)
        _tickCooldown--;
      else
      {
        if (_hasEntityCommands && _tickCooldown == 0)
        {

          // Execute next command
          var commandNext = _entityCommandQueue.Dequeue();
          HandleCommand(commandNext);
        }
      }
    }

    //
    void InitializeEntityVariables()
    {
      // Map entity variable indexes
      _entityVariableMappings = new Dictionary<string, int>();
      for (var i = 0; i < _EntityData.EntityVariables_Int.Count; i++)
        _entityVariableMappings.Add(_EntityData.EntityVariables_Int[i].Name, i);
    }

    void OnDestroy()
    {
      if (_spawned)
        RemoveScriptEntity(this);
    }

    // Saveable data for the entity
    [System.Serializable]
    public struct EntityData
    {

      // Unique Id of entity
      public int Id;

      // Identifier for what type of entity this is (ex: 0 = wood, 1 = stone, etc.)
      public int TypeId;

      // Owner of entity; -1 = server
      public int OwnerId;

      // X, Y, and Z position of the entity
      public int X, Y, Z;

      // Entity direction
      public int Direction;

      // Item storage
      public List<Item> ItemStorage;

      // Logger
      public List<string> Log;

      // Holds entity's variables that can be referenced in scripts
      public List<ScriptManager.EntityVariable_Int> EntityVariables_Int;
    }

    //
    [System.Serializable]
    public struct EntityTypeData
    {

      public int Id;

      // Name of type; ex: Wood
      public string Name;

      // Flavor text for user
      public string Description;

      // Whether other entities can share the tile; ex: ground items
      public bool Solid;

      // 'Radius' of the object; default 1, adding 1 will expand the outer ring of tiles
      public int Size;

      // Public functions are how entities can interact with other entities
      [System.NonSerialized]
      public int[] PublicFunctionIds;
    }

    public static class ScriptEntityHelper
    {

      static List<EntityTypeData> s_entityTypeData
      {
        get
        {
          return s_entityDataWrapper._EntityTypeData;
        }
      }
      [System.Serializable]
      struct EntityTypeDataWrapper
      {
        public List<EntityTypeData> _EntityTypeData;
      }
      static EntityTypeDataWrapper s_entityDataWrapper;


      //
      public static FunctionRepository s_FunctionRepository;

      //
      public static void Init()
      {
        s_ScriptEntities = new();
        s_ScriptEntitiesMapped = new();

        s_FunctionRepository = new();

        LoadTypeData();
      }

      // Load objects from json
      public static void LoadTypeData()
      {
        var functionsByType = s_FunctionRepository.Load(true);

        // Load in entity type data
        var rawText = System.IO.File.ReadAllText("entityTypeData.json");
        var jsonData = JsonUtility.FromJson<EntityTypeDataWrapper>(rawText);
        s_entityDataWrapper = jsonData;

        // Match functions per entity type
        for (var i = 0; i < s_entityTypeData.Count; i++)
        {
          var entityTypeData = s_entityTypeData[i];
          var functionIds = new List<int>();
          if (functionsByType.ContainsKey(entityTypeData.Name.ToLower()))
            foreach (var func in functionsByType[entityTypeData.Name.ToLower()])
            {
              var id = s_FunctionRepository.GetFunctionId(func);
              functionIds.Add(id);
            }
          var publicFunctionIds = functionIds.ToArray();
          entityTypeData.PublicFunctionIds = publicFunctionIds;
          s_entityTypeData[i] = entityTypeData;
        }
      }

      // Save objects to json
      public static void SaveTypeData()
      {
        var jsonData = JsonUtility.ToJson(s_entityDataWrapper, true);
        System.IO.File.WriteAllText("entityTypeData.json", jsonData);
      }

      //
      public static EntityTypeData GetEntityTypeData(int id)
      {
        return s_entityTypeData[id];
      }
      public static EntityTypeData GetEntityTypeData(ScriptEntity entity)
      {
        return GetEntityTypeData(entity._EntityData.TypeId);
      }

      //
      public static bool HasFunction(ScriptEntity entity, string functionName)
      {
        var functionId = s_FunctionRepository.GetFunctionId(functionName);
        var typeData = entity._EntityTypeData;
        var functions = typeData.PublicFunctionIds;
        return functions.Contains(functionId);
      }
    }

    //
    public bool HasEntityVariable_Int(string variableName)
    {
      return _entityVariableMappings.ContainsKey(variableName);
    }
    public int GetEntityVariable_Int(string variableName)
    {
      return _EntityData.EntityVariables_Int[_entityVariableMappings[variableName]].Value;
    }
    public void SetEntityVariable_Int(string variableName, int value)
    {
      _EntityData.EntityVariables_Int[_entityVariableMappings[variableName]].Value = value;
    }

    void HandleCommand(EntityCommand entityCommand)
    {

      int tickCooldown = 0;

      //
      var entityPosDelta = _TilePosition;
      var direction = -1;
      switch (entityCommand)
      {
        case EntityCommand.MoveUp:
          entityPosDelta.Item3 += 1;
          direction = 0;
          break;

        case EntityCommand.MoveDown:
          entityPosDelta.Item3 += -1;
          direction = 1;
          break;

        case EntityCommand.MoveLeft:
          entityPosDelta.Item1 += -1;
          direction = 2;
          break;

        case EntityCommand.MoveRight:
          entityPosDelta.Item1 += 1;
          direction = 3;
          break;
      }

      // Movement command
      if (entityPosDelta != _TilePosition)
      {
        TrySetDirection(direction);
        if (TryMove(entityPosDelta, true))
          tickCooldown = _TICKS_PER_MOVEMENT;
      }

      // Apply tick cooldown
      _tickCooldown = Mathf.Clamp(_tickCooldown, tickCooldown, 100);
    }

    public void ReceiveCommand(EntityCommand entityCommand, int ownerId)
    {

      // Authenticate
      if (ownerId != _EntityData.OwnerId)
      {
        Debug.LogError($"Non-authenticated command recieved from owner id [{ownerId}] to [{_EntityData.OwnerId}]: {entityCommand}");
        return;
      }

      // Queue command
      _entityCommandQueue ??= new();
      _entityCommandQueue.Enqueue(entityCommand);

      //Debug.Log($"Queued {entityCommand} [{_entityCommandQueue.Count}]");
    }

    public bool TryMove((int, int, int) toPos, bool solidCheck)
    {

      // Check valid position
      if (solidCheck && TileHasSolidEntity(toPos))
      {
        return false;
      }

      // Move position
      RemoveScriptEntity(this);

      _EntityData.X = toPos.Item1;
      _EntityData.Y = toPos.Item2;
      _EntityData.Z = toPos.Item3;

      AddScriptEntity(this);

      // Set entity variables for position
      SetEntityVariable_Int("x", _EntityData.X);
      SetEntityVariable_Int("y", _EntityData.Y);
      SetEntityVariable_Int("z", _EntityData.Z);

      // Set local model to network position
      transform.position = new Vector3(_EntityData.X, _EntityData.Y, _EntityData.Z);

      // Animate
      SetAnimation(Animation.AnimationType.Move, 1f);

      return true;
    }

    //
    public bool TrySetDirection(int direction)
    {
      _EntityData.Direction = direction;

      switch (direction)
      {
        case 0:
          transform.rotation = Quaternion.Euler(0, 0, 0);
          break;

        case 1:
          transform.rotation = Quaternion.Euler(0, 180, 0);
          break;

        case 2:
          transform.rotation = Quaternion.Euler(0, -90, 0);
          break;

        case 3:
          transform.rotation = Quaternion.Euler(0, 90, 0);
          break;
      }

      return true;
    }

    // Load and attach script to entity
    public ScriptManager.ScriptBase LoadAndAttachScript(ScriptManager.ScriptLoadData scriptLoadData)
    {
      var rawScript = (scriptLoadData.Headers != null ? scriptLoadData.Headers + "\n" : "") + (scriptLoadData.ScriptType switch
      {
        ScriptManager.ScriptType.ENTITY => ScriptManager.LoadSystemEntityScript(scriptLoadData.PathTo),
        ScriptManager.ScriptType.ITEM => ScriptManager.LoadSystemItemScript(scriptLoadData.PathTo),

        _ => ScriptManager.LoadPlayerScript(scriptLoadData.PathTo)
      });

      return ScriptManager.s_Singleton.AttachScriptTo(
        this,
        rawScript,
        _EntityData.OwnerId
      );
    }

    //
    public void AppendLog(string message)
    {
      var maxLogSize = 10;

      _EntityData.Log ??= new();
      var log = _EntityData.Log;

      // Append new message
      log.Add(message);

      // Remove oldest message if exceeds max log size
      if (log.Count > maxLogSize)
        log.RemoveAt(0);

      //
      StatusPanel.StatusPanelManager.UpdateStatusUI_S(this, StatusPanel.SubPanelType.Logger);
    }
    public string GetLogString()
    {
      if (_EntityData.Log == null)
        return "";
      return string.Join("\n", _EntityData.Log);
    }

    // Update menus associated with entities; ex: status panel
    public void UpdateUIs()
    {
      StatusPanel.StatusPanelManager.UpdateStatusUI_S(this, StatusPanel.SubPanelType.Inventory);
    }

  }
}