using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	PlayerControls playerControls;
	PlayerLocomotion playerLocomotion;
    AnimatorManager animatorManager;
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

    private void Awake()
	{
		playerControls = new PlayerControls();
		playerActions = playerControls.PlayerMovement;
		playerActionsActions = playerControls.PlayerActions;

		animatorManager = GetComponent<AnimatorManager>();
		playerLocomotion = GetComponent<PlayerLocomotion>();
    }

	private void OnEnable()
	{
		playerActions.Enable();
		playerActionsActions.Enable();

		// Register Sprint callbacks
		playerActionsActions.Sprint.performed += OnSprintPerformed;
		playerActionsActions.Sprint.canceled += OnSprintCanceled;
    }

	private void OnSprintPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
	{
		shiftInput = true;
	}

	private void OnSprintCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
	{
		shiftInput = false;
	}

	private void Update()
	{
		movementInput = playerActions.Movement.ReadValue<Vector2>();
		cameraInput = playerActions.Camera.ReadValue<Vector2>();
	}

	private void OnDisable()
	{
		playerActions.Disable();
		playerActionsActions.Disable();

		// Unregister Sprint callbacks
		playerActionsActions.Sprint.performed -= OnSprintPerformed;
		playerActionsActions.Sprint.canceled -= OnSprintCanceled;
	}

	private void OnDestroy()
	{
		playerControls?.Dispose();
	}

	public void HandleALLInput()
	{
		HandleMovementInput();
		HandleSprintingInput();
        //HandleJumpingInput
        //HandleActionInput
    }

    private void HandleMovementInput()
	{
		verticalInput = movementInput.y;
		horizontalInput = movementInput.x;

		cameraInputX = cameraInput.x;
		cameraInputY = cameraInput.y;

        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
        animatorManager.UpdateAnimatorValues(0, moveAmount);
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
    }
}
