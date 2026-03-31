using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Assets.Scripts.Game.SimpleScript.Scripting;
using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using Assets.Scripts.Game.SimpleScript.Entities.Item;

namespace Assets.Scripts.Game.UI
{
  public class StatusPanel
  {

    public enum SubPanelType
    {
      None,

      Inventory,
      Logger,
      Scripts,
    }

    //
    ScriptEntity _entity;
    public ScriptEntity _Entity { get { return _entity; } }

    int _entityId { get { return _entity._EntityData.Id; } }
    public int _EntityId { get { return _entityId; } }

    RectTransform _panel;
    public RectTransform _Panel { get { return _panel; } }

    Dictionary<SubPanelType, RectTransform> _openSubPanels;

    public StatusPanel(ScriptEntity entity)
    {
      _entity = entity;

      // Create status panel
      var statusBase = UIElements.s_Singleton._StatusPanel;
      _panel = Object.Instantiate(statusBase, statusBase.transform.parent).transform as RectTransform;
      var body = _panel.GetChild(1);

      _openSubPanels = new();

      // Set flavor text
      var statusText = body.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
      var entityName = _entity._EntityTypeData.Name;
      var entityDescription = _entity._EntityTypeData.Description;
      statusText.text = $"<b>Name:</b>{entityName}\n<b>Description:</b>{entityDescription}";

      // Register buttons
      {
        RegisterButtons();

        // Close button
        var closeButton = _panel.GetChild(0).GetChild(1).GetComponent<Button>();
        closeButton.onClick.AddListener(() => CloseButtonAction());
      }

      // Register with manager
      StatusPanelController.s_Singleton.RegisterStatusPanel(this);

      // Show panel
      _panel.gameObject.SetActive(true);
    }

    //
    void RegisterButtons()
    {
      var body = _panel.GetChild(1);

      var buttons = body.GetChild(1);
      var buttonBase = buttons.GetChild(0).gameObject;

      // Clean up old
      for (var i = buttons.childCount - 1; i > 0; i--)
        Object.DestroyImmediate(buttons.GetChild(i).gameObject);
      Object.DestroyImmediate(buttonBase.gameObject.GetComponent<Button>());

      // Set new buttons
      GameObject getButton(int buttonIndex)
      {
        return buttonIndex == 0 ? buttonBase : Object.Instantiate(buttonBase, buttons);
      }
      void configureButton(GameObject button, SubPanelType subPanelType, string iconName)
      {
        button.name = $"{subPanelType}";

        var icon = button.transform.GetChild(0).GetComponent<Image>();
        var loadedSprite = GameResources.LoadSprite($"UI/{iconName}");
        icon.sprite = loadedSprite;

        var buttonComponent = button.AddComponent<Button>();
        buttonComponent.onClick.AddListener(() =>
        {
          OnStatusButtonClicked(subPanelType);
        });
      }

      // Gather buttons
      var hasInventory = _entity._HasStorage;
      var hasLog = true;//_entity._HasLog;
      var hasScripts = true;
      int buttonCount = (hasInventory ? 1 : 0) + (hasLog ? 1 : 0) + (hasScripts ? 1 : 0);
      var buttonList = new List<GameObject>();
      for (int i = 0; i < buttonCount; i++)
        buttonList.Add(getButton(i));
      int currentButtonIndex = 0;

      // Inventory button
      if (hasInventory)
        configureButton(buttonList[currentButtonIndex++], SubPanelType.Inventory, "backpack");

      // Scripts button
      if (hasScripts)
        configureButton(buttonList[currentButtonIndex++], SubPanelType.Scripts, "script");

      // Logger button
      if (hasLog)
        configureButton(buttonList[currentButtonIndex++], SubPanelType.Logger, "script");
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
        case SubPanelType.Scripts:
          if (!_openSubPanels.ContainsKey(SubPanelType.Scripts))
            OpenScriptsPanel();
          else
            CloseScriptsPanel();
          break;
      }
    }

    //
    public void OpenInventoryPanel()
    {
      var panelBase = UIElements.s_Singleton._InventoryPanel;
      var panel = Object.Instantiate(panelBase, _panel.GetChild(1)).transform as RectTransform;

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
        var row = y == 0 ? rowBase : Object.Instantiate(rowBase, rowBase.parent);
      }

      // Create buttons based on status size
      if (statusSize == 0)
      {
        Object.Destroy(rowBase.gameObject);
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

            var itemSlot = x == 0 ? row.GetChild(0).gameObject : Object.Instantiate(itemSlotBase, row);
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
      for (int i = 0; i < entityStorage.Count; i++)
      {
        var itemData = entityStorage[i];
        if (itemData == null)
          continue;
        var itemSlot = itemSlots[i];

        // Set item icon
        var icon = new GameObject("Icon");
        icon.transform.localScale *= 0.5f;
        var sprite = icon.AddComponent<Image>();
        sprite.transform.SetParent(itemSlot.transform, false);

        var item = ScriptItemController.GetItem(itemData.Id);
        var loadedSprite = GameResources.LoadItemSprite(item._SpriteName);
        sprite.sprite = loadedSprite;

        // Set button action
        var button = itemSlot.AddComponent<Button>();
        var slotIndex = i;
        button.onClick.AddListener(() =>
        {
          Debug.Log("Clicked item slot " + slotIndex + " with item " + (item != null ? item._ItemTypeData.Name : "null"));
        });
      }

      //
      _openSubPanels.Add(SubPanelType.Inventory, panel);
      panel.gameObject.SetActive(true);
    }
    public void CloseInventoryPanel()
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
    public void UpdateLoggerText()
    {
      var panel = _openSubPanels[SubPanelType.Logger];
      var logText = panel.GetChild(1).GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
      logText.text = _entity.GetLogString();
    }

    //
    void OpenScriptsPanel()
    {
      var panelBase = UIElements.s_Singleton._ScriptsPanel;
      var panel = Object.Instantiate(panelBase, _panel.GetChild(1)).transform as RectTransform;

      //
      _openSubPanels.Add(SubPanelType.Scripts, panel);
      panel.gameObject.SetActive(true);

      //
      UpdateScriptsPanel();
    }
    void CloseScriptsPanel()
    {
      ClosePanel(SubPanelType.Scripts);
    }
    public void UpdateScriptsPanel()
    {
      var panel = _openSubPanels[SubPanelType.Scripts];
      var scriptEntries = panel.GetChild(1);

      var entriesAdded = 0;
      (TMPro.TextMeshProUGUI text, Button button) GetEntry()
      {
        // Try to get existing entry, else create new one
        var hasEntry = entriesAdded < scriptEntries.childCount;
        var entry = hasEntry ? scriptEntries.GetChild(entriesAdded) : Object.Instantiate(scriptEntries.GetChild(0).gameObject, scriptEntries).transform;
        var text = entry.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
        var button = entry.GetChild(1).GetComponent<Button>();

        entriesAdded++;

        return (text, button);
      }

      // Add list of engine scripts
      if (_entity._EntityData.OwnerId == -1)
      {
        var typeName = _entity._EntityTypeData.Name.ToLower();
        var methods = ScriptEntityHelper.s_FunctionRepository.GetFunctionsByType(typeName);
        foreach (var method in methods)
        {
          var (scriptText, scriptButton) = GetEntry();

          scriptText.text = $"<b>System:</b> {method}";

          scriptButton.onClick.RemoveAllListeners();
          scriptButton.onClick.AddListener(() =>
          {
            var newScript = _entity.LoadAndAttachScript(new ScriptBaseController.ScriptLoadData()
            {
              PathTo = $"{typeName}.{method}",
              ScriptType = ScriptBaseController.ScriptType.ENTITY
            });
            if (newScript == null)
              return;

            UIElements.s_Singleton._EditorPanel.AttachScript(newScript);
            UpdateScriptsPanel();
          });
        }
      }

      // No attached scripts
      if (_entity._AttachedScripts == null)
      {
        var (scriptText, scriptButton) = GetEntry();
        scriptText.text = "No scripts attached.";

        scriptButton.onClick.RemoveAllListeners();
        if (_entity._IsPlayer)
          scriptButton.onClick.AddListener(() =>
          {
            var newScript = _entity.LoadAndAttachScript(new ScriptBaseController.ScriptLoadData()
            {
              PathTo = "test",
              ScriptType = ScriptBaseController.ScriptType.PLAYER
            });
            if (newScript == null)
              return;

            UpdateScriptsPanel();
          });
      }

      // List attached scripts
      else
      {
        for (var i = 0; i < _entity._AttachedScripts.Count; i++)
        {
          var (scriptText, scriptButton) = GetEntry();

          var script = _entity._AttachedScripts[i];
          string scriptStatus;
          if (script._HasError)
            scriptStatus = "<color=red>Error</color>";
          else if (!script._IsEnabled)
            scriptStatus = "Disabled";
          else if (script._IsWaitingFor)
            scriptStatus = "<color=orange>Waiting</color>";
          else if (script._IsValid)
            scriptStatus = "<color=blue>Running</color>";
          else
            scriptStatus = "Exited";
          var scriptName = script._Name ?? $"Custom script";
          scriptText.text = $@"<b>Name: {scriptName}</b>
<b>State: {scriptStatus}</b>";

          scriptButton.onClick.RemoveAllListeners();
          if (_entity._IsPlayer)
            scriptButton.onClick.AddListener(() =>
            {
              if (script._IsEnabled)
                script.Disable();
              else
                ScriptBaseController.RemoveScript(script);

              UpdateScriptsPanel();
            });
        }
      }

      // Clean up extra entries
      for (var i = scriptEntries.childCount - 1; i >= entriesAdded; i--)
        Object.DestroyImmediate(scriptEntries.GetChild(i).gameObject);

      // Set panel size
      var baseSize = 111f;
      var subSize = 75f;
      var entrySize = 60f;
      var panelOffset = Mathf.Clamp(entriesAdded - 1, 0, int.MaxValue) * entrySize + (entriesAdded > 1 ? 5f : 0);

      panel.sizeDelta = new Vector2(panel.sizeDelta.x, baseSize + panelOffset);

      var panelSub = panel.GetChild(1) as RectTransform;
      panelSub.sizeDelta = new Vector2(panelSub.sizeDelta.x, subSize + panelOffset);
    }

    public void ClosePanel(SubPanelType panelKey)
    {
      Object.Destroy(_openSubPanels[panelKey].gameObject);
      _openSubPanels.Remove(panelKey);
    }

    //
    public void CloseButtonAction()
    {

      // Destroy status panel
      Object.Destroy(_panel.gameObject);
      _panel = null;

      // Unregister from manager
      StatusPanelController.s_Singleton.UnregisterStatusPanel(this);
    }

    //
    public bool HasOpenSubPanel(SubPanelType subPanelKey)
    {
      return _openSubPanels.ContainsKey(subPanelKey);
    }

  }
}