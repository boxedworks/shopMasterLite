using UnityEngine;
using UnityEngine.InputSystem;

using SimpleScript;
using CustomUI;

public class PlayerController
{

  public static PlayerController s_Singleton;
  UIElements _uiElements { get { return UIElements.s_Singleton; } }

  //
  Camera _camera;
  GameObject _mouseSelectorUI, _mouseSelectedUI;
  Vector2Int _mousePosition;

  ScriptEntity _selectedEntity;

  //
  public PlayerController()
  {
    s_Singleton = this;

    _camera = Camera.main;

    _mouseSelectorUI = GameObject.Find("MouseSelector");
    _mouseSelectedUI = GameObject.Find("MouseSelected");
    _mouseSelectorUI.transform.position = _mouseSelectedUI.transform.position = new Vector3(100, 100, 100);
  }

  //
  public void Update()
  {
    HandleInput();
  }

  //
  void HandleInput()
  {
    // Check terminal is not focused
    if (!Terminal.IsFocused)
    {

      // Move camera with WASD
      var moveSpeed = 5f;
      Vector3 moveDirection = Vector3.zero;
      if (Keyboard.current.wKey.isPressed)
        moveDirection += Vector3.forward;
      if (Keyboard.current.sKey.isPressed)
        moveDirection += Vector3.back;
      if (Keyboard.current.aKey.isPressed)
        moveDirection += Vector3.left;
      if (Keyboard.current.dKey.isPressed)
        moveDirection += Vector3.right;
      if (moveDirection != Vector3.zero)
        _camera.transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    // Move mouse selector
    var mousePosition = Mouse.current.position.ReadValue();
    if (Physics.SphereCast(_camera.ScreenPointToRay(mousePosition), 0.1f, out var hitInfo, 100))
    {
      var mousePositionWorld = hitInfo.point;
      _mousePosition = new Vector2Int(
        Mathf.Clamp(Mathf.RoundToInt(mousePositionWorld.x), -4, 4),
        Mathf.Clamp(Mathf.RoundToInt(mousePositionWorld.z), -4, 4)
      );
      _mouseSelectorUI.transform.position = new Vector3(_mousePosition.x, -0.49f, _mousePosition.y);
    }
    else
    {
      _mouseSelectorUI.transform.position = new Vector3(100, 100, 100);
    }

    // Check left mouse click
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {

      // UI select
      if (_uiElements.HandleClick())
      {

      }

      // Tile select
      else
      {
        UpdateMouseSelectedUI(_mousePosition.x, _mousePosition.y);

        // Get entity at position
        var entity = ScriptEntity.GetEntity((_mousePosition.x, 0, _mousePosition.y));
        _selectedEntity = entity;

        if (_selectedEntity != null)
          StatusPanel.StatusPanelManager.TryCreateStatusForEntity_S(_selectedEntity);
      }
    }

    // Check entity moved
    if (_selectedEntity != null)
    {
      if (_selectedEntity._EntityData.X != _mousePosition.x || _selectedEntity._EntityData.Z != _mousePosition.y)
      {
        UpdateMouseSelectedUI(_selectedEntity._EntityData.X, _selectedEntity._EntityData.Z);
      }
    }
  }

  //
  void UpdateMouseSelectedUI(int x, int y)
  {
    _mouseSelectedUI.transform.position = new Vector3(x, -0.48f, y);
  }

}