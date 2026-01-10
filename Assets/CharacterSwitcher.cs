using UnityEngine;
using UnityEngine.InputSystem;
using FlyingSystem;

public class CharacterSwitcher : MonoBehaviour
{
    [Header("References")]
    public GameObject player;
    public GameObject eagle;
    public CameraManager cameraManager;
    public Transform cameraPivot;
    public Camera mainCamera;

    [Header("Eagle Camera Settings")]
    public float eagleCameraFollowSpeed = 10f;
    public float eagleCameraLookSpeed = 100f;
    public float eagleCameraPivotSpeed = 80f;
    public float cameraDistance = 1f;
    public float cameraHeight = 3f;
    public float minPivotAngle = -30f;
    public float maxPivotAngle = 60f;

    [Header("Eagle Flight Settings")]
    public float turnSpeed = 60f;
    public float pitchSpeed = 40f;
    public float maxPitchAngle = 50f;
    public float bankAngle = 35f;
    public float bankSpeed = 5f;
    public float baseFlightSpeed = 15f;
    public float boostMultiplier = 2f;

    [Header("Eagle Return Settings")]
    public float returnSpeed = 20f;
    public float returnRotationSpeed = 5f;

    [Header("Current State")]
    public bool controllingEagle = false;
    public bool eagleReturning = false;

    // Component references
    private PlayerManager playerManager;
    private InputManager playerInputManager;
    private PlayerLocomotion playerLocomotion;

    private EagleFollower eagleFollower;
    private CreatureFlyingSystem eagleFlyingSystem;
    private EagleController eagleController;
    private Rigidbody eagleRigidbody;

    // Eagle control variables
    private Vector2 eagleMovementInput;
    private Vector2 eagleCameraInput;
    private bool eagleBoosting = false;

    // Camera orbit variables
    private float cameraOrbitYaw = 0f;
    private float cameraOrbitPitch = 20f;

    // Flight variables
    private float currentSpeed = 0f;
    private float currentPitch = 0f;
    private float currentYaw = 0f;
    private float currentBank = 0f;

    // Camera smoothing
    private Vector3 cameraPositionVelocity = Vector3.zero;

    // Store original camera state
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private Transform originalCameraParent;
    private float originalCameraManagerLookAngle;
    private float originalCameraManagerPivotAngle;

    // Input System
    private PlayerControls playerControls;

    void Awake()
    {
        if (player != null)
        {
            playerManager = player.GetComponent<PlayerManager>();
            playerInputManager = player.GetComponent<InputManager>();
            playerLocomotion = player.GetComponent<PlayerLocomotion>();
        }

        if (eagle != null)
        {
            eagleFollower = eagle.GetComponent<EagleFollower>();
            eagleFlyingSystem = eagle.GetComponent<CreatureFlyingSystem>();
            eagleController = eagle.GetComponent<EagleController>();
            eagleRigidbody = eagle.GetComponent<Rigidbody>();
        }

        playerControls = new PlayerControls();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Store original camera setup
        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.transform.parent;
            originalCameraLocalPosition = mainCamera.transform.localPosition;
            originalCameraLocalRotation = mainCamera.transform.localRotation;
        }

        if (cameraManager != null)
        {
            originalCameraManagerLookAngle = cameraManager.lookAngle;
            originalCameraManagerPivotAngle = cameraManager.pivotAngle;
        }
    }

    void OnEnable()
    {
        playerControls.Enable();
    }

    void OnDisable()
    {
        playerControls.Disable();
    }

    void OnDestroy()
    {
        playerControls?.Dispose();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            if (!eagleReturning) // Don't allow switching while eagle is returning
            {
                ToggleControl();
            }
        }

        if (controllingEagle)
        {
            ReadEagleInput();
            HandleCameraOrbitInput();
        }

        if (eagleReturning)
        {
            HandleEagleReturn();
        }
    }

    void FixedUpdate()
    {
        if (controllingEagle && !eagleReturning)
        {
            HandleEagleMovement();
        }
    }

    void LateUpdate()
    {
        if (controllingEagle && !eagleReturning)
        {
            HandleEagleCamera();
        }
    }

    void ToggleControl()
    {
        controllingEagle = !controllingEagle;

        if (controllingEagle)
        {
            SwitchToEagle();
        }
        else
        {
            SwitchToPlayer();
        }
    }

    void SwitchToEagle()
    {
        Debug.Log("Switching to Eagle");

        // Store current camera manager angles before switching
        if (cameraManager != null)
        {
            originalCameraManagerLookAngle = cameraManager.lookAngle;
            originalCameraManagerPivotAngle = cameraManager.pivotAngle;
        }

        // Disable player
        if (playerManager != null) playerManager.enabled = false;
        if (playerInputManager != null) playerInputManager.enabled = false;
        if (playerLocomotion != null) playerLocomotion.enabled = false;
        if (cameraManager != null) cameraManager.enabled = false;

        // Disable eagle auto-systems
        if (eagleFollower != null) eagleFollower.enabled = false;
        if (eagleController != null) eagleController.enabled = false;
        if (eagleFlyingSystem != null) eagleFlyingSystem.enabledFlyingLogic = false;

        // Setup rigidbody
        if (eagleRigidbody != null)
        {
            eagleRigidbody.useGravity = false;
            eagleRigidbody.linearDamping = 0f;
            eagleRigidbody.angularDamping = 0f;
            eagleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            eagleRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            eagleRigidbody.linearVelocity = Vector3.zero;
        }

        // Initialize from eagle's current state
        Vector3 eagleRotation = eagle.transform.eulerAngles;
        currentYaw = eagleRotation.y;
        currentPitch = NormalizeAngle(eagleRotation.x);
        currentBank = 0f;
        currentSpeed = baseFlightSpeed;

        // Initialize camera behind eagle
        cameraOrbitYaw = 0f;
        cameraOrbitPitch = 20f;

        // Detach camera from parent for free movement
        if (mainCamera != null)
        {
            mainCamera.transform.SetParent(null);
            mainCamera.transform.position = CalculateCameraPosition();
            mainCamera.transform.LookAt(eagle.transform.position + Vector3.up * 0.5f);
        }
    }

    void SwitchToPlayer()
    {
        Debug.Log("Switching to Player");

        // Start eagle return sequence
        eagleReturning = true;
        controllingEagle = false;

        // Stop eagle movement
        if (eagleRigidbody != null)
        {
            eagleRigidbody.linearVelocity = Vector3.zero;
            eagleRigidbody.angularVelocity = Vector3.zero;
        }

        // Restore camera to original parent and position
        RestoreCamera();

        // Enable player controls
        if (playerManager != null) playerManager.enabled = true;
        if (playerInputManager != null) playerInputManager.enabled = true;
        if (playerLocomotion != null) playerLocomotion.enabled = true;

        // Enable camera manager with original angles
        if (cameraManager != null)
        {
            cameraManager.enabled = true;
            cameraManager.targetTransform = player.transform;
            cameraManager.lookAngle = originalCameraManagerLookAngle;
            cameraManager.pivotAngle = originalCameraManagerPivotAngle;
        }
    }

    void RestoreCamera()
    {
        if (mainCamera == null) return;

        // Reparent camera to original parent
        mainCamera.transform.SetParent(originalCameraParent);

        // Reset local position and rotation
        mainCamera.transform.localPosition = originalCameraLocalPosition;
        mainCamera.transform.localRotation = originalCameraLocalRotation;
    }

    void HandleEagleReturn()
    {
        if (eagle == null || player == null) return;

        // Calculate target position (behind and above player, like EagleFollower)
        float followDistance = 3f;
        float heightOffset = 2f;

        if (eagleFollower != null)
        {
            followDistance = eagleFollower.followDistance;
            heightOffset = eagleFollower.heightOffset;
        }

        Vector3 targetPosition = player.transform.position
            - player.transform.forward * followDistance
            + Vector3.up * heightOffset;

        // Move eagle toward target
        float distanceToTarget = Vector3.Distance(eagle.transform.position, targetPosition);

        if (distanceToTarget > 0.5f)
        {
            // Move toward player
            Vector3 direction = (targetPosition - eagle.transform.position).normalized;
            
            if (eagleRigidbody != null)
            {
                eagleRigidbody.linearVelocity = direction * returnSpeed;
            }
            else
            {
                eagle.transform.position = Vector3.MoveTowards(
                    eagle.transform.position,
                    targetPosition,
                    returnSpeed * Time.deltaTime
                );
            }

            // Rotate eagle to face movement direction
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                eagle.transform.rotation = Quaternion.Slerp(
                    eagle.transform.rotation,
                    targetRotation,
                    returnRotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // Eagle has arrived, finish return sequence
            FinishEagleReturn();
        }
    }

    void FinishEagleReturn()
    {
        Debug.Log("Eagle returned to player");

        eagleReturning = false;

        // Reset rigidbody
        if (eagleRigidbody != null)
        {
            eagleRigidbody.linearVelocity = Vector3.zero;
            eagleRigidbody.angularVelocity = Vector3.zero;
            eagleRigidbody.interpolation = RigidbodyInterpolation.None;
            eagleRigidbody.constraints = RigidbodyConstraints.None;
        }

        // Level out eagle rotation
        if (eagle != null)
        {
            Vector3 currentRotation = eagle.transform.eulerAngles;
            eagle.transform.rotation = Quaternion.Euler(0f, currentRotation.y, 0f);
        }

        // Enable eagle follower to resume following
        if (eagleFollower != null)
        {
            eagleFollower.enabled = true;
        }
    }

    void ReadEagleInput()
    {
        eagleMovementInput = playerControls.PlayerMovement.Movement.ReadValue<Vector2>();
        eagleCameraInput = playerControls.PlayerMovement.Camera.ReadValue<Vector2>();

        if (Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            eagleBoosting = !eagleBoosting;
            Debug.Log("Boost: " + (eagleBoosting ? "ON" : "OFF"));
        }
    }

    void HandleCameraOrbitInput()
    {
        cameraOrbitYaw += eagleCameraInput.x * eagleCameraLookSpeed * Time.deltaTime;
        cameraOrbitPitch -= eagleCameraInput.y * eagleCameraPivotSpeed * Time.deltaTime;
        cameraOrbitPitch = Mathf.Clamp(cameraOrbitPitch, minPivotAngle, maxPivotAngle);

        if (cameraOrbitYaw > 360f) cameraOrbitYaw -= 360f;
        if (cameraOrbitYaw < -360f) cameraOrbitYaw += 360f;
    }

    void HandleEagleMovement()
    {
        if (eagle == null || eagleRigidbody == null) return;

        float dt = Time.fixedDeltaTime;

        // --- TURNING (A/D) ---
        float turnInput = eagleMovementInput.x;
        currentYaw += turnInput * turnSpeed * dt;

        // --- PITCH (W/S) ---
        float pitchInput = -eagleMovementInput.y;
        float targetPitch = currentPitch + pitchInput * pitchSpeed * dt;
        currentPitch = Mathf.Clamp(targetPitch, -maxPitchAngle, maxPitchAngle);

        if (Mathf.Abs(eagleMovementInput.y) < 0.1f)
        {
            currentPitch = Mathf.MoveTowards(currentPitch, 0f, pitchSpeed * 0.5f * dt);
        }

        // --- BANKING ---
        float targetBank = -turnInput * bankAngle;
        currentBank = Mathf.Lerp(currentBank, targetBank, bankSpeed * dt);

        // --- SPEED ---
        float targetSpeed = baseFlightSpeed;
        if (eagleBoosting) targetSpeed *= boostMultiplier;

        float pitchFactor = 1f + (currentPitch / maxPitchAngle) * 0.4f;
        targetSpeed *= pitchFactor;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 3f * dt);

        // --- APPLY ROTATION ---
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, currentBank);
        eagle.transform.rotation = Quaternion.Slerp(eagle.transform.rotation, targetRotation, 10f * dt);

        // --- APPLY VELOCITY ---
        Vector3 velocity = eagle.transform.forward * currentSpeed;
        eagleRigidbody.linearVelocity = velocity;
    }

    void HandleEagleCamera()
    {
        if (eagle == null || mainCamera == null) return;

        Vector3 targetPos = CalculateCameraPosition();

        mainCamera.transform.position = Vector3.SmoothDamp(
            mainCamera.transform.position,
            targetPos,
            ref cameraPositionVelocity,
            1f / eagleCameraFollowSpeed
        );

        Vector3 lookTarget = eagle.transform.position + Vector3.up * 0.5f;
        mainCamera.transform.LookAt(lookTarget);
    }

    Vector3 CalculateCameraPosition()
    {
        float totalYaw = currentYaw + cameraOrbitYaw;
        float pitchRad = cameraOrbitPitch * Mathf.Deg2Rad;
        float yawRad = totalYaw * Mathf.Deg2Rad;

        float horizontalDist = cameraDistance * Mathf.Cos(pitchRad);
        float verticalDist = cameraDistance * Mathf.Sin(pitchRad);

        Vector3 offset = new Vector3(
            -horizontalDist * Mathf.Sin(yawRad),
            verticalDist + cameraHeight,
            -horizontalDist * Mathf.Cos(yawRad)
        );

        return eagle.transform.position + offset;
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}