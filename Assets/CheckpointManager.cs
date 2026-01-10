using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    // singleton for easy access from Checkpoint
    public static CheckpointManager Instance { get; private set; }

    [Tooltip("Ordered list of checkpoints. First one will be activated at Start if playOnStart is true.")]
    public List<Checkpoint> checkpoints = new List<Checkpoint>();

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
                checkpoints[i].index = i;
        }
    }

    private void Start()
    {
        // If starting, deactivate all then activate first
        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] != null)
                checkpoints[i].gameObject.SetActive(false);
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
        var cp = checkpoints[index];
        if (cp != null)
            cp.Activate();
    }

    /// <summary>
    /// Called by Checkpoint when reached. Manager will deactivate it and activate next in list (if any).
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
    public void AddCheckpoint(Checkpoint cp)
    {
        if (cp == null) return;
        if (!checkpoints.Contains(cp))
        {
            checkpoints.Add(cp);
            cp.index = checkpoints.Count - 1;
        }
    }

    /// <summary>
    /// Remove checkpoint (updates indices).
    /// </summary>
    public void RemoveCheckpoint(Checkpoint cp)
    {
        if (cp == null) return;
        if (checkpoints.Remove(cp))
        {
            // reassign indices
            for (int i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i] != null) checkpoints[i].index = i;
            }
            // Adjust currentIndex if needed
            if (currentIndex >= checkpoints.Count) currentIndex = checkpoints.Count - 1;
        }
    }
}
