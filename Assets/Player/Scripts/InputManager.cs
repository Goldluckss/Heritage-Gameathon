using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	PlayerControls playerControls;
	PlayerControls.PlayerActions playerActions;

	public Vector2 movementInput;
	public float verticalInput;
	public float horizontalInput;

    private void Awake()
	{
		playerControls = new PlayerControls();
		playerActions = playerControls.Player;
	}

	private void OnEnable()
	{
		playerActions.Enable();
	}

	private void Update()
	{
		movementInput = playerActions.Move.ReadValue<Vector2>();
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
    }
}
