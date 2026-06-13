using UnityEngine;

namespace FreeFire.Player
{
    public enum SkydivePhase
    {
        InPlane,
        Freefall,
        Parachute,
        Landed
    }

    public class ParachuteController : MonoBehaviour
    {
        [Header("Freefall Settings")]
        [SerializeField] private float freefallTerminalVelocity = 60f;
        [SerializeField] private float freefallAcceleration = 20f;
        [SerializeField] private float freefallHorizontalSpeed = 15f;
        [SerializeField] private float diveBoostMultiplier = 1.5f;

        [Header("Parachute Settings")]
        [SerializeField] private float parachuteTerminalVelocity = 8f;
        [SerializeField] private float parachuteHorizontalSpeed = 12f;
        [SerializeField] private float parachuteDeceleration = 10f;
        [SerializeField] private float autoDeployAltitude = 50f;

        [Header("Controls")]
        [SerializeField] private float leanSensitivity = 2f;
        [SerializeField] private float turnSpeed = 90f;

        [Header("Landing")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float landingDetectionDistance = 2f;

        private SkydivePhase currentPhase;
        private Vector3 velocity;
        private float currentVerticalSpeed;
        private float currentAltitude;

        public SkydivePhase CurrentPhase => currentPhase;
        public float Altitude => currentAltitude;

        private CharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public void BeginSkydive(Vector3 ejectPosition)
        {
            transform.position = ejectPosition;
            currentPhase = SkydivePhase.Freefall;
            currentVerticalSpeed = 0f;
            velocity = Vector3.zero;
        }

        private void Update()
        {
            switch (currentPhase)
            {
                case SkydivePhase.Freefall:
                    UpdateFreefall();
                    break;
                case SkydivePhase.Parachute:
                    UpdateParachute();
                    break;
                case SkydivePhase.Landed:
                    break;
            }

            UpdateAltitude();
        }

        private void UpdateFreefall()
        {
            float inputVertical = Input.GetAxis("Vertical");
            float inputHorizontal = Input.GetAxis("Horizontal");

            float targetTerminalVelocity = freefallTerminalVelocity;
            if (inputVertical > 0.1f)
            {
                targetTerminalVelocity *= diveBoostMultiplier;
            }

            currentVerticalSpeed = Mathf.MoveTowards(
                currentVerticalSpeed, targetTerminalVelocity,
                freefallAcceleration * Time.deltaTime);

            float yaw = inputHorizontal * turnSpeed * Time.deltaTime;
            transform.Rotate(0, yaw, 0);

            Vector3 horizontalMove = transform.forward * (inputVertical * freefallHorizontalSpeed);
            horizontalMove += transform.right * (inputHorizontal * freefallHorizontalSpeed * 0.5f);

            velocity = horizontalMove + Vector3.down * currentVerticalSpeed;
            characterController.Move(velocity * Time.deltaTime);

            if (currentAltitude <= autoDeployAltitude)
            {
                DeployParachute();
            }

            if (Input.GetKeyDown(KeyCode.Space) && currentAltitude > autoDeployAltitude + 20f)
            {
                DeployParachute();
            }
        }

        private void UpdateParachute()
        {
            float inputVertical = Input.GetAxis("Vertical");
            float inputHorizontal = Input.GetAxis("Horizontal");

            currentVerticalSpeed = Mathf.MoveTowards(
                currentVerticalSpeed, parachuteTerminalVelocity,
                parachuteDeceleration * Time.deltaTime);

            float yaw = inputHorizontal * turnSpeed * 0.5f * Time.deltaTime;
            transform.Rotate(0, yaw, 0);

            float horizontalSpeed = parachuteHorizontalSpeed;
            if (inputVertical > 0.1f) horizontalSpeed *= 1.3f;

            Vector3 horizontalMove = transform.forward * (inputVertical * horizontalSpeed);
            velocity = horizontalMove + Vector3.down * currentVerticalSpeed;

            characterController.Move(velocity * Time.deltaTime);

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                    landingDetectionDistance, groundMask))
            {
                Land(hit.point);
            }
        }

        private void DeployParachute()
        {
            currentPhase = SkydivePhase.Parachute;
            currentVerticalSpeed = Mathf.Min(currentVerticalSpeed, parachuteTerminalVelocity * 3f);
            Debug.Log($"[Parachute] Deployed at altitude {currentAltitude:F0}m");
        }

        private void Land(Vector3 groundPoint)
        {
            currentPhase = SkydivePhase.Landed;
            transform.position = groundPoint + Vector3.up * 0.1f;
            velocity = Vector3.zero;
            currentVerticalSpeed = 0f;

            Debug.Log("[Parachute] Landed successfully");
        }

        private void UpdateAltitude()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                    1000f, groundMask))
            {
                currentAltitude = hit.distance;
            }
            else
            {
                currentAltitude = transform.position.y;
            }
        }

        public bool HasLanded()
        {
            return currentPhase == SkydivePhase.Landed;
        }
    }
}
