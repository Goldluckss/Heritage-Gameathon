using UnityEngine;

public class EffectToggleWithIcons : MonoBehaviour
{
    [Header("Effect Targets (set one or both)")]
    [SerializeField] private GameObject effectObject;     // e.g., particle root or post-process volume
    [SerializeField] private Behaviour effectComponent;   // e.g., Light, custom script, Volume override

    [Header("Icons / Panels")]
    [SerializeField] private GameObject effectOnIcon;   // visible when effect is ON
    [SerializeField] private GameObject effectOffIcon;  // visible when effect is OFF

    [Header("Persistence (optional)")]
    [SerializeField] private bool rememberState = false;
    private const string PrefKey = "effect_toggle_state"; // 1 = on, 0 = off

    private bool isOn = true;

    private void Awake()
    {
        if (rememberState)
            isOn = PlayerPrefs.GetInt(PrefKey, 1) == 1;
        else
            isOn = GetInitialState();

        ApplyState();
    }

    private bool GetInitialState()
    {
        if (effectObject) return effectObject.activeSelf;
        if (effectComponent) return effectComponent.enabled;
        return true; // default on
    }

    public void ToggleEffect()
    {
        isOn = !isOn;
        ApplyState();
        SaveIfNeeded();
    }

    public void EnableEffect()
    {
        isOn = true;
        ApplyState();
        SaveIfNeeded();
    }

    public void DisableEffect()
    {
        isOn = false;
        ApplyState();
        SaveIfNeeded();
    }

    private void ApplyState()
    {
        if (effectObject) effectObject.SetActive(isOn);
        if (effectComponent) effectComponent.enabled = isOn;

        if (effectOnIcon) effectOnIcon.SetActive(isOn);
        if (effectOffIcon) effectOffIcon.SetActive(!isOn);
    }

    private void SaveIfNeeded()
    {
        if (!rememberState) return;
        PlayerPrefs.SetInt(PrefKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}