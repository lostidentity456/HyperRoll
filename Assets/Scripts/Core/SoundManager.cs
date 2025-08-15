using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    // Reference to the component that plays the sound.
    private AudioSource audioSource;

    // --- Sound Effect Clips ---
    [Header("Sound Effects")]
    public AudioClip diceRollSfx;
    public AudioClip pieceMoveSfx;
    public AudioClip buildPropertySfx;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}