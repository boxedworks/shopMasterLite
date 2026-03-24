using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class FxController
{

  // public static FxController s_Singleton;
  // public FxController()
  // {
  //   s_Singleton = this;
  // }

  //
  enum VisualEffectType
  {
    SMOKE,
    X,

    BUFF,

    LINE,
  }
  static VisualEffect GetVisualEffect(VisualEffectType visualEffectType)
  {
    return GameObject.Find("Particles").transform.GetChild((int)visualEffectType).GetComponent<VisualEffect>();
  }

  // Fx are a mix of visual effect + sound FX
  public enum FxType
  {
    SPAWN,
    DIE,
    MOVE,
    BUFF,
    DAMAGE,

  }
  public static void PlayFx(FxType fxType, Vector3 atPos)
  {

    VisualEffect visualEffect = null;
    switch (fxType)
    {

      // case FxType.SPAWN:

      //   visualEffect = GetVisualEffect(VisualEffectType.SMOKE);
      //   SfxController.PlaySfx(SfxController.AudioObjectType.CARD_OBJECT_EFFECTS, (int)fxType, 0.5f);
      //   break;

      // case FxType.DIE:

      //   visualEffect = GetVisualEffect(VisualEffectType.X);
      //   SfxController.PlaySfx(SfxController.AudioObjectType.CARD_OBJECT_EFFECTS, (int)fxType, 0.4f);
      //   break;

      // case FxType.MOVE:

      //   SfxController.PlaySfx(SfxController.AudioObjectType.CARD_OBJECT_EFFECTS, (int)fxType, 0.5f);
      //   break;

      // case FxType.BUFF:

      //   visualEffect = GetVisualEffect(VisualEffectType.BUFF);
      //   SfxController.PlaySfx(SfxController.AudioObjectType.CARD_OBJECT_EFFECTS, (int)fxType, 0.5f);
      //   break;

      // case FxType.DAMAGE:

      //   visualEffect = GetVisualEffect(VisualEffectType.LINE);
      //   SfxController.PlaySfx(SfxController.AudioObjectType.CARD_OBJECT_EFFECTS, (int)fxType, 0.5f);
      //   break;

    }

    //
    if (visualEffect != null)
    {
      visualEffect.transform.position = atPos;
      visualEffect.Play();
    }
  }

}
