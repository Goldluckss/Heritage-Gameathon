using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HeneGames.DialogueSystem
{
    public class DialogueUI : MonoBehaviour
    {
        #region Singleton

        public static DialogueUI instance { get; private set; }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            // Hide dialogue and interaction UI at awake
            dialogueWindow.SetActive(false);
            interactionUI.SetActive(false);
        }

        #endregion

        private DialogueManager currentDialogueManager;
        private bool typing;
        private string currentMessage;
        private float startDialogueDelayTimer;

        [Header("References")]
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private GameObject dialogueWindow;
        [SerializeField] private GameObject interactionUI;

        [Header("Settings")]
        [SerializeField] private bool animateText = true;

        [Range(0.1f, 1f)]
        [SerializeField] private float textAnimationSpeed = 0.5f;

        [Header("Next sentence input")]
        public KeyCode actionInput = KeyCode.Space;

        // Optional debug toggle
        [Header("Debug")]
        public bool enableDebugLogs = true;

        private void Update()
        {
            // Delay timer
            if (startDialogueDelayTimer > 0f)
            {
                startDialogueDelayTimer -= Time.deltaTime;
            }

            InputUpdate();
        }

        public virtual void InputUpdate()
        {
            // Only process input if:
            //  - a dialogue is active (dialogueWindow is visible) OR
            //  - we have a currentDialogueManager (processing dialogue)
            // This reduces accidental input when dialogue system isn't open.
            bool dialogueActive = (dialogueWindow != null && dialogueWindow.activeSelf) || currentDialogueManager != null;

            if (!dialogueActive)
            {
                return;
            }

            // Ensure game has focus (optional)
            if (!Application.isFocused)
            {
                if (enableDebugLogs) Debug.Log("[DialogueUI] Application not focused - ignoring input");
                return;
            }

            // Check input: support both old Input Manager and New Input System (if enabled)
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            // New Input System check
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    pressed = true;
                    if (enableDebugLogs) Debug.Log("[DialogueUI] Space detected via New Input System");
                }
            }
#endif
            // Old Input Manager fallback
            if (!pressed)
            {
                if (Input.GetKeyDown(actionInput))
                {
                    pressed = true;
                    if (enableDebugLogs) Debug.Log("[DialogueUI] Space detected via Input.GetKeyDown");
                }
            }

            if (pressed)
            {
                if (enableDebugLogs) Debug.Log($"[DialogueUI] NextSentenceSoft called (typing={typing}, delay={startDialogueDelayTimer:F3})");
                NextSentenceSoft();
            }
        }

        /// <summary>
        /// If a sentence is being written and this function is called, the sentence is completed instead of immediately moving to the next sentence.
        /// This function needs to be called twice if you want to switch to a new sentence.
        /// </summary>
        public void NextSentenceSoft()
        {
            if (startDialogueDelayTimer <= 0f)
            {
                if (!typing)
                {
                    if (enableDebugLogs) Debug.Log("[DialogueUI] NextSentenceHard() called from NextSentenceSoft");
                    NextSentenceHard();
                }
                else
                {
                    if (enableDebugLogs) Debug.Log("[DialogueUI] Stopping typing and showing full sentence");
                    StopAllCoroutines();
                    typing = false;
                    messageText.text = currentMessage;
                }
            }
            else
            {
                if (enableDebugLogs) Debug.Log($"[DialogueUI] Ignored input due to startDialogueDelayTimer ({startDialogueDelayTimer:F3}s remaining)");
            }
        }

        /// <summary>
        /// Even if a sentence is being written, with this function immediately moves to the next sentence.
        /// </summary>
        public void NextSentenceHard()
        {
            // Continue only if we have dialogue
            if (currentDialogueManager == null)
                return;

            // Tell the current dialogue manager to display the next sentence. This function also gives information if we are at the last sentence
            currentDialogueManager.NextSentence(out bool lastSentence);

            if (enableDebugLogs) Debug.Log($"[DialogueUI] NextSentenceHard invoked. lastSentence={lastSentence}");

            // If last sentence remove current dialogue manager
            if (lastSentence)
            {
                currentDialogueManager = null;
                // optionally hide the window here if you want:
                // dialogueWindow.SetActive(false);
            }
        }

        public void StartDialogue(DialogueManager _dialogueManager)
        {
            // Delay timer (make small so it doesn't block user input too long)
            startDialogueDelayTimer = 0.1f;

            // Store dialogue manager
            currentDialogueManager = _dialogueManager;

            // Start displaying dialogue
            currentDialogueManager.StartDialogue();
        }

        public void ShowSentence(DialogueCharacter _dialogueCharacter, string _message)
        {
            StopAllCoroutines();

            dialogueWindow.SetActive(true);

            portrait.sprite = _dialogueCharacter.characterPhoto;
            nameText.text = _dialogueCharacter.characterName;
            currentMessage = _message;

            if (animateText)
            {
                StartCoroutine(WriteTextToTextmesh(_message, messageText));
            }
            else
            {
                messageText.text = _message;
                typing = false; // ensure typing flag is false when we instantly set text
            }
        }

        public void ClearText()
        {
            dialogueWindow.SetActive(false);
        }

        public void ShowInteractionUI(bool _value)
        {
            interactionUI.SetActive(_value);
        }

        public bool IsProcessingDialogue()
        {
            if (currentDialogueManager != null)
            {
                return true;
            }

            return false;
        }

        public bool IsTyping()
        {
            return typing;
        }

        public int CurrentDialogueSentenceLenght()
        {
            if (currentDialogueManager == null)
                return 0;

            return currentDialogueManager.CurrentSentenceLenght();
        }

        IEnumerator WriteTextToTextmesh(string _text, TextMeshProUGUI _textMeshObject)
        {
            typing = true;

            _textMeshObject.text = "";
            char[] _letters = _text.ToCharArray();

            float _speed = 1f - textAnimationSpeed;

            foreach (char _letter in _letters)
            {
                _textMeshObject.text += _letter;

                if (_textMeshObject.text.Length == _letters.Length)
                {
                    typing = false;
                }

                yield return new WaitForSeconds(0.1f * _speed);
            }
        }
    }
}
