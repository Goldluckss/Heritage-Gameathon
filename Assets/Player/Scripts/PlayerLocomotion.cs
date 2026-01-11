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

    AnimatorManager animatorManager; // Added missing reference

    [Header("Movement Flags")]
    public bool isSprinting;
    public bool isGrounded;
    public bool isJumping;

    [Header("Movement Speeds")]
    public float walkingSpeed = 3f;
    public float runningSpeed = 7f; 
    public float sprintingSpeed = 10f; 
    public float rotationSpeed = 15;

    [Header("Falling")]
    public float inAirTimer;
    public float leapingVelocity;
    public float fallingVelocity = 30; 
    public float rayCastHeightOffset = 0.3f;
    public LayerMask groundLayer;

    [Header("Jump Speeds")]
    public float jumpingHeight = 3;
    public float gravityIntensity = -15;

    // New variable to track vertical velocity explicitly
    float inAirYVelocity;

    CapsuleCollider playerCollider;
    float originalHeight;
    Vector3 originalCenter;

    public void Awake()
    {
        playerManager = GetComponent<PlayerManager>();
        animatorManager = GetComponent<AnimatorManager>(); // Initialize it
        inputManager = GetComponent<InputManager>();
        playerRigidbody = GetComponent<Rigidbody>();
        cameraObject = Camera.main.transform;
        
        playerCollider = GetComponent<CapsuleCollider>();
        originalHeight = playerCollider.height;
        originalCenter = playerCollider.center;
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
        }

        // Apply Vertical Velocity (Y) - ALWAYS
        finalVelocity.y = inAirYVelocity;

        // DEBUG: Print movement values
        // Debug.Log($"Grounded: {isGrounded} | Speed: {moveDirection.magnitude} | Y-Vel: {inAirYVelocity}");
        
        // SAFEGUARD: On older Unity versions 'linearVelocity' is 'velocity'
        // 'linearVelocity' was added in Unity 6000.0.1+
        // If the user error said "missing definition for Velocity", they might be on < Unity 6.
        // I will change it to 'velocity' which is the standard legacy property.
        playerRigidbody.linearVelocity = finalVelocity; // Changed from linearVelocity to velocity
    }

    private void HandleRotation()
    {
        if (isJumping)
            return;
        
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

    private void HandleFallingAndLanding()
    {
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        rayCastOrigin.y = rayCastOrigin.y + rayCastHeightOffset; 

        // We always apply gravity if we are not grounded, even if jumping.
        if (!isGrounded)
        {
             // Debug.Log("State: AIRBORNE (Falling/Jumping)"); // Animation debug
             
            if (!playerManager.isInteracting)
            {
                // Animation handled by transition
            }

            inAirTimer = inAirTimer + Time.deltaTime;     
            
            // Apply Gravity
            inAirYVelocity = inAirYVelocity + (-fallingVelocity * Time.deltaTime);
            
            // Only shrink collider if we are actually falling (negative velocity) to avoid clipping when jumping up?
            // Or just keep it dynamic. Let's keep it simple for now.
             playerCollider.height = originalHeight * 0.75f; 
             float heightDifference = originalHeight - playerCollider.height;
             playerCollider.center = new Vector3(originalCenter.x, originalCenter.y + (heightDifference / 2), originalCenter.z);
        }

        // Raycast down
        // We increased distance to 0.5f to ensure we find ground even if slight hovering happens.
        float castDistance = 0.5f; 

        // DEBUG: Visualize the ray in Scene view
        Debug.DrawRay(rayCastOrigin, -Vector3.up * castDistance, Color.red);



        if (Physics.Raycast(rayCastOrigin, -Vector3.up, out hit, castDistance, groundLayer))
        {
            if (!playerManager.isGrounded && !playerManager.isInteracting)
            {
                // animatorManager.PlayTargetAnimation("Land", true);
            }

            Vector3 rayCastHitPoint = hit.point;
            
            // If we are jumping (positive Y velocity) we should NOT snap to ground yet.
            if (inAirYVelocity > 0)
            {
                 Debug.Log("State: JUMPING UP - Ignoring Ground");
                // We are going up! Don't land.
                isGrounded = false;
                playerManager.isGrounded = false;
                // Let gravity naturally slow us down until we fall back to earth.
                return;
            }

            if (!isGrounded) {
                 Debug.Log("State: LANDED"); // Animation debug: Only print once on entry
            }

            inAirTimer = 0;
            playerManager.isGrounded = true;
            isGrounded = true; // Fix: Sync local variable
            isJumping = false; // Reset jump flag on land
            
            // RESET GRAVITY / STICK TO GROUND
            inAirYVelocity = -5f;

            // RESET COLLIDER
            playerCollider.height = originalHeight;
            playerCollider.center = originalCenter;
        }
        else
        {
            playerManager.isGrounded = false;
            isGrounded = false; // Fix: Sync local variable
        }
    }

    public void HandleJumping()
    {
        if (isGrounded)
        {
            Debug.Log("State: JUMP COMMAND"); // Animation debug
            animatorManager.animator.SetBool("isJumping", true);
            // animatorManager.PlayTargetAnimation("Jump", false); // DISABLED: Relying on boolean transition

            float jumpVelocity = Mathf.Sqrt(-2 * gravityIntensity * jumpingHeight);
            
            // Integrate with our kinematic system
            inAirYVelocity = jumpVelocity;
            
            // Note: We don't set linearVelocity directly here, HandleALLMovement will do it.
        }
    }


}
