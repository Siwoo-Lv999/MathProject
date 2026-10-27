using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _01.Code.Camera
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineInputAxisController))]
    public class CameraCursor : MonoBehaviour
    {
        [SerializeField] private CinemachineInputAxisController inputController;

        private int _captureFrame;

        private void Reset()
        {
            inputController = GetComponent<CinemachineInputAxisController>();
        }

        private void Awake()
        {
            if (inputController != null)
                return;

            Debug.LogError("Assign the camera input controller to CameraCursor.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            if (Application.isPlaying && inputController != null)
                SetCursorLocked(Application.isFocused);
        }

        private void OnDisable()
        {
            if (Application.isPlaying && inputController != null)
                SetCursorLocked(false);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
                return;
            }

            if (!Application.isFocused)
            {
                SetCursorLocked(false);
                return;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                inputController.enabled = false;
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 pointer = Mouse.current.position.ReadValue();
                    if (pointer.x >= 0f && pointer.x < Screen.width &&
                        pointer.y >= 0f && pointer.y < Screen.height)
                        SetCursorLocked(true);
                }
                return;
            }

            // Ignore the capture frame so the cursor warp cannot jump the camera.
            inputController.enabled = Time.frameCount > _captureFrame;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && inputController != null)
                SetCursorLocked(false);
        }

        private void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
            inputController.enabled = false;
            if (locked)
                _captureFrame = Time.frameCount;
        }
    }
}
