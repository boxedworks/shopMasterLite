

using System.Collections.Generic;

using Assets.Scripts.Game.SimpleScript.Entities.Entity;
using UnityEngine;
using static Assets.Scripts.Game.UI.StatusPanel;

namespace Assets.Scripts.Game.UI
{
  //
  public class StatusPanelController
  {
    public static StatusPanelController s_Singleton { get; private set; }
    UIElements _uiElements { get { return UIElements.s_Singleton; } }

    //
    Dictionary<int, StatusPanel> _openStatusPanels;

    public StatusPanelController()
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
        statusPanel._Panel.position = lastStatusPanel._Panel.position + new Vector3(30, -30, 0);
        _uiElements.SmartSetPanelPosition(statusPanel._Panel);
      }

      _openStatusPanels.Add(statusPanel._EntityId, statusPanel);
    }
    public void UnregisterStatusPanel(StatusPanel statusPanel)
    {
      _openStatusPanels.Remove(statusPanel._EntityId);
    }

    void TryCreateStatusForEntity(ScriptEntity entity)
    {
      if (!_openStatusPanels.ContainsKey(entity._EntityData.Id))
        new StatusPanel(entity);
      else
      {
        var statusPanel = _openStatusPanels[entity._EntityData.Id];
        statusPanel._Panel.SetAsLastSibling();
      }
    }
    public static void TryCreateStatusForEntity_S(ScriptEntity entity)
    {
      s_Singleton.TryCreateStatusForEntity(entity);
    }

    public static void TryDestroyStatusForEntity_S(ScriptEntity entity)
    {
      if (s_Singleton._openStatusPanels.ContainsKey(entity._EntityData.Id))
        s_Singleton._openStatusPanels[entity._EntityData.Id].CloseButtonAction();
    }

    // Update or replace status panel with new one
    void UpdateStatusUI(ScriptEntity entity, SubPanelType subPanelKey)
    {
      if (_openStatusPanels.ContainsKey(entity._EntityData.Id))
      {
        var statusPanel = _openStatusPanels[entity._EntityData.Id];
        if (!statusPanel.HasOpenSubPanel(subPanelKey))
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
          case SubPanelType.Scripts:
            statusPanel.UpdateScriptsPanel();
            break;
        }
      }
    }
    public static void UpdateStatusUI_S(ScriptEntity entity, SubPanelType subPanelKey)
    {
      s_Singleton.UpdateStatusUI(entity, subPanelKey);
    }
  }
}