using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Individual spot for MultiSpotCheckpoint.
/// Attach this to each spot GameObject with a trigger collider.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CheckpointSpot : MonoBehaviour
{
    [Header("Spot Settings")]
    [Tooltip("Is this the correct spot?")]
    public bool isCorrectSpot = false;

    [Tooltip("Tag of the player object")]
    public string playerTag = "Player";

    [Header("Events")]
    public UnityEvent onSpotReached;

    [HideInInspector]
    public MultiSpotCheckpoint parentCheckpoint;

    private bool hasBeenTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered) return;

        if (other.CompareTag(playerTag))
        {
            hasBeenTriggered = true;
            onSpotReached?.Invoke();

            // Notify parent checkpoint
            if (parentCheckpoint != null)
            {
                parentCheckpoint.OnSpotReached(this);
            }
        }
    }

    // 2D version
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenTriggered) return;

        if (other.CompareTag(playerTag))
        {
            hasBeenTriggered = true;
            onSpotReached?.Invoke();

            // Notify parent checkpoint
            if (parentCheckpoint != null)
            {
                parentCheckpoint.OnSpotReached(this);
            }
        }
    }

    /// <summary>
    /// Reset the spot so it can be triggered again (used when checkpoint reactivates)
    /// </summary>
    public void ResetSpot()
    {
        hasBeenTriggered = false;
    }
}