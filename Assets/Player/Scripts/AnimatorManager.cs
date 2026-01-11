using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorManager : MonoBehaviour
{
    public Animator animator;
    int horizontal;
    int vertical;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        horizontal = Animator.StringToHash("Horizontal");
        vertical = Animator.StringToHash("Vertical");
    }

    public void PlayTargetAnimation(string targetAnimation, bool isInteracting)
    {
        animator.SetBool("isInteracting", isInteracting);
        animator.CrossFade(targetAnimation, 0.2f);
    }

    public void UpdateAnimatorValues(float horizontalMovement, float verticalMovement, bool isSprinting, bool isGrounded)
    {
        // Animation Snapping
        float snappedHorizontal;
        float snappedVertical;

        #region Snapped Horizontal
        if (horizontalMovement > 0 && horizontalMovement < 0.55f)
        {
            snappedHorizontal = 0.5f;
        }
        else if (horizontalMovement >= 0.55f)
        {
            snappedHorizontal = 1;
        }
        else if (horizontalMovement < 0 && horizontalMovement > -0.55f)
        {
            snappedHorizontal = -0.5f;
        }
        else if (horizontalMovement <= -0.55f)
        {
            snappedHorizontal = -1;
        }
        else
        {
            snappedHorizontal = 0;
        }
        #endregion
        #region Snapped Vertical
        // Use absolute value to handle backward movement (blend tree expects 0-1 range)
        float absVerticalMovement = Mathf.Abs(verticalMovement);
        
        if (absVerticalMovement > 0 && absVerticalMovement < 0.55f)
        {
            snappedVertical = 0.5f;
        }
        else if (absVerticalMovement >= 0.55f)
        {
            snappedVertical = 1;
        }
        else
        {
            snappedVertical = 0;
        }
        #endregion

        if (isSprinting)
        {
            snappedHorizontal = horizontalMovement;
            snappedVertical = 2;
        }

        float dampingTime = isSprinting ? 0f : 0.1f;
        
        animator.SetFloat(horizontal, snappedHorizontal, dampingTime, Time.deltaTime);
        animator.SetFloat(vertical, snappedVertical, dampingTime, Time.deltaTime);
        animator.SetBool("isGrounded", isGrounded);
    }
}
