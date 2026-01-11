using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    // singleton for easy access from Checkpoint
    public static CheckpointManager Instance { get; private set; }

    [Tooltip("Ordered list of checkpoints. Can be regular Checkpoint or MultiSpotCheckpoint.")]
    public List<GameObject> checkpoints = new List<GameObject>();

    [Tooltip("Automatically activate the first checkpoint on Start.")]
    public bool playOnStart = true;

    private int currentIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple CheckpointManagers in scene. Destroying duplicate.");
            Destroy(this);
            return;
        }
        Instance = this;

        // Give checkpoints their indices
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
            {
                // Check if it's a regular Checkpoint
                var regularCP = checkpoints[i].GetComponent<Checkpoint>();
                if (regularCP != null)
                {
                    regularCP.index = i;
                }

                // Check if it's a MultiSpotCheckpoint
                var multiCP = checkpoints[i].GetComponent<MultiSpotCheckpoint>();
                if (multiCP != null)
                {
                    multiCP.index = i;
                }
            }
        }
    }

    private void Start()
    {
        // If starting, deactivate all then activate first
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
                checkpoints[i].SetActive(false);
        }

        if (playOnStart && checkpoints.Count > 0)
        {
            ActivateCheckpoint(0);
        }
    }

    /// <summary>
    /// Activates the checkpoint at index (makes it visible/active) and updates currentIndex.
    /// Safe if index out of range (does nothing).
    /// </summary>
    public void ActivateCheckpoint(int index)
    {
        if (index < 0 || index >= checkpoints.Count) return;

        currentIndex = index;
        var cpObject = checkpoints[index];
        
        if (cpObject != null)
        {
            // Try regular Checkpoint first
            var regularCP = cpObject.GetComponent<Checkpoint>();
            if (regularCP != null)
            {
                regularCP.Activate();
                return;
            }

            // Try MultiSpotCheckpoint
            var multiCP = cpObject.GetComponent<MultiSpotCheckpoint>();
            if (multiCP != null)
            {
                multiCP.Activate();
                return;
            }

            // Fallback: just activate the GameObject
            cpObject.SetActive(true);
        }
    }

    /// <summary>
    /// Called by regular Checkpoint when reached. Manager will deactivate it and activate next in list (if any).
    /// </summary>
    public void HandleCheckpointReached(Checkpoint checkpoint)
    {
        if (checkpoint == null) return;

        // Only respond if this checkpoint is the current active one
        if (checkpoint.index != currentIndex)
        {
            // not the current checkpoint (ignore)
            return;
        }

        // Deactivate current
        checkpoint.Deactivate();

        // Advance to next
        AdvanceToNextCheckpoint();
    }

    /// <summary>
    /// Called by MultiSpotCheckpoint when the correct spot is reached.
    /// </summary>
    public void HandleMultiSpotCheckpointReached(MultiSpotCheckpoint checkpoint)
    {
        if (checkpoint == null) return;

        // Only respond if this checkpoint is the current active one
        if (checkpoint.index != currentIndex)
        {
            return;
        }

        // Deactivate current
        checkpoint.Deactivate();

        // Advance to next
        AdvanceToNextCheckpoint();
    }

    /// <summary>
    /// Advances to the next checkpoint in the list.
    /// </summary>
    private void AdvanceToNextCheckpoint()
    {
        int next = currentIndex + 1;
        if (next < checkpoints.Count)
        {
            ActivateCheckpoint(next);
        }
        else
        {
            // Reached last checkpoint. Option: loop, stop, or fire event.
            currentIndex = -1;
            Debug.Log("All checkpoints reached.");
        }
    }

    /// <summary>
    /// Public helper to add a checkpoint at runtime (keeps indices consistent).
    /// </summary>
    public void AddCheckpoint(GameObject cpObject)
    {
        if (cpObject == null) return;
        if (!checkpoints.Contains(cpObject))
        {
            checkpoints.Add(cpObject);
            int newIndex = checkpoints.Count - 1;

            // Assign index to whichever component exists
            var regularCP = cpObject.GetComponent<Checkpoint>();
            if (regularCP != null) regularCP.index = newIndex;

            var multiCP = cpObject.GetComponent<MultiSpotCheckpoint>();
            if (multiCP != null) multiCP.index = newIndex;
        }
    }

    /// <summary>
    /// Remove checkpoint (updates indices).
    /// </summary>
    public void RemoveCheckpoint(GameObject cpObject)
    {
        if (cpObject == null) return;
        if (checkpoints.Remove(cpObject))
        {
            // reassign indices
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] != null)
                {
                    var regularCP = checkpoints[i].GetComponent<Checkpoint>();
                    if (regularCP != null) regularCP.index = i;

                    var multiCP = checkpoints[i].GetComponent<MultiSpotCheckpoint>();
                    if (multiCP != null) multiCP.index = i;
                }
            }
            // Adjust currentIndex if needed
            if (currentIndex >= checkpoints.Count) currentIndex = checkpoints.Count - 1;
        }
    }
}