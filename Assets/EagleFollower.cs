using UnityEngine;

public class EagleFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public float followDistance = 3f;
    public float heightOffset = 2f;
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
        // Force fly animation
        if (animator != null)
        {
            animator.Play(flyAnimationName, 0, 0f);
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPosition =
            player.position
            - player.forward * followDistance
            + Vector3.up * heightOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
