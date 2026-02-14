using System.Collections.Generic;
using SimpleScript;
using UnityEngine;
using UnityEngine.UI;

namespace CustomUI
{
  public class InventoryPanel
  {

    //
    public class InventoryPanelManager
    {
      public static InventoryPanelManager s_Singleton { get; private set; }
      UIElements _uiElements { get { return UIElements.s_Singleton; } }

      //
      Dictionary<int, InventoryPanel> _openInventoryPanels;

      public InventoryPanelManager()
      {
        s_Singleton = this;

        _openInventoryPanels = new();
      }

      public void RegisterInventoryPanel(InventoryPanel inventoryPanel)
      {

        // Offset new inventory panel
        if (_openInventoryPanels.Count > 0)
        {
          var inventoryPanels = new List<InventoryPanel>(_openInventoryPanels.Values);
          var lastInventoryPanel = inventoryPanels[^1];
          inventoryPanel._panel.position = lastInventoryPanel._panel.position + new Vector3(30, -30, 0);
          _uiElements.SmartSetPanelPosition(inventoryPanel._panel);
        }

        _openInventoryPanels.Add(inventoryPanel._entityId, inventoryPanel);
      }
      public void UnregisterInventoryPanel(InventoryPanel inventoryPanel)
      {
        _openInventoryPanels.Remove(inventoryPanel._entityId);
      }

      void TryCreateInventoryForEntity(ScriptEntity entity)
      {
        if (!_openInventoryPanels.ContainsKey(entity._EntityData.Id))
          new InventoryPanel(entity);
        else
        {
          var inventoryPanel = _openInventoryPanels[entity._EntityData.Id];
          inventoryPanel._panel.SetAsLastSibling();
        }
      }
      public static void TryCreateInventoryForEntity_S(ScriptEntity entity)
      {
        s_Singleton.TryCreateInventoryForEntity(entity);
      }

      // Replace inventory panel with new one
      void TryReplaceInventoryPanel(ScriptEntity entity)
      {
        if (_openInventoryPanels.ContainsKey(entity._EntityData.Id))
        {
          var inventoryPanel = _openInventoryPanels[entity._EntityData.Id];
          var panelPosition = inventoryPanel._panel.position;
          var isDragging = _uiElements.IsDraggingPanel(inventoryPanel._panel);
          inventoryPanel.CloseButtonAction();

          var newInventoryPanel = new InventoryPanel(entity);
          newInventoryPanel._panel.position = panelPosition;
          if (isDragging)
            _uiElements.ReplaceDraggingPanel(newInventoryPanel._panel);

        }
      }
      public static void TryReplaceInventoryPanel_S(ScriptEntity entity)
      {
        s_Singleton.TryReplaceInventoryPanel(entity);
      }
    }

    //
    ScriptEntity _entity;
    int _entityId { get { return _entity._EntityData.Id; } }
    RectTransform _panel;

    public InventoryPanel(ScriptEntity entity)
    {
      _entity = entity;

      // Create inventory panel
      var inventoryBase = UIElements.s_Singleton._InventoryPanel;
      _panel = GameObject.Instantiate(inventoryBase, inventoryBase.transform.parent).transform as RectTransform;

      // Create inventory based on size
      var entityStorage = _entity._EntityData.ItemStorage;
      var itemSlots = new List<GameObject>();

      var inventorySize = entityStorage.Count;
      var inventoryWidth = 4;

      var rowBase = _panel.GetChild(1).GetChild(0);
      var itemSlotBase = rowBase.GetChild(0).gameObject;

      // Create rows based on inventory width
      var inventoryHeight = Mathf.CeilToInt(inventorySize / (float)inventoryWidth);
      for (int y = 0; y < inventoryHeight; y++)
      {
        var row = y == 0 ? rowBase : GameObject.Instantiate(rowBase, rowBase.parent);
      }

      // Create buttons based on inventory size
      if (inventorySize == 0)
      {
        GameObject.Destroy(rowBase.gameObject);
      }
      else
        for (int y = 0; y < inventoryHeight; y++)
        {
          var row = rowBase.parent.GetChild(y);

          for (int x = 0; x < inventoryWidth; x++)
          {
            var buttonIndex = y * inventoryWidth + x;
            if (buttonIndex >= inventorySize)
              break;

            var itemSlot = x == 0 ? row.GetChild(0).gameObject : GameObject.Instantiate(itemSlotBase, row);
            itemSlots.Add(itemSlot);
          }

          // Resize panel based on inventory size
          var buttonHeight = 60 + (inventoryHeight - 1) * 0.75f;
          var mainPanel = _panel;
          var subPanel = mainPanel.GetChild(1) as RectTransform;
          mainPanel.sizeDelta = new Vector2(mainPanel.sizeDelta.x, 65 + inventoryHeight * buttonHeight);
          subPanel.sizeDelta = new Vector2(subPanel.sizeDelta.x, 25 + inventoryHeight * buttonHeight);
        }

      // Register buttons
      {

        // Item slots
        // fake item data
        var itemDataList = _entity._EntityData.ItemStorage;
        for (int i = 0; i < itemDataList.Count; i++)
        {
          var itemData = itemDataList[i];
          if (itemData == null)
            continue;
          var itemSlot = itemSlots[i];

          // Set item icon
          var icon = new GameObject("Icon");
          icon.transform.localScale *= 0.5f;
          var sprite = icon.AddComponent<Image>();
          sprite.transform.SetParent(itemSlot.transform, false);

          var loadedSprite = Resources.Load<Sprite>($"Images/Items/{itemData._ItemTypeData.Name.ToLower()}");
          sprite.sprite = loadedSprite;

          // Set button action
          var button = itemSlot.AddComponent<Button>();
          var slotIndex = i;
          button.onClick.AddListener(() =>
          {
            Debug.Log("Clicked item slot " + slotIndex + " with item " + (itemData != null ? itemData._ItemTypeData.Name : "null"));
          });
        }

        // Close button
        var closeButton = _panel.GetChild(0).GetChild(1).GetComponent<Button>();
        closeButton.onClick.AddListener(() => CloseButtonAction());
      }

      // Register with manager
      InventoryPanelManager.s_Singleton.RegisterInventoryPanel(this);

      // Show panel
      _panel.gameObject.SetActive(true);
    }

    void CloseButtonAction()
    {

      // Destroy inventory panel
      GameObject.Destroy(_panel.gameObject);
      _panel = null;

      // Unregister from manager
      InventoryPanelManager.s_Singleton.UnregisterInventoryPanel(this);
    }
  }
}