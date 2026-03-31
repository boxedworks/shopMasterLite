using UnityEngine;

[CreateAssetMenu(fileName = "AudioObject", menuName = "ScriptableObjects/AudioObject", order = 1)]
public class AudioObject : ScriptableObject
{
  public AudioClip[] Clips;
}
