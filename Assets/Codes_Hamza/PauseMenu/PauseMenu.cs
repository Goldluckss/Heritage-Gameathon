using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private bool openOnStart = false; // optional

    private bool isOpen;

    private void Awake()
    {
        isOpen = openOnStart;
        if (settingsPanel) settingsPanel.SetActive(isOpen);
        Time.timeScale = isOpen ? 0f : 1f;
    }

    private void Update()
    {
        if (EscapePressed())
        {
            ToggleSettings();
        }
    }

    private bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    public void ToggleSettings()
    {
        if (isOpen) CloseSettings();
        else OpenSettings();
    }

    public void OpenSettings()
    {
        if (!settingsPanel) return;
        settingsPanel.SetActive(true);
        Time.timeScale = 0f;
        isOpen = true;
    }

    public void CloseSettings()
    {
        if (!settingsPanel) return;
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        isOpen = false;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}