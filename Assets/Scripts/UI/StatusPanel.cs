using System.Collections.Generic;
using SimpleScript;
using UnityEngine;
using UnityEngine.UI;

namespace CustomUI
{
  public class StatusPanel
  {

    public enum SubPanelType
    {
      None,

      Inventory,
      Logger,
    }

    //
    public class StatusPanelManager
    {
      public static StatusPanelManager s_Singleton { get; private set; }
      UIElements _uiElements { get { return UIElements.s_Singleton; } }

      //
      Dictionary<int, StatusPanel> _openStatusPanels;

      public StatusPanelManager()
      {
        s_Singleton = this;

        _openStatusPanels = new();
      }

      public void RegisterStatusPanel(StatusPanel statusPanel)
      {

        // Offset new status panel
        if (_openStatusPanels.Count > 0)
        {
          var statusPanels = new List<StatusPanel>(_openStatusPanels.Values);
          var lastStatusPanel = statusPanels[^1];
          statusPanel._panel.position = lastStatusPanel._panel.position + new Vector3(30, -30, 0);
          _uiElements.SmartSetPanelPosition(statusPanel._panel);
        }

        _openStatusPanels.Add(statusPanel._entityId, statusPanel);
      }
      public void UnregisterStatusPanel(StatusPanel statusPanel)
      {
        _openStatusPanels.Remove(statusPanel._entityId);
      }

      void TryCreateStatusForEntity(ScriptEntity entity)
      {
        if (!_openStatusPanels.ContainsKey(entity._EntityData.Id))
          new StatusPanel(entity);
        else
        {
          var statusPanel = _openStatusPanels[entity._EntityData.Id];
          statusPanel._panel.SetAsLastSibling();
        }
      }
      public static void TryCreateStatusForEntity_S(ScriptEntity entity)
      {
        s_Singleton.TryCreateStatusForEntity(entity);
      }

      // Replace status panel with new one
      void UpdateStatusUI(ScriptEntity entity, SubPanelType subPanelKey)
      {
        if (_openStatusPanels.ContainsKey(entity._EntityData.Id))
        {
          var statusPanel = _openStatusPanels[entity._EntityData.Id];
          if (!statusPanel._openSubPanels.ContainsKey(subPanelKey))
            return;

          switch (subPanelKey)
          {
            case SubPanelType.Inventory:
              statusPanel.CloseInventoryPanel();
              statusPanel.OpenInventoryPanel();
              break;
            case SubPanelType.Logger:
              statusPanel.UpdateLoggerText();
              break;
          }
        }
      }
      public static void UpdateStatusUI_S(ScriptEntity entity, SubPanelType subPanelKey)
      {
        s_Singleton.UpdateStatusUI(entity, subPanelKey);
      }
    }

    //
    ScriptEntity _entity;
    int _entityId { get { return _entity._EntityData.Id; } }
    RectTransform _panel;

    Dictionary<SubPanelType, RectTransform> _openSubPanels;

    public StatusPanel(ScriptEntity entity)
    {
      _entity = entity;

      // Create status panel
      var statusBase = UIElements.s_Singleton._StatusPanel;
      _panel = GameObject.Instantiate(statusBase, statusBase.transform.parent).transform as RectTransform;
      var body = _panel.GetChild(1);

      _openSubPanels = new();

      // Set flavor text
      var statusText = body.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
      var entityName = _entity._EntityTypeData.Name;
      var entityDescription = _entity._EntityTypeData.Description;
      statusText.text = $"<b>Name:</b>{entityName}\n<b>Description:</b>{entityDescription}";

      // Register buttons
      {
        var buttons = body.GetChild(1);
        var buttonBase = buttons.GetChild(0).gameObject;

        GameObject getButton(int buttonIndex)
        {
          return buttonIndex == 0 ? buttonBase : GameObject.Instantiate(buttonBase, buttons);
        }
        void configureButton(GameObject button, SubPanelType subPanelType, string iconName)
        {
          button.name = $"{subPanelType}";

          var icon = button.transform.GetChild(0).GetComponent<Image>();
          var loadedSprite = Resources.Load<Sprite>($"Images/UI/{iconName}");
          icon.sprite = loadedSprite;

          var buttonComponent = button.AddComponent<Button>();
          buttonComponent.onClick.AddListener(() =>
          {
            OnStatusButtonClicked(subPanelType);
          });
        }

        // Gather buttons
        var hasInventory = _entity._HasStorage;
        var hasLog = true;
        int buttonCount = (hasInventory ? 1 : 0) + (hasLog ? 1 : 0);
        var buttonList = new List<GameObject>();
        for (int i = 0; i < buttonCount; i++)
          buttonList.Add(getButton(i));
        int currentButtonIndex = 0;

        // Inventory button
        if (hasInventory)
          configureButton(buttonList[currentButtonIndex++], SubPanelType.Inventory, "backpack");

        // Logger button
        if (hasLog)
          configureButton(buttonList[currentButtonIndex++], SubPanelType.Logger, "script");

        // Close button
        var closeButton = _panel.GetChild(0).GetChild(1).GetComponent<Button>();
        closeButton.onClick.AddListener(() => CloseButtonAction());
      }

      // Register with manager
      StatusPanelManager.s_Singleton.RegisterStatusPanel(this);

      // Show panel
      _panel.gameObject.SetActive(true);
    }

    //
    void OnStatusButtonClicked(SubPanelType buttonName)
    {
      switch (buttonName)
      {
        case SubPanelType.Inventory:
          if (!_openSubPanels.ContainsKey(SubPanelType.Inventory))
            OpenInventoryPanel();
          else
            CloseInventoryPanel();
          break;
        case SubPanelType.Logger:
          if (!_openSubPanels.ContainsKey(SubPanelType.Logger))
            OpenLoggerPanel();
          else
            CloseLoggerPanel();
          break;
      }
    }

    //
    void OpenInventoryPanel()
    {
      var panelBase = UIElements.s_Singleton._InventoryPanel;
      var panel = GameObject.Instantiate(panelBase, _panel.GetChild(1)).transform as RectTransform;

      // Create status based on size
      var entityStorage = _entity._Storage;
      var itemSlots = new List<GameObject>();

      var statusSize = entityStorage.Count;
      var statusWidth = 4;

      var rowBase = panel.GetChild(1).GetChild(0);
      var itemSlotBase = rowBase.GetChild(0).gameObject;

      // Create rows based on status width
      var statusHeight = Mathf.CeilToInt(statusSize / (float)statusWidth);
      for (int y = 0; y < statusHeight; y++)
      {
        var row = y == 0 ? rowBase : GameObject.Instantiate(rowBase, rowBase.parent);
      }

      // Create buttons based on status size
      if (statusSize == 0)
      {
        GameObject.Destroy(rowBase.gameObject);
      }
      else
        for (int y = 0; y < statusHeight; y++)
        {
          var row = rowBase.parent.GetChild(y);

          for (int x = 0; x < statusWidth; x++)
          {
            var buttonIndex = y * statusWidth + x;
            if (buttonIndex >= statusSize)
              break;

            var itemSlot = x == 0 ? row.GetChild(0).gameObject : GameObject.Instantiate(itemSlotBase, row);
            itemSlots.Add(itemSlot);
          }

          // Resize panel based on status size
          var buttonHeight = 60 + (statusHeight - 1) * 0.75f;
          var mainPanel = panel;
          var subPanel = mainPanel.GetChild(1) as RectTransform;
          mainPanel.sizeDelta = new Vector2(mainPanel.sizeDelta.x, 65 + statusHeight * buttonHeight);
          subPanel.sizeDelta = new Vector2(subPanel.sizeDelta.x, 25 + statusHeight * buttonHeight);
        }

      // Item slots
      var itemDataList = _entity._Storage;
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

      //
      _openSubPanels.Add(SubPanelType.Inventory, panel);
      panel.gameObject.SetActive(true);
    }
    void CloseInventoryPanel()
    {
      ClosePanel(SubPanelType.Inventory);
    }

    //
    void OpenLoggerPanel()
    {
      var panelBase = UIElements.s_Singleton._LoggerPanel;
      var panel = GameObject.Instantiate(panelBase, _panel.GetChild(1)).transform as RectTransform;

      //
      _openSubPanels.Add(SubPanelType.Logger, panel);
      panel.gameObject.SetActive(true);

      // Set log from entity data
      UpdateLoggerText();
    }
    void CloseLoggerPanel()
    {
      ClosePanel(SubPanelType.Logger);
    }
    void UpdateLoggerText()
    {
      var panel = _openSubPanels[SubPanelType.Logger];
      var logText = panel.GetChild(1).GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
      logText.text = _entity.GetLogString();
    }

    void ClosePanel(SubPanelType panelKey)
    {
      GameObject.Destroy(_openSubPanels[panelKey].gameObject);
      _openSubPanels.Remove(panelKey);
    }

    //
    void CloseButtonAction()
    {

      // Destroy status panel
      GameObject.Destroy(_panel.gameObject);
      _panel = null;

      // Unregister from manager
      StatusPanelManager.s_Singleton.UnregisterStatusPanel(this);
    }
  }
}