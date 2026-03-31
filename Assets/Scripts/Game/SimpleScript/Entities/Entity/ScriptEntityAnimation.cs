
using UnityEngine;

namespace Assets.Scripts.Game.SimpleScript.Entities.Entity
{
  //
  public class ScriptEntityAnimation
  {

    ScriptEntity _entity;
    Transform _billboard { get { return _entity._Billboard; } }
    Transform _sprite { get { return _entity._Sprite; } }

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

    public ScriptEntityAnimation(ScriptEntity entity, AnimationType animationType, float duration)
    {
      _entity = entity;
      _animationType = animationType;

      _animationStartPos = _billboard.position;

      _animationDuration = duration;
      _animationTime = 0f;

      // Play sfx
      switch (animationType)
      {
        case AnimationType.Move:
          SfxController.PlaySfxAt(entity._TilePositionVector3, SfxController.AudioObjectType.Character, (int)SfxController.CharacterSfx.Move, 0.13f);
          break;

        case AnimationType.Jump:
          SfxController.PlaySfxAt(entity._TilePositionVector3, SfxController.AudioObjectType.Character, (int)SfxController.CharacterSfx.Jump, 0.3f);
          break;
      }
    }

    //
    public void Update()
    {

      // Check if animation finished
      if (_animationTime > _animationDuration)
      {
        _entity._CurrentAnimation = null;
        return;
      }
      _animationTime += Time.deltaTime;
      var isAnimationComplete = _animationTime > _animationDuration;

      var animationTimeNormalized = _animationTime / _animationDuration;
      var endPos = _entity._TilePositionVector3;

      // Apply animation effect based on type
      if (isAnimationComplete)
        OnAnimatedRemoved();
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
            _animationStartPos = Vector3.zero;
            var localDirection = _sprite.InverseTransformDirection(ScriptEntityHelper.DirectionToVector3(_entity._Direction));
            endPos = _animationStartPos + localDirection * 0.65f;
            position = Vector3.Lerp(_animationStartPos, endPos, Mathf.Sin(animationTimeNormalized * Mathf.PI));
            _sprite.localPosition = position;
            break;

          case AnimationType.Shake:
            var shakeIntensity = 1f;
            var shakeDisplacement = Random.insideUnitSphere * shakeIntensity * 0.1f;
            _sprite.localPosition = shakeDisplacement;
            break;
        }
    }

    //
    public void OnAnimatedRemoved()
    {
      var endPos = _entity._TilePositionVector3;

      switch (_animationType)
      {
        case AnimationType.Move:
        case AnimationType.Jump:
          _billboard.position = endPos;
          break;

        case AnimationType.Attack:
        case AnimationType.Shake:
          _sprite.localPosition = Vector3.zero;
          break;
      }

      // Check sfx
      switch (_animationType)
      {
        case AnimationType.Jump:
          SfxController.PlaySfxAt(_entity._TilePositionVector3, SfxController.AudioObjectType.Character, (int)SfxController.CharacterSfx.Jump_Land, 0.2f);
          break;
      }
    }

  }
}