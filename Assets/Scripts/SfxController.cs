using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SfxController
{

  public static SfxController s_Singleton;
  public SfxController()
  {
    s_Singleton = this;

    _sfxs = new();

    //
    _loadedAudioObjects = new()
    {
      { AudioObjectType.PlayerController, GetAudioObject(AudioObjectType.PlayerController) },
      { AudioObjectType.Character, GetAudioObject(AudioObjectType.Character) },
      { AudioObjectType.Rock, GetAudioObject(AudioObjectType.Rock) },
    };
  }

  //
  List<AudioSource> _sfxs;
  static List<AudioSource> s_sfxs { get { return s_Singleton._sfxs; } }

  //
  Dictionary<AudioObjectType, AudioObject> _loadedAudioObjects;

  //
  public enum AudioObjectType
  {
    PlayerController,
    Character,
    Rock,
  }

  public enum PlayerControllerSfx
  {

  }
  public enum CharacterSfx
  {
    Move,

    Jump,
    Jump_Land,
  }
  public enum RockSfx
  {
    Hit
  }

  public static AudioSource PlaySfxAt(Vector3 position, AudioObjectType audioObjectType, int clipIndex, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f)
  {
    var audioSource = new GameObject($"{audioObjectType}[{clipIndex}]").AddComponent<AudioSource>();
    var audioObject = s_Singleton._loadedAudioObjects[audioObjectType];

    audioSource.transform.position = position;
    audioSource.clip = audioObject.Clips[clipIndex];
    audioSource.volume = volume;
    audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
    audioSource.Play();

    s_sfxs.Add(audioSource);
    return audioSource;
  }

  //
  public static void Update()
  {

    for (var i = s_sfxs.Count - 1; i >= 0; i--)
    {
      var audioSource = s_sfxs[i];

      if (!audioSource.isPlaying)
      {
        s_sfxs.RemoveAt(i);
        GameObject.Destroy(audioSource.gameObject);
      }
    }

  }

  //
  AudioObject GetAudioObject(string name)
  {
    return Resources.Load<AudioObject>($"AudioObjects/{name}");
  }
  AudioObject GetAudioObject(AudioObjectType audioObjectType)
  {
    return Resources.Load<AudioObject>($"AudioObjects/{audioObjectType}");
  }

}
