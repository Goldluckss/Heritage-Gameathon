using UnityEngine;
using UnityEngine.UI;

public class MuteButton : MonoBehaviour
{
    [Header("Icons / Panels")]
    [SerializeField] private GameObject soundOnPanel;  // visible when sound is ON
    [SerializeField] private GameObject soundOffPanel; // visible when sound is OFF

    private bool isMuted;

    private void Awake()
    {
        // Optional: load saved state
        isMuted = PlayerPrefs.GetInt("muted", 0) == 1;
        ApplyState();
    }

    // Hook this to your button’s OnClick
    public void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyState();

        // Optional: save
        PlayerPrefs.SetInt("muted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyState()
    {
        AudioListener.volume = isMuted ? 0f : 1f;

        if (soundOnPanel) soundOnPanel.SetActive(!isMuted);
        if (soundOffPanel) soundOffPanel.SetActive(isMuted);

        // If using an AudioMixer, replace AudioListener.volume with:
        // audioMixer.SetFloat("MasterVolume", isMuted ? -80f : 0f);
    }
}