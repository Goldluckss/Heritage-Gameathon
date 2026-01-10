using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	PlayerControls playerControls;
	AnimatorManager animatorManager;
	PlayerControls.PlayerActions playerActions;

	public Vector2 movementInput;
	public Vector2 cameraInput;

	public float cameraInputX;
	public float cameraInputY;

    private float moveAmount;
    public float verticalInput;
	public float horizontalInput;

    private void Awake()
	{
		playerControls = new PlayerControls();
		playerActions = playerControls.Player;

		animatorManager = GetComponent<AnimatorManager>();
    }

	private void OnEnable()
	{
		playerActions.Enable();
    }

	private void Update()
	{
		movementInput = playerActions.Move.ReadValue<Vector2>();
		cameraInput = playerActions.Camera.ReadValue<Vector2>();
	}

	private void OnDisable()
	{
		playerActions.Disable();
	}

	private void OnDestroy()
	{
		playerControls?.Dispose();
	}

	public void HandleALLInput()
	{
		HandleMovementInput();
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
}
