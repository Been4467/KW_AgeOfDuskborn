using UnityEngine;

public class UIButtonSound : MonoBehaviour
{
    public AudioSource audioSource;

    public void PlayClickSound()
    {
        audioSource.Play();
    }
}