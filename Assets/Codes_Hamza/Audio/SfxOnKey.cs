using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SfxOnKey : MonoBehaviour
{
    public KeyCode key = KeyCode.Space;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public float cooldown = 0.05f; // optional debounce

    AudioSource source;
    float nextAllowed;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    void Update()
    {
        if (!clip || Time.time < nextAllowed) return;
        if (Input.GetKeyDown(key))
        {
            source.PlayOneShot(clip, volume);
            nextAllowed = Time.time + cooldown;
        }
    }
}