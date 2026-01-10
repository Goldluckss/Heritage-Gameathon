using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    InputManager inputManager;
    CameraManager cameraManager;
    PlayerLocomotion playerLocomotion;

    private void Awake()
    {
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
    }
}
