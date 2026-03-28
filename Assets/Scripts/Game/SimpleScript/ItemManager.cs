using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript
{

  public class ItemManager
  {

    //
    static ItemManager s_singleton;

    //
    FunctionRepository _functionRepository;
    public static FunctionRepository s_FunctionRepository { get { return s_singleton._functionRepository; } }

    //
    [System.Serializable]
    struct ItemTypeDataWrapper
    {
      public List<Item.ItemTypeData> _ItemTypeData;
    }
    ItemTypeDataWrapper _itemDataWrapper;
    static List<Item.ItemTypeData> s_itemTypeData
    {
      get
      {
        return s_singleton._itemDataWrapper._ItemTypeData;
      }
    }

    //
    Dictionary<int, Item> _items;

    //
    public static void Initialize()
    {
      new ItemManager();
    }
    public ItemManager()
    {
      s_singleton = this;

      _functionRepository = new();

      // Load entity type data
      LoadTypeData();

      //
      _items = new();
    }

    //
    public static ItemVisualIndicatorManager s_ItemVisualIndicatorManager { get { return s_singleton._itemVisualIndicatorManager; } }
    ItemVisualIndicatorManager _itemVisualIndicatorManager = new();
    public static void Update()
    {
      s_singleton._itemVisualIndicatorManager.Update();
    }

    public class ItemVisualIndicatorManager
    {

      struct ItemVisualIndicator
      {
        public ScriptEntity _Entity;
        public Item.ItemTypeData _ItemType;
        public GameObject _IndicatorObject;
        public float _CreationTime;
        public Vector3 _SpawnPosition;
      }
      List<ItemVisualIndicator> _activeIndicators;

      public ItemVisualIndicatorManager()
      {
        _activeIndicators = new();
      }

      public void Update()
      {
        for (var i = _activeIndicators.Count - 1; i >= 0; i--)
        {
          var indicator = _activeIndicators[i];
          var elapsedTime = Time.time - indicator._CreationTime;
          var duration = 0.5f; // Duration for the indicator to move from spawn position to entity position
          if (elapsedTime > duration)
          {
            // Destroy visual indicator object
            if (indicator._IndicatorObject != null)
              Object.Destroy(indicator._IndicatorObject);

            // Remove from active indicators list
            _activeIndicators.RemoveAt(i);
          }
          else
          {
            // Update visual indicator position or other properties if needed
            if (indicator._IndicatorObject != null)
            {
              var indicatorTransform = indicator._IndicatorObject.transform;
              var lookAt = Quaternion.LookRotation(GameResources._MainCamera.transform.position - indicatorTransform.position);
              indicatorTransform.rotation = lookAt;
              indicatorTransform.localRotation = Quaternion.Euler(indicatorTransform.localRotation.eulerAngles.x, GameResources._MainCamera.transform.localRotation.eulerAngles.y + 180f, indicatorTransform.localRotation.eulerAngles.z);

              indicator._IndicatorObject.transform.position = Vector3.Lerp(indicator._SpawnPosition, indicator._Entity._TilePositionVector3, elapsedTime / duration);

              var position = Vector3.Lerp(indicator._SpawnPosition, indicator._Entity._TilePositionVector3, elapsedTime / duration);
              var jumpHeight = 0.5f;
              position.y += Mathf.Sin(elapsedTime / duration * Mathf.PI) * jumpHeight;
              indicator._IndicatorObject.transform.position = position;
            }
          }
        }
      }

      public void CreateIndicator(ScriptEntity forEntity, Item.ItemTypeData itemType, Vector3 spawnPosition)
      {
        // Create visual indicator object and set properties based on item type
        var itemSprite = GameResources.LoadItemSprite($"{itemType.Name.ToLower()}");
        var indicatorObject = new GameObject($"ItemIndicator_{itemType.Name}");
        indicatorObject.transform.position = spawnPosition;
        var spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = itemSprite;

        var indicator = new ItemVisualIndicator
        {
          _Entity = forEntity,
          _ItemType = itemType,
          _CreationTime = Time.time,
          _SpawnPosition = spawnPosition,
          _IndicatorObject = indicatorObject
        };

        _activeIndicators.Add(indicator);
      }

    }

    // Get item by id
    public static Item GetItem(int id)
    {
      var items = s_singleton._items;
      return items.ContainsKey(id) ? items[id] : null;
    }

    //
    public static void AddItem(Item item)
    {
      var items = s_singleton._items;
      items[item._ItemData.Id] = item;
    }
    public static void RemoveItem(int id)
    {
      var items = s_singleton._items;
      if (items.ContainsKey(id))
        items.Remove(id);
    }

    // Give entity an item
    public static Item GiveItem(ScriptEntity entity, int itemTypeId)
    {
      var storage = entity._Storage;
      if (storage == null)
      {
        Debug.LogError("Entity has no item storage!");
        return null;
      }
      for (var i = 0; i < storage.Count; i++)
      {
        if (storage[i] != null)
          continue;

        var newItem = new Item(itemTypeId);
        storage[i] = newItem._ItemData;
        entity.OnItemGiven();

        return newItem;
      }

      Debug.LogWarning("No empty storage slot found for item!");
      return null;
    }

    //
    public static Item.ItemTypeData GetItemTypeData(int itemTypeId)
    {
      return s_singleton._itemDataWrapper._ItemTypeData[itemTypeId];
    }

    // Load objects from json
    public void LoadTypeData()
    {
      _functionRepository.Load(false);

      // Load in item type data
      var rawText = System.IO.File.ReadAllText("itemTypeData.json");
      var jsonData = JsonConvert.DeserializeObject<ItemTypeDataWrapper>(rawText);
      _itemDataWrapper = jsonData;

      // Match functions per item type
      for (var i = 0; i < s_itemTypeData.Count; i++)
      {
        var itemTypeData = s_itemTypeData[i];
        var functionsByType = ScriptEntityHelper.s_FunctionRepository.GetFunctionsByType(itemTypeData.Name.ToLower());
        var functionIds = new List<int>();
        foreach (var func in functionsByType)
        {
          var id = _functionRepository.GetFunctionId(func);
          functionIds.Add(id);
        }
        var publicFunctionIds = functionIds.ToArray();
        itemTypeData.PublicFunctionIds = publicFunctionIds;
        s_itemTypeData[i] = itemTypeData;
      }
    }

    // Save objects to json
    public void SaveTypeData()
    {
      var jsonData = JsonConvert.SerializeObject(_itemDataWrapper, Formatting.Indented, new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore
      });
      System.IO.File.WriteAllText("itemTypeData.json", jsonData);
    }

    //
    public static bool HasFunction(Item item, string functionName)
    {
      var itemKey = item._ItemTypeData.Name.ToLower();
      var functionId = s_FunctionRepository.GetFunctionId($"{itemKey}.{functionName}");
      var typeData = item._ItemTypeData;
      var functions = typeData.PublicFunctionIds;
      return functions.Contains(functionId);
    }

    //
    public static int GetFunctionParameterCount(Item item, string functionName)
    {
      var itemKey = item._ItemTypeData.Name.ToLower();
      var functionTypeData = s_FunctionRepository.GetFunctionData($"{itemKey}.{functionName}");
      return functionTypeData.ParameterCount;
    }

  }
}