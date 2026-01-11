using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    PlayerManager playerManager;
    InputManager inputManager;

    Vector3 moveDirection;
    Transform cameraObject;
    Rigidbody playerRigidbody;

    public bool isSprinting;

    [Header("Movement Speeds")]
    public float walkingSpeed = 3f; // Increased from 1.5
    public float runningSpeed = 7f; // Increased from 5
    public float sprintingSpeed = 10f; // Increased from 7
    public float rotationSpeed = 15;

    [Header("Falling")]
    public float inAirTimer;
    public float leapingVelocity;
    public float fallingVelocity = 30; 
    public float rayCastHeightOffset = 0.5f;
    public LayerMask groundLayer;
    
    // New variable to track vertical velocity explicitly
    float inAirYVelocity;

    CapsuleCollider playerCollider;
    float originalHeight;
    Vector3 originalCenter;

    public void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
        inputManager = GetComponent<InputManager>();
        playerRigidbody = GetComponent<Rigidbody>();
        cameraObject = Camera.main.transform;
        
        playerCollider = GetComponent<CapsuleCollider>();
        originalHeight = playerCollider.height;
        originalCenter = playerCollider.center;
    }

    // ... (HandleALLMovement and HandleMovement remain same) ...

    private void HandleFallingAndLanding()
    {
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        // Lower origin to 0.2f for better precision near feet
        rayCastOrigin.y = rayCastOrigin.y + 0.2f; 

        if (!playerManager.isGrounded)
        {
            if (!playerManager.isInteracting)
            {
                // Falling Animation logic
            }

            inAirTimer = inAirTimer + Time.deltaTime;
            
            // GRAVITY: Decrease Y velocity over time
            inAirYVelocity = inAirYVelocity + (-fallingVelocity * Time.deltaTime);
            
            // COLLIDER ADJUSTMENT: Shrink and Raise
            // Retract the bottom of the capsule so we don't hit edges early given the leg lift
             playerCollider.height = originalHeight * 0.75f; // Shrink to 75%
             // Raise center so the top stays roughly same/middle moves up, effectively pulling bottom up
             float heightDifference = originalHeight - playerCollider.height;
             playerCollider.center = new Vector3(originalCenter.x, originalCenter.y + (heightDifference / 2), originalCenter.z);
        }

        // Raycast down from 0.2f. 
        float castDistance = 0.25f; 

        // DEBUG: Visualize the ray in Scene view
        Debug.DrawRay(rayCastOrigin, -Vector3.up * castDistance, Color.red);

        if (Physics.Raycast(rayCastOrigin, -Vector3.up, out hit, castDistance, groundLayer))
        {
            if (!playerManager.isGrounded && !playerManager.isInteracting)
            {
                // animatorManager.PlayTargetAnimation("Land", true);
            }

            Vector3 rayCastHitPoint = hit.point;
            
            inAirTimer = 0;
            playerManager.isGrounded = true;
            
            // RESET GRAVITY / STICK TO GROUND
            inAirYVelocity = -5f;

            // RESET COLLIDER
            playerCollider.height = originalHeight;
            playerCollider.center = originalCenter;
        }
        else
        {
            playerManager.isGrounded = false;
        }
    }

    public void HandleALLMovement()
    {
        HandleFallingAndLanding();

        // Separate Input-based Movement from Gravity/Physics
        // Gravity/Y-Velocity must be applied ALWAYS.
        // Input/X-Z Velocity is only applied if not interacting.
        
        Vector3 finalVelocity = Vector3.zero;

        if (!playerManager.isInteracting)
        {
            HandleRotation();
            
            // Calculate Horizontal Movement (X/Z)
            moveDirection = cameraObject.forward * inputManager.verticalInput;
            moveDirection = moveDirection + cameraObject.right * inputManager.horizontalInput;
            moveDirection.Normalize();
            moveDirection.y = 0;

            float speed = walkingSpeed;
            // Check Grounded for Sprint logic
            if (playerManager.isGrounded)
            {
                if (isSprinting && inputManager.moveAmount > 0.5f)
                {
                    speed = sprintingSpeed;
                }
                else if (inputManager.moveAmount >= 0.5f)
                {
                    speed = runningSpeed;
                }
            }
            else
            {
                speed = walkingSpeed;
            }

            moveDirection = moveDirection * speed;
            finalVelocity = moveDirection;
        }
        else
        {
            // If interacting (e.g. Landing), kill horizontal velocity (or keep it zero)
            // unless we want root motion support (not enabled here yet).
            // finalVelocity x/z remains 0.
        }

        // Apply Vertical Velocity (Y) - ALWAYS
        finalVelocity.y = inAirYVelocity;

        // Apply Final Velocity
        playerRigidbody.linearVelocity = finalVelocity;
    }

    // Removed HandleMovement since it's now integrated into HandleALLMovement for better flow control
    // Kept HandleRotation distinct.

    private void HandleRotation()
    {
        Vector3 targetDirection = Vector3.zero;

        targetDirection = cameraObject.forward * inputManager.verticalInput;
        targetDirection = targetDirection + cameraObject.right * inputManager.horizontalInput;
        targetDirection.Normalize();
        targetDirection.y = 0;

        if (targetDirection == Vector3.zero)
            targetDirection = transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        transform.rotation = playerRotation;
    }


}
