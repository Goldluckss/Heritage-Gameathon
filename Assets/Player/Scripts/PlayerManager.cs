using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    Animator animator;
    InputManager inputManager;
    CameraManager cameraManager;
    PlayerLocomotion playerLocomotion;

    public bool isInteracting;
    public bool isGrounded;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();
        cameraManager = FindFirstObjectByType<CameraManager>();
        playerLocomotion = GetComponent<PlayerLocomotion>();
    }

    private void Update()
    {
        inputManager.HandleALLInput();
    }

    private void FixedUpdate()
    {
        playerLocomotion.HandleALLMovement();
    }

    private void LateUpdate()
    {
        cameraManager.HandleALLCamearaMovement();

        isInteracting = animator.GetBool("isInteracting");
        playerLocomotion.isJumping = animator.GetBool("isJumping");
        animator.SetBool("isGrounded", playerLocomotion.isGrounded);

    }
}
