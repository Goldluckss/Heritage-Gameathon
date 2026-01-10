using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Optional event invoked when this checkpoint is activated (shown).")]
    public UnityEvent onActivated;

    [Tooltip("Optional event invoked when this checkpoint is reached (player entered).")]
    public UnityEvent onReached;

    [HideInInspector]
    public int index = -1; // set by manager

    // If you want activity separate from GameObject active, you can implement it here.
    public void Activate()
    {
        gameObject.SetActive(true);
        onActivated?.Invoke();
    }

    public void Deactivate()
    {
        // Make invisible/unavailable
        gameObject.SetActive(false);
    }

    private void Reset()
    {
        // ensure collider is a trigger by default
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // The user requested "body that enters these checkpoints must be name Player"
        if (other != null && other.gameObject != null && other.gameObject.name == "Player")
        {
            // Notify manager via static event or leave manager to check active state
            onReached?.Invoke();

            // If manager exists, let it handle deactivation & advancing.
            var manager = CheckpointManager.Instance;
            if (manager != null)
            {
                manager.HandleCheckpointReached(this);
            }
            else
            {
                // Fallback: just deactivate self
                Deactivate();
            }
        }
    }
}
