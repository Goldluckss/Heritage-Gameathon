using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A special checkpoint with multiple spots where only one is correct.
/// Wrong spots disappear when touched, correct spot advances to next checkpoint.
/// </summary>
public class MultiSpotCheckpoint : MonoBehaviour
{
    [Header("Spots")]
    [Tooltip("List of all spot GameObjects. One should be marked as correct.")]
    public List<CheckpointSpot> spots = new List<CheckpointSpot>();

    [Header("Checkpoint Settings")]
    [Tooltip("Index assigned by CheckpointManager")]
    public int index = -1;

    [Header("Events")]
    public UnityEvent onActivated;
    public UnityEvent onCorrectSpotReached;
    public UnityEvent onWrongSpotReached;

    private bool isActive = false;

    private void Awake()
    {
        // Register this checkpoint with each spot
        foreach (var spot in spots)
        {
            if (spot != null)
            {
                spot.parentCheckpoint = this;
            }
        }
    }

    /// <summary>
    /// Called by CheckpointManager to activate this checkpoint.
    /// Shows all spots.
    /// </summary>
    public void Activate()
    {
        isActive = true;
        gameObject.SetActive(true);

        // Activate all spots
        foreach (var spot in spots)
        {
            if (spot != null)
            {
                spot.gameObject.SetActive(true);
                spot.ResetSpot();
            }
        }

        onActivated?.Invoke();
        Debug.Log($"[MultiSpotCheckpoint] Activated with {spots.Count} spots");
    }

    /// <summary>
    /// Called by CheckpointManager to deactivate this checkpoint.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;

        // Deactivate all spots
        foreach (var spot in spots)
        {
            if (spot != null)
            {
                spot.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(false);
        Debug.Log("[MultiSpotCheckpoint] Deactivated");
    }

    /// <summary>
    /// Called by CheckpointSpot when player reaches it.
    /// </summary>
    public void OnSpotReached(CheckpointSpot spot)
    {
        if (!isActive) return;

        if (spot.isCorrectSpot)
        {
            Debug.Log("[MultiSpotCheckpoint] Correct spot reached!");
            onCorrectSpotReached?.Invoke();

            // Hide all spots
            foreach (var s in spots)
            {
                if (s != null)
                {
                    s.gameObject.SetActive(false);
                }
            }

            // Notify manager to advance to next checkpoint
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.HandleMultiSpotCheckpointReached(this);
            }
        }
        else
        {
            Debug.Log("[MultiSpotCheckpoint] Wrong spot reached - hiding spot");
            onWrongSpotReached?.Invoke();

            // Just hide this wrong spot
            spot.gameObject.SetActive(false);
        }
    }
}