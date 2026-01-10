using UnityEngine;
using UnityEngine.InputSystem;

namespace OnScreenPointerPluginExample
{
    public class CameraRotationControl : MonoBehaviour
    {
        public float sensitivity = 150f;
        public float maxPitch = 80f;

        float yaw;
        float pitch;

        void Update()
        {
            // Mouse delta from new Input System
            if (Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                float mouseX = mouseDelta.x * sensitivity * Time.deltaTime;
                float mouseY = mouseDelta.y * sensitivity * Time.deltaTime;

                yaw += mouseX;
                pitch -= mouseY;
                pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }
    }
}
