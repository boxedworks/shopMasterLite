
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using System.Linq;

namespace CustomUI
{
  public class UIElements
  {

    public static UIElements s_Singleton;

    //
    public RectTransform _UI;
    GraphicRaycaster _graphicRaycaster;

    RectTransform _draggingPanel;
    Vector3 _dragDelta;

    public GameObject _InventoryPanel;

    //
    public UIElements()
    {
      s_Singleton = this;

      // Gather UI elements
      _UI = GameObject.Find("UI").GetComponent<RectTransform>();
      _graphicRaycaster = _UI.GetComponent<GraphicRaycaster>();

      _InventoryPanel = _UI.Find("InventoryPanels").GetChild(0).gameObject;
      _InventoryPanel.SetActive(false);

      // Initialize other UI systems
      new InventoryPanel.InventoryPanelManager();
    }

    //
    public void Update()
    {
      // Handle dragging panels
      if (_draggingPanel != null)
      {
        var mousePosition = new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0) + _dragDelta;
        SmartSetPanelPosition(_draggingPanel, mousePosition);

        if (Mouse.current.leftButton.wasReleasedThisFrame)
          _draggingPanel = null;
      }
    }

    // Keep panel from going off screen keeping into account recttransform pivot
    public void SmartSetPanelPosition(RectTransform panel, Vector3 position)
    {
      var panelSize = panel.sizeDelta;
      var panelPivot = panel.pivot;
      position.x = Mathf.Clamp(position.x, panelSize.x * panelPivot.x, Screen.width - panelSize.x * (1 - panelPivot.x));
      position.y = Mathf.Clamp(position.y, panelSize.y * panelPivot.y, Screen.height - panelSize.y * (1 - panelPivot.y));
      panel.position = position;
    }
    public void SmartSetPanelPosition(RectTransform panel)
    {
      SmartSetPanelPosition(panel, panel.position);
    }

    // Check ui drag
    public bool HandleClick()
    {
      if (EventSystem.current.IsPointerOverGameObject())
      {

        var pointerEventData = new PointerEventData(EventSystem.current)
        {
          position = Mouse.current.position.ReadValue()
        };

        var raycastResults = new List<RaycastResult>();
        _graphicRaycaster.Raycast(pointerEventData, raycastResults);

        if (raycastResults.Count > 0)
        {
          var clickedUI = raycastResults
            .FirstOrDefault(result => result.gameObject.layer == LayerMask.NameToLayer("UI")).gameObject;

          if (clickedUI != null)
          {
            if (clickedUI.name == "Header")
            {
              _draggingPanel = clickedUI.transform.parent as RectTransform;
              _draggingPanel.SetAsLastSibling();

              _dragDelta = _draggingPanel.position - new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0);
              return true;
            }
          }
        }
      }

      return false;
    }

    //
    public void ReplaceDraggingPanel(RectTransform newPanel)
    {
      if (_draggingPanel != null)
      {
        _draggingPanel = newPanel;
      }
    }
    public bool IsDraggingPanel(RectTransform panel)
    {
      return _draggingPanel == panel;
    }


  }
}