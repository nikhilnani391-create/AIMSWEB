using UnityEngine;

namespace FreeFire.Player
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private Transform playerTarget;

        [Header("TPS Camera Settings")]
        [SerializeField] private Vector3 hipOffset = new Vector3(0.5f, 1.6f, -2.5f);
        [SerializeField] private Vector3 adsOffset = new Vector3(0.3f, 1.5f, -1.0f);

        [Header("FOV")]
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float adsFOV = 40f;
        [SerializeField] private float fovTransitionSpeed = 10f;

        [Header("Sensitivity")]
        [SerializeField] private float horizontalSensitivity = 2.0f;
        [SerializeField] private float verticalSensitivity = 2.0f;
        [SerializeField] private float adsSensitivityMultiplier = 0.6f;

        [Header("Pitch Limits")]
        [SerializeField] private float minPitch = -60f;
        [SerializeField] private float maxPitch = 75f;

        [Header("Collision")]
        [SerializeField] private float cameraCollisionRadius = 0.3f;
        [SerializeField] private LayerMask collisionMask;

        private Camera mainCamera;
        private float currentYaw;
        private float currentPitch;
        private bool isADS;
        private Vector3 currentOffset;

        public float Yaw => currentYaw;
        public float Pitch => currentPitch;

        private void Start()
        {
            mainCamera = cameraTransform.GetComponent<Camera>();
            currentOffset = hipOffset;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            HandleRotationInput();
            UpdateCameraPosition();
            UpdateFOV();
        }

        public void SetADS(bool ads)
        {
            isADS = ads;
        }

        private void HandleRotationInput()
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            float sensitivity = isADS ? adsSensitivityMultiplier : 1f;

            currentYaw += mouseX * horizontalSensitivity * sensitivity;
            currentPitch -= mouseY * verticalSensitivity * sensitivity;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            if (playerTarget != null)
            {
                playerTarget.rotation = Quaternion.Euler(0, currentYaw, 0);
            }
        }

        private void UpdateCameraPosition()
        {
            Vector3 targetOffset = isADS ? adsOffset : hipOffset;
            currentOffset = Vector3.Lerp(currentOffset, targetOffset, fovTransitionSpeed * Time.deltaTime);

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 targetPosition = playerTarget.position +
                                     rotation * new Vector3(currentOffset.x, 0, currentOffset.z) +
                                     Vector3.up * currentOffset.y;

            Vector3 directionFromPlayer = targetPosition - (playerTarget.position + Vector3.up * currentOffset.y);
            float desiredDistance = directionFromPlayer.magnitude;

            if (Physics.SphereCast(
                    playerTarget.position + Vector3.up * currentOffset.y,
                    cameraCollisionRadius,
                    directionFromPlayer.normalized,
                    out RaycastHit hit,
                    desiredDistance,
                    collisionMask))
            {
                targetPosition = hit.point + hit.normal * cameraCollisionRadius;
            }

            cameraTransform.position = targetPosition;
            cameraTransform.LookAt(playerTarget.position + Vector3.up * currentOffset.y);
        }

        private void UpdateFOV()
        {
            float targetFOV = isADS ? adsFOV : normalFOV;
            mainCamera.fieldOfView = Mathf.Lerp(
                mainCamera.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }
    }
}
