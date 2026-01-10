using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    InputManager inputManager;


    public Transform targetTransform;
    public Transform cameraPivot;
    public Transform cameraTransform;
    public LayerMask collisionLayers;
    private float defaultPosition;
    private Vector3 cameraFollowVelocity = Vector3.zero;
    private Vector3 cameraVectorPosition;
    private float cameraZVelocity = 0f; // For smoothing camera Z position

    public float cameraCollisionOffSet = 0.2f;
    public float minimumCollisionOffSet = 0.2f; // Minimum distance camera can be from pivot (in local Z)
    public float cameraCollisionRadius = 0.2f;
    public float cameraFollowSpeed = 0.2f;
    public float cameraLookSpeed = 2;
    public float cameraPivotSpeed = 2;

    public float lookAngle;
    public float pivotAngle;
    public float minimumPivotAngle = -35;
    public float maximumPivotAngle = 35;

    private void Awake()
    {
        inputManager = FindFirstObjectByType<InputManager>();
        targetTransform = FindFirstObjectByType<PlayerManager>().transform;
        
        // Find camera transform - check if it's assigned, otherwise find it
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                // Try to find camera as child of camera pivot
                cameraTransform = cameraPivot.GetComponentInChildren<Camera>()?.transform;
            }
        }
        
        // Get default position - if camera is a child of pivot, use local position
        if (cameraTransform != null && cameraTransform.parent == cameraPivot)
        {
            defaultPosition = cameraTransform.localPosition.z;
            // Ensure defaultPosition is negative (camera should be behind pivot)
            if (defaultPosition >= 0)
            {
                defaultPosition = -3f; // Default fallback distance
            }
        }
        else
        {
            // Fallback: calculate distance from pivot to camera
            Vector3 toCamera = cameraPivot.position - cameraTransform.position;
            defaultPosition = -toCamera.magnitude; // Negative because camera is behind pivot
            if (defaultPosition == 0) defaultPosition = -3f; // Default fallback distance
        }
        
    }

    public void HandleALLCamearaMovement()
    {
        FollowTarget();
        RotateCamera();
        HandleCameraCollision();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = Vector3.SmoothDamp
            (transform.position, targetTransform.position, ref cameraFollowVelocity, cameraFollowSpeed);
        transform.position = targetPosition;
    }

    private void RotateCamera()
    {
        Vector3 rotation;
        Quaternion targetRotation;

        lookAngle = lookAngle + (inputManager.cameraInputX * cameraLookSpeed);
        pivotAngle = pivotAngle - (inputManager.cameraInputY * cameraPivotSpeed);
        pivotAngle = Mathf.Clamp(pivotAngle, minimumPivotAngle, maximumPivotAngle);

        rotation = Vector3.zero;
        rotation.y = lookAngle;
        targetRotation = Quaternion.Euler(rotation);
        transform.rotation = targetRotation;

        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        targetRotation = Quaternion.Euler(rotation);
        cameraPivot.localRotation = targetRotation;
    }

    private void HandleCameraCollision()
    {
        if (cameraTransform == null || cameraPivot == null) return;
        
        float targetPosition = defaultPosition; // Start with default position (e.g., -3)
        RaycastHit hit;
        
        // Calculate direction from camera pivot to where the camera should be (behind the pivot)
        // Camera looks forward, so it's positioned backward relative to pivot's forward
        Vector3 direction = -cameraPivot.forward; // Backward direction (towards where camera should be)
        float maxDistance = Mathf.Abs(defaultPosition); // Maximum distance to cast (e.g., 3)
        
        // Start the cast forward from the pivot to avoid hitting the player character
        // This prevents glitches when walking backwards - use a larger offset
        float forwardOffset = cameraCollisionRadius * 1.5f; // Increased offset
        Vector3 castOrigin = cameraPivot.position + cameraPivot.forward * forwardOffset;
        
        // Debug: Draw ray in scene view
        Debug.DrawRay(castOrigin, direction * maxDistance, Color.red);
        
        // Cast from slightly forward of camera pivot towards the desired camera position (backward)
        if (Physics.SphereCast(castOrigin, cameraCollisionRadius, direction, out hit, maxDistance, collisionLayers))
        {
            // Ignore hits on the player character to prevent glitches when walking backwards
            if (hit.collider != null && hit.collider.transform == targetTransform)
            {
                // Hit the player, ignore it and use default position
                targetPosition = defaultPosition;
            }
            else
            {
                // hit.distance = distance from cast origin to where sphere center hits the wall
                // Account for the forward offset we added to get distance from pivot
                float distanceFromPivot = hit.distance + forwardOffset;
                
                // To get the safe camera position:
                // - Subtract sphere radius to get distance to wall surface
                // - Subtract offset to keep camera away from wall
                float distanceToWall = distanceFromPivot - cameraCollisionRadius;
                float safeCameraDistance = distanceToWall - cameraCollisionOffSet;
                
                // Clamp to ensure camera doesn't get too close to pivot
                // safeCameraDistance should be between minimumCollisionOffSet and maxDistance
                safeCameraDistance = Mathf.Clamp(safeCameraDistance, minimumCollisionOffSet, maxDistance);
                
                // Camera position is negative (behind pivot in local space)
                targetPosition = -safeCameraDistance;
            }
        }

        // Check if camera is a child of pivot (use local position) or not (use world position)
        if (cameraTransform.parent == cameraPivot)
        {
            // Camera is child of pivot - use local position
            // Preserve X and Y positions, only adjust Z (distance from pivot)
            Vector3 localPos = cameraTransform.localPosition;
            float originalX = localPos.x;
            float originalY = localPos.y;
            
            // Use SmoothDamp for smoother camera movement to prevent glitches
            float newZ = Mathf.SmoothDamp(localPos.z, targetPosition, ref cameraZVelocity, 0.1f);
            
            // Set position preserving X and Y, only changing Z
            cameraTransform.localPosition = new Vector3(originalX, originalY, newZ);
        }
        else
        {
            // Camera is not a child - calculate world position relative to pivot
            // Preserve the offset from pivot's local X and Y axes
            Vector3 localOffset = cameraPivot.InverseTransformPoint(cameraTransform.position);
            localOffset.z = targetPosition; // Only adjust Z
            Vector3 worldPos = cameraPivot.TransformPoint(localOffset);
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, worldPos, 0.5f);
        }
    }
}
