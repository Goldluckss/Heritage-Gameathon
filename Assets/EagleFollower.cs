using UnityEngine;

public class EagleFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public float followDistance = 3f;
    public float heightOffset = 2f;
    public float sideOffset = 1.5f;
    public float followSpeed = 6f;
    public float rotationSpeed = 8f;

    [Header("Animation")]
    public RuntimeAnimatorController animatorController;
    public string flyAnimationName = "Fly";

    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();

        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }
    }

    void Start()
    {
        if (animator != null)
        {
            animator.Play(flyAnimationName, 0, 0f);
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Base position behind + up
        Vector3 basePosition =
            player.position
            - player.forward * followDistance
            + Vector3.up * heightOffset;

        // Candidate positions
        Vector3 leftPosition  = basePosition - player.right * sideOffset;
        Vector3 rightPosition = basePosition + player.right * sideOffset;

        // Choose whichever is closer to current position
        float distToLeft  = (transform.position - leftPosition).sqrMagnitude;
        float distToRight = (transform.position - rightPosition).sqrMagnitude;

        Vector3 targetPosition =
            distToLeft < distToRight ? leftPosition : rightPosition;

        // Smooth movement
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        // Face forward with the player
        Quaternion targetRotation = Quaternion.LookRotation(player.forward);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
