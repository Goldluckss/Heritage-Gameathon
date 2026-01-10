using UnityEngine;

public class EffectToggle : MonoBehaviour
{
    [Header("Assign either object OR component")]
    [SerializeField] private GameObject effectObject;          // e.g., particle system root, post-process volume
    [SerializeField] private Behaviour effectComponent;        // e.g., Bloom, custom script, Light, etc.

    private bool isOn;

    private void Awake()
    {
        // Initialize to current state (if assigned)
        if (effectObject) isOn = effectObject.activeSelf;
        else if (effectComponent) isOn = effectComponent.enabled;
    }

    public void ToggleEffect()
    {
        if (isOn) DisableEffect();
        else EnableEffect();
    }

    public void EnableEffect()
    {
        if (effectObject) effectObject.SetActive(true);
        if (effectComponent) effectComponent.enabled = true;
        isOn = true;
    }

    public void DisableEffect()
    {
        if (effectObject) effectObject.SetActive(false);
        if (effectComponent) effectComponent.enabled = false;
        isOn = false;
    }
}