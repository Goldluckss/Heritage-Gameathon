using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using HeneGames.DialogueSystem; // Required for DialogueManager access

public class InputManager : MonoBehaviour
{
    PlayerControls playerControls;
    PlayerLocomotion playerLocomotion;
    AnimatorManager animatorManager;
    PlayerManager playerManager;

    PlayerControls.PlayerMovementActions playerActions;
    PlayerControls.PlayerActionsActions playerActionsActions;

    public Vector2 movementInput;
    public Vector2 cameraInput;

    public float cameraInputX;
    public float cameraInputY;

    public float moveAmount;
    public float verticalInput;
    public float horizontalInput;

    public bool shiftInput;
    public bool jumpInput;

    private void Awake()
    {
        playerControls = new PlayerControls();
        playerActions = playerControls.PlayerMovement;
        playerActionsActions = playerControls.PlayerActions;

        animatorManager = GetComponent<AnimatorManager>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
        playerManager = GetComponent<PlayerManager>();
    }

    private void OnEnable()
    {
        playerActions.Enable();
        playerActionsActions.Enable();

        playerActionsActions.Sprint.performed += OnSprintPerformed;
        playerActionsActions.Sprint.canceled += OnSprintCanceled;
        playerActionsActions.Jump.performed += OnJumpPerformed;

    }

    private void OnSprintPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        shiftInput = true;
    }

    private void OnSprintCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        shiftInput = false;
    }

    private void OnJumpPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        jumpInput = true;
    }

    private void Update()
    {
        // Don't read input during dialogue
        if (DialogueManager.IsDialogueActive)
        {
            // Zero out all inputs during dialogue
            movementInput = Vector2.zero;
            cameraInput = Vector2.zero;
            return;
        }

        movementInput = playerActions.Movement.ReadValue<Vector2>();
        cameraInput = playerActions.Camera.ReadValue<Vector2>();
    }

    private void OnDisable()
    {
        playerActions.Disable();
        playerActionsActions.Disable();

        playerActionsActions.Sprint.performed -= OnSprintPerformed;
        playerActionsActions.Sprint.canceled -= OnSprintCanceled;
    }

    private void OnDestroy()
    {
        playerControls?.Dispose();
    }

    public void HandleALLInput()
    {
        // Block all input handling during dialogue
        if (DialogueManager.IsDialogueActive)
        {
            // Reset movement values to stop the player
            verticalInput = 0f;
            horizontalInput = 0f;
            moveAmount = 0f;
            cameraInputX = 0f;
            cameraInputY = 0f;
            shiftInput = false;
            jumpInput = false;
            
            // Update animator to idle state
            playerLocomotion.isSprinting = false;
            animatorManager.UpdateAnimatorValues(0f, 0f, false, playerManager.isGrounded);
            return;
        }

        HandleMovementInput();
        HandleSprintingInput();
        HandleJumpingInput();
    }

    private void HandleMovementInput()
    {
        verticalInput = movementInput.y;
        horizontalInput = movementInput.x;

        cameraInputX = cameraInput.x;
        cameraInputY = cameraInput.y;

        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
    }

    private void HandleSprintingInput()
    {
        if (shiftInput && moveAmount > 0.5f)
        {
            playerLocomotion.isSprinting = true;
        }
        else
        {
            playerLocomotion.isSprinting = false;
        }
        
        animatorManager.UpdateAnimatorValues(horizontalInput, verticalInput, playerLocomotion.isSprinting, playerManager.isGrounded);
    }

    private void HandleJumpingInput()
    {         
        if (jumpInput)
        {
            jumpInput = false;
            playerLocomotion.HandleJumping();
        }
    }
}