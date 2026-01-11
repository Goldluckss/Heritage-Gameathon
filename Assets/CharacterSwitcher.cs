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
    public Transform eagleMeshRoot;

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

    [Header("Eagle Grab Settings")]
    public Vector3 grabOffset = new Vector3(0f, -2.25f, -2.172f);
    public float grabWeightIncrease = 3f;
    public bool dropItemOnReturn = true;

    [Header("Eagle Return Settings")]
    public float returnSpeed = 20f;
    public float returnRotationSpeed = 5f;

    [Header("Current State")]
    public bool controllingEagle = false;
    public bool eagleReturning = false;
    public bool isGrabbing = false;

    // Component references
    private PlayerManager playerManager;
    private InputManager playerInputManager;
    private PlayerLocomotion playerLocomotion;

    private EagleFollower eagleFollower;
    private CreatureFlyingSystem eagleFlyingSystem;
    private EagleController eagleController;
    private Rigidbody eagleRigidbody;

    // Grab references
    private Transform grabbedObjectTransform;
    private Rigidbody grabbedObjectRigidbody;
    private Collider grabbedObjectCollider;

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

    // Store original collision state
    private Collider[] eagleColliders;
    private bool[] originalColliderStates;
    private int originalEagleLayer;

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

            if (eagleMeshRoot == null && eagleFlyingSystem != null)
            {
                eagleMeshRoot = eagleFlyingSystem.meshRootTransform;
            }
            if (eagleMeshRoot == null)
            {
                eagleMeshRoot = eagle.transform;
            }

            // Cache all colliders on the eagle (including children)
            eagleColliders = eagle.GetComponentsInChildren<Collider>();
            originalColliderStates = new bool[eagleColliders.Length];
            for (int i = 0; i < eagleColliders.Length; i++)
            {
                originalColliderStates[i] = eagleColliders[i].enabled;
            }

            // Store original layer
            originalEagleLayer = eagle.layer;

            // Auto-setup the grab detector on eagle
            EagleGrabDetector grabDetector = eagle.GetComponent<EagleGrabDetector>();
            if (grabDetector == null)
            {
                grabDetector = eagle.AddComponent<EagleGrabDetector>();
            }
            grabDetector.characterSwitcher = this;
        }

        playerControls = new PlayerControls();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

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
            if (!eagleReturning)
            {
                ToggleControl();
            }
        }

        if (controllingEagle)
        {
            ReadEagleInput();

            // Space to drop grabbed object
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (isGrabbing)
                {
                    Debug.Log("Space pressed, dropping object");
                    DropObject();
                }
            }
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

        if (cameraManager != null)
        {
            originalCameraManagerLookAngle = cameraManager.lookAngle;
            originalCameraManagerPivotAngle = cameraManager.pivotAngle;
        }

        if (playerManager != null) playerManager.enabled = false;
        if (playerInputManager != null) playerInputManager.enabled = false;
        if (playerLocomotion != null) playerLocomotion.enabled = false;
        if (cameraManager != null) cameraManager.enabled = false;

        if (eagleFollower != null) eagleFollower.enabled = false;
        if (eagleController != null) eagleController.enabled = false;
        if (eagleFlyingSystem != null) eagleFlyingSystem.enabledFlyingLogic = false;

        if (eagleRigidbody != null)
        {
            eagleRigidbody.useGravity = false;
            eagleRigidbody.linearDamping = 0f;
            eagleRigidbody.angularDamping = 0f;
            eagleRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            eagleRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            eagleRigidbody.linearVelocity = Vector3.zero;
        }

        Vector3 eagleRotation = eagle.transform.eulerAngles;
        currentYaw = eagleRotation.y;
        currentPitch = NormalizeAngle(eagleRotation.x);
        currentBank = 0f;
        currentSpeed = baseFlightSpeed;

        cameraOrbitYaw = 0f;
        cameraOrbitPitch = 20f;

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

        // DON'T drop the item here - eagle will carry it back!

        eagleReturning = true;
        controllingEagle = false;

        // Disable eagle collisions so it can phase through walls
        SetEagleCollisionsEnabled(false);

        if (eagleRigidbody != null)
        {
            eagleRigidbody.linearVelocity = Vector3.zero;
            eagleRigidbody.angularVelocity = Vector3.zero;
        }

        RestoreCamera();

        if (playerManager != null) playerManager.enabled = true;
        if (playerInputManager != null) playerInputManager.enabled = true;
        if (playerLocomotion != null) playerLocomotion.enabled = true;

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

        mainCamera.transform.SetParent(originalCameraParent);
        mainCamera.transform.localPosition = originalCameraLocalPosition;
        mainCamera.transform.localRotation = originalCameraLocalRotation;
    }

    /// <summary>
    /// Enables or disables all colliders on the eagle.
    /// </summary>
    void SetEagleCollisionsEnabled(bool enabled)
    {
        if (eagleColliders == null) return;

        if (enabled)
        {
            // Restore original collider states
            for (int i = 0; i < eagleColliders.Length; i++)
            {
                if (eagleColliders[i] != null)
                {
                    eagleColliders[i].enabled = originalColliderStates[i];
                }
            }
            
            // Restore original layer
            SetEagleLayerRecursive(eagle.transform, originalEagleLayer);
            
            Debug.Log("[CharacterSwitcher] Eagle collisions ENABLED");
        }
        else
        {
            // Disable all colliders
            for (int i = 0; i < eagleColliders.Length; i++)
            {
                if (eagleColliders[i] != null)
                {
                    eagleColliders[i].enabled = false;
                }
            }
            
            // Optionally change to IgnoreRaycast layer (layer 2) for extra safety
            // SetEagleLayerRecursive(eagle.transform, 2);
            
            Debug.Log("[CharacterSwitcher] Eagle collisions DISABLED");
        }
    }

    /// <summary>
    /// Sets the layer of the eagle and all its children recursively.
    /// </summary>
    void SetEagleLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            SetEagleLayerRecursive(child, layer);
        }
    }

    void HandleEagleReturn()
    {
        if (eagle == null || player == null) return;

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

        float distanceToTarget = Vector3.Distance(eagle.transform.position, targetPosition);

        if (distanceToTarget > 0.5f)
        {
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

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                eagle.transform.rotation = Quaternion.RotateTowards(
                    eagle.transform.rotation,
                    targetRotation,
                    returnRotationSpeed * 100f * Time.deltaTime
                );
            }
        }
        else
        {
            FinishEagleReturn();
        }
    }

    void FinishEagleReturn()
    {
        Debug.Log("Eagle returned to player");

        eagleReturning = false;

        // Re-enable eagle collisions now that it's back
        SetEagleCollisionsEnabled(true);

        // Drop the item now that eagle has returned
        if (isGrabbing && dropItemOnReturn)
        {
            Debug.Log("Eagle delivering item to player");
            DropObject();
        }

        if (eagleRigidbody != null)
        {
            eagleRigidbody.linearVelocity = Vector3.zero;
            eagleRigidbody.angularVelocity = Vector3.zero;
            eagleRigidbody.interpolation = RigidbodyInterpolation.None;
            eagleRigidbody.constraints = RigidbodyConstraints.None;
        }

        if (eagle != null && player != null)
        {
            eagle.transform.rotation = Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f);
        }

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

    void HandleEagleMovement()
    {
        if (eagle == null || eagleRigidbody == null) return;

        float dt = Time.fixedDeltaTime;

        float turnInput = eagleMovementInput.x;
        currentYaw += turnInput * turnSpeed * dt;

        float pitchInput = -eagleMovementInput.y;
        float targetPitch = currentPitch + pitchInput * pitchSpeed * dt;
        currentPitch = Mathf.Clamp(targetPitch, -maxPitchAngle, maxPitchAngle);

        if (Mathf.Abs(eagleMovementInput.y) < 0.1f)
        {
            currentPitch = Mathf.MoveTowards(currentPitch, 0f, pitchSpeed * 0.5f * dt);
        }

        float targetBank = -turnInput * bankAngle;
        currentBank = Mathf.Lerp(currentBank, targetBank, bankSpeed * dt);

        float targetSpeed = baseFlightSpeed;
        if (eagleBoosting) targetSpeed *= boostMultiplier;

        float pitchFactor = 1f + (currentPitch / maxPitchAngle) * 0.4f;
        targetSpeed *= pitchFactor;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 3f * dt);

        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, currentBank);
        eagle.transform.rotation = Quaternion.Slerp(eagle.transform.rotation, targetRotation, 10f * dt);

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

    // --- GRABBING FUNCTIONALITY ---

    public void OnEagleCollision(Collision collision)
    {
        if (!controllingEagle) return;
        if (isGrabbing) return;

        if (collision.collider.name == "Weight")
        {
            GrabObject(collision.transform);
        }
    }

    public void GrabObject(Transform objectToGrab)
    {
        if (isGrabbing || objectToGrab == null) return;

        Debug.Log("Grabbing object: " + objectToGrab.name);

        isGrabbing = true;
        grabbedObjectTransform = objectToGrab;
        grabbedObjectRigidbody = objectToGrab.GetComponent<Rigidbody>();
        grabbedObjectCollider = objectToGrab.GetComponent<Collider>();

        if (grabbedObjectCollider != null)
        {
            grabbedObjectCollider.enabled = false;
        }

        if (grabbedObjectRigidbody != null)
        {
            grabbedObjectRigidbody.useGravity = false;
            grabbedObjectRigidbody.isKinematic = true;
        }

        grabbedObjectTransform.SetParent(eagleMeshRoot);
        grabbedObjectTransform.localPosition = grabOffset;
        grabbedObjectTransform.localRotation = Quaternion.identity;

        if (eagleFlyingSystem != null)
        {
            eagleFlyingSystem.currentCarryingWeight += grabWeightIncrease;
        }

        Debug.Log("Eagle grabbed: " + objectToGrab.name);
    }

    public void DropObject()
    {
        if (!isGrabbing || grabbedObjectTransform == null)
        {
            Debug.Log("Cannot drop - isGrabbing: " + isGrabbing + ", grabbedObject: " + grabbedObjectTransform);
            return;
        }

        Debug.Log("Dropping object: " + grabbedObjectTransform.name);

        grabbedObjectTransform.SetParent(null);

        if (grabbedObjectCollider != null)
        {
            grabbedObjectCollider.enabled = true;
        }

        if (grabbedObjectRigidbody != null)
        {
            grabbedObjectRigidbody.useGravity = true;
            grabbedObjectRigidbody.isKinematic = false;
            // Give less velocity when delivering to player
            if (eagleReturning || !controllingEagle)
            {
                grabbedObjectRigidbody.linearVelocity = Vector3.zero;
            }
            else
            {
                grabbedObjectRigidbody.linearVelocity = eagleRigidbody != null ? eagleRigidbody.linearVelocity * 0.5f : Vector3.zero;
            }
        }

        if (eagleFlyingSystem != null)
        {
            eagleFlyingSystem.currentCarryingWeight -= grabWeightIncrease;
        }

        Debug.Log("Eagle dropped: " + grabbedObjectTransform.name);

        grabbedObjectTransform = null;
        grabbedObjectRigidbody = null;
        grabbedObjectCollider = null;
        isGrabbing = false;
    }
}