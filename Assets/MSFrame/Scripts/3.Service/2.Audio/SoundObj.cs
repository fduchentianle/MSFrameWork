using UnityEngine;

namespace MSFrame
{

[Pool(maxNum = 100)]
public class SoundObj : MonoBehaviour
{
    private void Awake()
    {
        AudioSource audioSource = this.GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = this.gameObject.AddComponent<AudioSource>();
    }
}
}
