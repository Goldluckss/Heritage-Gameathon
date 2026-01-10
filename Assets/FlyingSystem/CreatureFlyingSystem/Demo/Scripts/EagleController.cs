using UnityEngine;
using UnityEngine.InputSystem; // <-- Input System
using FlyingSystem;

public class EagleController : MonoBehaviour
{
    private Transform characterTransform;
    public Transform meshRootTransform;

    public Transform springArmTransform;
    public Camera characterCamera;
    private Transform characterCameraTransform;

    public Animator animator;

    public TrailRenderer leftWingTrailRenderer, rightWingTrailRenderer;

    public Renderer speedLineParticleRenderer;

    private CreatureFlyingSystem creatureFlyingSystem;

    private AudioSource audioSource;

    private Airflow airflow;

    public bool activated = false;

    [Header("General Attributes")]
    public bool takeOff;
    public bool boosting;

    [Header("Camera & Input")]
    public float cameraSpeed = 300.0f;
    [Tooltip("Extra multiplier to convert Mouse.current.delta (pixels/frame) into units similar to old Input.GetAxis(\"Mouse X\")")]
    public float mouseSensitivity = 0.01f;
    [Tooltip("Sensitivity used on mobile mouse-tap simulation")]
    public float mobileMouseSensitivity = 0.01f;

    [Range(0.0f, 100.0f)]
    public float springArmSmoothingFactor = 0.25f;

    public float normalCameraY = 3.0f, normalCameraZ = -12.0f;
    public float divingZoomOutY = 3.0f, divingZoomOutZ = -15.0f;

    private bool hideWingTrails = false;

    private float targetSpringArmRotationX, targetSpringArmRotationY;

    public bool isGrabbing = false;
    private Transform targetGrabObjectTransform;
    private Rigidbody targetGrabObjectRigidbody;

    [Header("Mobile")]
    public Joystick joystick;
    public bool mobileInputControl = false;
    public float mobileCameraSpeed = 300.0f;
    private float screenCenterX;

    void Start()
    {
        characterTransform = this.transform;
        if (characterCamera != null)
            characterCameraTransform = characterCamera.transform;

        if (speedLineParticleRenderer != null)
            speedLineParticleRenderer.enabled = false;

        creatureFlyingSystem = this.GetComponent<CreatureFlyingSystem>();

        audioSource = this.GetComponent<AudioSource>();

        screenCenterX = Screen.width / 2.0f;

        if (activated)
            Activate();
    }

    void Update()
    {
        if (activated)
        {
            if (!mobileInputControl)
            {
                PCInputControlLogic();
                CameraControlLogic();
            }
            else
            {
                MobileInputControlLogic();
                MobileCameraControlLogic();
            }
        }
    }

    public void Activate()
    {
        activated = true;
        if (characterCamera != null)
        {
            characterCamera.enabled = true;
            var listener = characterCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }
    }

    public void Deactivate()
    {
        activated = false;
        if (characterCamera != null)
        {
            characterCamera.enabled = false;
            var listener = characterCamera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }

    void PCInputControlLogic()
    {
        // --- Take off / grab (Space) ---
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            if (creatureFlyingSystem != null)
            {
                if (creatureFlyingSystem.inAir)
                {
                    if (isGrabbing)
                        Drop();
                }
                else
                    TakeOff();
            }
        }

        // --- Fly forward / stop (W / S) ---
        if (Keyboard.current != null && Keyboard.current.wKey.isPressed)
        {
            creatureFlyingSystem?.FlyForward();
        }
        else if (Keyboard.current != null && Keyboard.current.sKey.isPressed)
        {
            creatureFlyingSystem?.SlowDown();
        }
        else if (Keyboard.current != null && Keyboard.current.sKey.wasReleasedThisFrame)
        {
            creatureFlyingSystem?.StopSlowingDown();
        }

        // --- Turn left / right (mouse delta X mapped to yaw) ---
        float mouseDeltaX = 0f;
        if (Mouse.current != null)
        {
            var d = Mouse.current.delta.ReadValue();
            mouseDeltaX = d.x * mouseSensitivity;
        }
        // AddYawInput expects a scaled value similar to previous Input.GetAxis("Mouse X")
        creatureFlyingSystem?.AddYawInput(mouseDeltaX);

        DivingLogic();

        // --- Boost on / off (LeftShift or RightShift) ---
        if (Keyboard.current != null && (Keyboard.current.leftShiftKey.wasReleasedThisFrame || Keyboard.current.rightShiftKey.wasReleasedThisFrame))
        {
            Boost();
        }
    }

    void MobileInputControlLogic()
    {
        if (joystick != null)
        {
            if (joystick.inputAxisY > 0.01f)
                creatureFlyingSystem?.FlyForward();
            else if (joystick.inputAxisY < -0.85f)
                creatureFlyingSystem?.SlowDown();
            else if (creatureFlyingSystem != null && creatureFlyingSystem.slowingDown && joystick.inputAxisY > -0.85f)
                creatureFlyingSystem.StopSlowingDown();

            // Use joystick horizontal to add yaw (if desired)
            if (Mathf.Abs(joystick.inputAxisX) > 0.01f)
                creatureFlyingSystem?.AddYawInput(joystick.inputAxisX);
            
            DivingLogic();
        }
    }

    void DivingLogic()
    {
        if (creatureFlyingSystem != null && creatureFlyingSystem.inAir && creatureFlyingSystem.diving)
        {
            // Camera zoom out
            if (characterCameraTransform != null)
                characterCameraTransform.localPosition = Vector3.Lerp(characterCameraTransform.localPosition, new Vector3(0.0f, divingZoomOutY, divingZoomOutZ), 0.95f * Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool("FlyToGlide", true);
                animator.SetBool("GlideToFly", false);
            }

            // Enable trails from both wings
            if (leftWingTrailRenderer != null && !leftWingTrailRenderer.enabled)
            {
                hideWingTrails = false;

                leftWingTrailRenderer.enabled = true;
                rightWingTrailRenderer.enabled = true;

                if (speedLineParticleRenderer != null)
                    speedLineParticleRenderer.enabled = true;
            }
        }
        else
        {
            // Reset all effects
            if (characterCameraTransform != null)
                characterCameraTransform.localPosition = Vector3.Lerp(characterCameraTransform.localPosition, new Vector3(0.0f, normalCameraY, normalCameraZ), 0.5f * Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool("GlideToFly", true);
                animator.SetBool("FlyToGlide", false);
            }

            if (!hideWingTrails)
            {
                hideWingTrails = true;

                if (leftWingTrailRenderer != null) leftWingTrailRenderer.enabled = false;
                if (rightWingTrailRenderer != null) rightWingTrailRenderer.enabled = false;
                if (speedLineParticleRenderer != null) speedLineParticleRenderer.enabled = false;
            }
        }
    }

    void CameraControlLogic()
    {
        if (springArmTransform != null && characterTransform != null)
        {
            springArmTransform.position = Vector3.Lerp(characterTransform.position, springArmTransform.position, springArmSmoothingFactor * Time.deltaTime);

            float mouseX = 0f, mouseY = 0f;
            if (Mouse.current != null)
            {
                var d = Mouse.current.delta.ReadValue();
                mouseX = d.x * mouseSensitivity;
                mouseY = d.y * mouseSensitivity;
            }

            float newRotX = springArmTransform.rotation.eulerAngles.x - mouseY * cameraSpeed * Time.deltaTime;
            float newRotY = springArmTransform.rotation.eulerAngles.y + mouseX * cameraSpeed * Time.deltaTime;

            springArmTransform.rotation = Quaternion.Euler(newRotX, newRotY, 0.0f);
        }
    }

    void MobileCameraControlLogic()
    {
        // Temporarily use mouse to simulate the touch
        bool mousePressed = (Mouse.current != null && Mouse.current.leftButton.isPressed);
        if (mousePressed && Mouse.current != null && Mouse.current.position.ReadValue().x > screenCenterX)
        {
            var d = Mouse.current.delta.ReadValue();
            float dx = d.x * mobileMouseSensitivity;
            float dy = d.y * mobileMouseSensitivity;

            targetSpringArmRotationX = springArmTransform.rotation.eulerAngles.x - dy * mobileCameraSpeed * Time.deltaTime;
            targetSpringArmRotationY = springArmTransform.rotation.eulerAngles.y + dx * mobileCameraSpeed * Time.deltaTime;

            creatureFlyingSystem?.AddYawInput(dx);
        }
        else
        {
            targetSpringArmRotationX = springArmTransform.rotation.eulerAngles.x;
            targetSpringArmRotationY = springArmTransform.rotation.eulerAngles.y;
        }

        springArmTransform.rotation = Quaternion.Euler(targetSpringArmRotationX, targetSpringArmRotationY, 0.0f);
    }

    public void TakeOff()
    {
        if (creatureFlyingSystem != null && !creatureFlyingSystem.inAir)
        {
            creatureFlyingSystem.TakeOff();
            takeOff = creatureFlyingSystem.inAir;

            if (animator != null)
            {
                animator.SetBool("FlyToIdle", false);
                animator.SetBool("IdleToFly", true);

                animator.SetBool("GlideToIdle", false);
            }

            if (audioSource != null)
                audioSource.Play();
        }
    }

    public void Boost()
    {
        if (creatureFlyingSystem != null)
        {
            creatureFlyingSystem.boosting = !creatureFlyingSystem.boosting;
            boosting = creatureFlyingSystem.boosting;
        }
    }

    public void Drop()
    {
        if (targetGrabObjectTransform != null)
        {
            isGrabbing = false;

            targetGrabObjectTransform.SetParent(null);

            if (targetGrabObjectRigidbody != null)
            {
                targetGrabObjectRigidbody.useGravity = true;
                targetGrabObjectRigidbody.isKinematic = false;
            }

            if (creatureFlyingSystem != null)
                creatureFlyingSystem.currentCarryingWeight -= 3.0f;

            targetGrabObjectTransform = null;
            targetGrabObjectRigidbody = null;
        }
    }

    public float GetFlyingSpeed()
    {
        return creatureFlyingSystem != null ? creatureFlyingSystem.flyingSpeed : 0f;
    }

    public float GetStaminaPercentage()
    {
        return creatureFlyingSystem != null ? creatureFlyingSystem.staminaPercentage : 0f;
    }

    public float GetWeightPercentage()
    {
        return creatureFlyingSystem != null ? creatureFlyingSystem.weightPercentage : 0f;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null) return;

        // The target collision can be anything like ground, terrain, etc.
        if (collision.collider.name == "Road")
        {
            if (creatureFlyingSystem != null && creatureFlyingSystem.inAir && !isGrabbing)
            {
                creatureFlyingSystem.Land();
                takeOff = creatureFlyingSystem.inAir;

                if (animator != null)
                {
                    animator.SetBool("GlideToIdle", true);

                    animator.SetBool("FlyToIdle", true);
                    animator.SetBool("IdleToFly", false);

                    animator.SetBool("FlyToGlide", false);
                }

                if (leftWingTrailRenderer != null) leftWingTrailRenderer.enabled = false;
                if (rightWingTrailRenderer != null) rightWingTrailRenderer.enabled = false;
                if (speedLineParticleRenderer != null) speedLineParticleRenderer.enabled = false;
            }
        }
        else if (collision.collider.name == "Weight" && !isGrabbing)
        {
            // Grab
            isGrabbing = true;

            targetGrabObjectTransform = collision.transform;

            if (targetGrabObjectTransform != null)
            {
                targetGrabObjectRigidbody = targetGrabObjectTransform.GetComponent<Rigidbody>();
                if (targetGrabObjectRigidbody != null)
                {
                    targetGrabObjectRigidbody.useGravity = false;
                    targetGrabObjectRigidbody.isKinematic = true;
                }

                targetGrabObjectTransform.SetParent(meshRootTransform);
                targetGrabObjectTransform.localPosition = new Vector3(0.0f, -2.25f, -2.172f);

                if (creatureFlyingSystem != null)
                    creatureFlyingSystem.currentCarryingWeight += 3.0f;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Entering the airflow can lift up the flyer
        if (other.name == "Airflow")
        {
            airflow = other.GetComponent<Airflow>();

            if (airflow != null && creatureFlyingSystem != null)
                creatureFlyingSystem.AddAirflowForce(airflow.intensity, airflow.acceleration, airflow.fadeOutAcceleration);

            if (creatureFlyingSystem != null)
                creatureFlyingSystem.stopFlying = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        if (other.name == "Airflow" && creatureFlyingSystem != null)
        {
            creatureFlyingSystem.EndAirflowForce();
            creatureFlyingSystem.stopFlying = false;
        }
    }
}
