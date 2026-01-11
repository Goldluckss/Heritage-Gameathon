using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HeneGames.DialogueSystem
{
    public class DialogueManager : MonoBehaviour
    {
        private int currentSentence;
        private float coolDownTimer;
        private bool dialogueIsOn;
        private DialogueTrigger dialogueTrigger;

        public enum TriggerState
        {
            Collision,
            Input
        }

        [Header("References")]
        [SerializeField] private AudioSource audioSource;

        [Header("Events")]
        public UnityEvent startDialogueEvent;
        public UnityEvent nextSentenceDialogueEvent;
        public UnityEvent endDialogueEvent;

        [Header("Dialogue")]
        [SerializeField] private TriggerState triggerState;
        [SerializeField] private List<NPC_Centence> sentences = new List<NPC_Centence>();

        private void Start()
        {
            Debug.Log("[DialogueManager] Start() - AudioSource assigned: " + (audioSource != null) + ", Sentences count: " + sentences.Count);
            if (audioSource != null)
            {
                Debug.Log("[DialogueManager] AudioSource GameObject: " + audioSource.gameObject.name + ", enabled: " + audioSource.enabled + ", volume: " + audioSource.volume);
            }
        }

        private void Update()
        {
            //Timer
            if(coolDownTimer > 0f)
            {
                coolDownTimer -= Time.deltaTime;
            }

            //Start dialogue by input
            if (Input.GetKeyDown(DialogueUI.instance.actionInput) && dialogueTrigger != null && !dialogueIsOn)
            {
                Debug.Log("[DialogueManager] Update() - Input detected, starting dialogue");
                
                //Trigger event inside DialogueTrigger component
                if (dialogueTrigger != null)
                {
                    dialogueTrigger.startDialogueEvent.Invoke();
                }

                startDialogueEvent.Invoke();

                //If component found start dialogue
                DialogueUI.instance.StartDialogue(this);

                //Hide interaction UI
                DialogueUI.instance.ShowInteractionUI(false);

                dialogueIsOn = true;
            }
        }

        //Start dialogue by trigger
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("[DialogueManager] OnTriggerEnter() - Collider: " + other.gameObject.name + ", triggerState: " + triggerState + ", dialogueIsOn: " + dialogueIsOn);
            
            if (triggerState == TriggerState.Collision && !dialogueIsOn)
            {
                //Try to find the "DialogueTrigger" component in the crashing collider
                if (other.gameObject.TryGetComponent<DialogueTrigger>(out DialogueTrigger _trigger))
                {
                    Debug.Log("[DialogueManager] OnTriggerEnter() - Found DialogueTrigger, starting dialogue via collision");
                    
                    //Trigger event inside DialogueTrigger component and store refenrece
                    dialogueTrigger = _trigger;
                    dialogueTrigger.startDialogueEvent.Invoke();

                    startDialogueEvent.Invoke();

                    //If component found start dialogue
                    DialogueUI.instance.StartDialogue(this);

                    dialogueIsOn = true;
                }
                else
                {
                    Debug.Log("[DialogueManager] OnTriggerEnter() - No DialogueTrigger component found on " + other.gameObject.name);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Debug.Log("[DialogueManager] OnTriggerEnter2D() - Collider: " + collision.gameObject.name);
            
            if (triggerState == TriggerState.Collision && !dialogueIsOn)
            {
                //Try to find the "DialogueTrigger" component in the crashing collider
                if (collision.gameObject.TryGetComponent<DialogueTrigger>(out DialogueTrigger _trigger))
                {
                    Debug.Log("[DialogueManager] OnTriggerEnter2D() - Found DialogueTrigger, starting dialogue");
                    
                    //Trigger event inside DialogueTrigger component and store refenrece
                    dialogueTrigger = _trigger;
                    dialogueTrigger.startDialogueEvent.Invoke();

                    startDialogueEvent.Invoke();

                    //If component found start dialogue
                    DialogueUI.instance.StartDialogue(this);

                    dialogueIsOn = true;
                }
            }
        }

        //Start dialogue by pressing DialogueUI action input
        private void OnTriggerStay(Collider other)
        {
            if (dialogueTrigger != null)
                return;

            if (triggerState == TriggerState.Input && dialogueTrigger == null)
            {
                //Try to find the "DialogueTrigger" component in the crashing collider
                if (other.gameObject.TryGetComponent<DialogueTrigger>(out DialogueTrigger _trigger))
                {
                    //Show interaction UI
                    DialogueUI.instance.ShowInteractionUI(true);

                    //Store refenrece
                    dialogueTrigger = _trigger;
                    Debug.Log("[DialogueManager] OnTriggerStay() - Found DialogueTrigger, showing interaction UI");
                }
            }
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (dialogueTrigger != null)
                return;

            if (triggerState == TriggerState.Input && dialogueTrigger == null)
            {
                //Try to find the "DialogueTrigger" component in the crashing collider
                if (collision.gameObject.TryGetComponent<DialogueTrigger>(out DialogueTrigger _trigger))
                {
                    //Show interaction UI
                    DialogueUI.instance.ShowInteractionUI(true);

                    //Store refenrece
                    dialogueTrigger = _trigger;
                    Debug.Log("[DialogueManager] OnTriggerStay2D() - Found DialogueTrigger, showing interaction UI");
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log("[DialogueManager] OnTriggerExit() - Collider: " + other.gameObject.name);
            
            //Try to find the "DialogueTrigger" component from the exiting collider
            if (other.gameObject.TryGetComponent<DialogueTrigger>(out DialogueTrigger _trigger))
            {
                Debug.Log("[DialogueManager] OnTriggerExit() - DialogueTrigger exited, stopping dialogue");
                
                //Hide interaction UI
                DialogueUI.instance.ShowInteractionUI(false);

                //Stop dialogue
                StopDialogue();
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Debug.Log("[DialogueManager] OnTriggerExit2D() - Collider: " + collision.gameObject.name);
            
            //Try to find the "DialogueTrigger" component from the exiting collider
            if (collision.gameObject.TryGetComponent<DialogueTrigger>(out DialogueTrigger _trigger))
            {
                Debug.Log("[DialogueManager] OnTriggerExit2D() - DialogueTrigger exited, stopping dialogue");
                
                //Hide interaction UI
                DialogueUI.instance.ShowInteractionUI(false);

                //Stop dialogue
                StopDialogue();
            }
        }

        public void StartDialogue()
        {
            Debug.Log("[DialogueManager] StartDialogue() called");
            Debug.Log("[DialogueManager] AudioSource null check: " + (audioSource == null ? "NULL!" : "OK - " + audioSource.gameObject.name));
            Debug.Log("[DialogueManager] Sentences count: " + sentences.Count);
            
            //Start event
            if(dialogueTrigger != null)
            {
                dialogueTrigger.startDialogueEvent.Invoke();
                Debug.Log("[DialogueManager] dialogueTrigger.startDialogueEvent invoked");
            }

            //Reset sentence index
            currentSentence = 0;

            if (sentences.Count == 0)
            {
                Debug.LogError("[DialogueManager] No sentences configured!");
                return;
            }

            //Show first sentence in dialogue UI
            ShowCurrentSentence();

            //Play dialogue sound
            Debug.Log("[DialogueManager] About to call PlaySound for sentence " + currentSentence);
            if (sentences[currentSentence].sentenceSound != null)
            {
                Debug.Log("[DialogueManager] Sentence sound clip: " + sentences[currentSentence].sentenceSound.name + ", length: " + sentences[currentSentence].sentenceSound.length + "s");
            }
            else
            {
                Debug.LogWarning("[DialogueManager] Sentence " + currentSentence + " has NO audio clip assigned!");
            }
            
            PlaySound(sentences[currentSentence].sentenceSound);

            //Cooldown timer
            coolDownTimer = sentences[currentSentence].skipDelayTime;
            Debug.Log("[DialogueManager] StartDialogue() completed, coolDownTimer: " + coolDownTimer);
        }

        public void NextSentence(out bool lastSentence)
        {
            Debug.Log("[DialogueManager] NextSentence() called, currentSentence: " + currentSentence + ", coolDownTimer: " + coolDownTimer);
            
            //The next sentence cannot be changed immediately after starting
            if (coolDownTimer > 0f)
            {
                Debug.Log("[DialogueManager] NextSentence() blocked by cooldown timer");
                lastSentence = false;
                return;
            }

            //Add one to sentence index
            currentSentence++;
            Debug.Log("[DialogueManager] Advanced to sentence " + currentSentence + " of " + sentences.Count);

            //Next sentence event
            if (dialogueTrigger != null)
            {
                dialogueTrigger.nextSentenceDialogueEvent.Invoke();
            }

            nextSentenceDialogueEvent.Invoke();

            //If last sentence stop dialogue and return
            if (currentSentence > sentences.Count - 1)
            {
                Debug.Log("[DialogueManager] Last sentence reached, stopping dialogue");
                StopDialogue();

                lastSentence = true;

                endDialogueEvent.Invoke();

                return;
            }

            //If not last sentence continue...
            lastSentence = false;

            //Play dialogue sound
            Debug.Log("[DialogueManager] Playing sound for sentence " + currentSentence);
            PlaySound(sentences[currentSentence].sentenceSound);

            //Show next sentence in dialogue UI
            ShowCurrentSentence();

            //Cooldown timer
            coolDownTimer = sentences[currentSentence].skipDelayTime;
        }

        public void StopDialogue()
        {
            Debug.Log("[DialogueManager] StopDialogue() called");
            
            //Stop dialogue event
            if (dialogueTrigger != null)
            {
                dialogueTrigger.endDialogueEvent.Invoke();
            }

            //Hide dialogue UI
            DialogueUI.instance.ClearText();

            //Stop audiosource so that the speaker's voice does not play in the background
            if(audioSource != null)
            {
                audioSource.Stop();
                Debug.Log("[DialogueManager] AudioSource stopped");
            }

            //Remove trigger refence
            dialogueIsOn = false;
            dialogueTrigger = null;
            Debug.Log("[DialogueManager] Dialogue state reset");
        }

        private void PlaySound(AudioClip _audioClip)
        {
            Debug.Log("[DialogueManager] PlaySound() called");
            
            //Play the sound only if it exists
            if (_audioClip == null)
            {
                Debug.LogWarning("[DialogueManager] PlaySound() - AudioClip is NULL, cannot play");
                return;
            }
            
            if (audioSource == null)
            {
                Debug.LogError("[DialogueManager] PlaySound() - AudioSource is NULL! Cannot play audio.");
                return;
            }

            Debug.Log("[DialogueManager] PlaySound() - AudioSource.enabled: " + audioSource.enabled);
            Debug.Log("[DialogueManager] PlaySound() - AudioSource.gameObject.activeInHierarchy: " + audioSource.gameObject.activeInHierarchy);
            Debug.Log("[DialogueManager] PlaySound() - AudioSource.volume: " + audioSource.volume);
            Debug.Log("[DialogueManager] PlaySound() - AudioSource.mute: " + audioSource.mute);
            Debug.Log("[DialogueManager] PlaySound() - AudioClip: " + _audioClip.name + ", length: " + _audioClip.length + "s");

            //Stop the audioSource so that the new sentence does not overlap with the old one
            audioSource.Stop();

            //Play sentence sound
            audioSource.PlayOneShot(_audioClip);
            Debug.Log("[DialogueManager] PlaySound() - PlayOneShot() called successfully");
            
            // Additional check after a frame
            StartCoroutine(CheckAudioPlayingDelayed());
        }

        private IEnumerator CheckAudioPlayingDelayed()
        {
            yield return null; // Wait one frame
            Debug.Log("[DialogueManager] Post-play check - AudioSource.isPlaying: " + (audioSource != null ? audioSource.isPlaying.ToString() : "NULL"));
        }

        private void ShowCurrentSentence()
        {
            Debug.Log("[DialogueManager] ShowCurrentSentence() - sentence index: " + currentSentence);
            
            if (sentences[currentSentence].dialogueCharacter != null)
            {
                Debug.Log("[DialogueManager] ShowCurrentSentence() - Character: " + sentences[currentSentence].dialogueCharacter.characterName);
                
                //Show sentence on the screen
                DialogueUI.instance.ShowSentence(sentences[currentSentence].dialogueCharacter, sentences[currentSentence].sentence);

                //Invoke sentence event
                sentences[currentSentence].sentenceEvent.Invoke();
            }
            else
            {
                Debug.Log("[DialogueManager] ShowCurrentSentence() - No character assigned, using empty");
                
                DialogueCharacter _dialogueCharacter = new DialogueCharacter();
                _dialogueCharacter.characterName = "";
                _dialogueCharacter.characterPhoto = null;

                DialogueUI.instance.ShowSentence(_dialogueCharacter, sentences[currentSentence].sentence);

                //Invoke sentence event
                sentences[currentSentence].sentenceEvent.Invoke();
            }
        }

        public int CurrentSentenceLenght()
        {
            if(sentences.Count <= 0)
                return 0;

            return sentences[currentSentence].sentence.Length;
        }
    }

    [System.Serializable]
    public class NPC_Centence
    {
        [Header("------------------------------------------------------------")]

        public DialogueCharacter dialogueCharacter;

        [TextArea(3, 10)]
        public string sentence;

        public float skipDelayTime = 0.5f;

        public AudioClip sentenceSound;

        public UnityEvent sentenceEvent;
    }
}