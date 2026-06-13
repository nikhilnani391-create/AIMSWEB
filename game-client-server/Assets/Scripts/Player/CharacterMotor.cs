using UnityEngine;

namespace FreeFire.Player
{
    public enum MovementState
    {
        Idle,
        Walking,
        Sprinting,
        Crouching,
        Jumping,
        Falling,
        AimDownSights,
        Skydiving,
        Parachuting
    }

    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 7.5f;
        [SerializeField] private float crouchSpeed = 2.0f;
        [SerializeField] private float adsSpeedMultiplier = 0.6f;

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 1.8f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask groundMask;

        [Header("Crouch")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float crouchTransitionSpeed = 8f;

        [Header("Slope Handling")]
        [SerializeField] private float maxSlopeAngle = 50f;

        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private float targetHeight;

        public MovementState CurrentState { get; private set; }
        public bool IsADS { get; set; }
        public Vector3 Velocity => velocity;
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            targetHeight = standingHeight;
        }

        private void Update()
        {
            GroundCheck();
            HandleMovementInput();
            ApplyGravity();
            HandleCrouchTransition();
        }

        private void GroundCheck()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.SphereCast(
                origin, controller.radius * 0.9f,
                Vector3.down, out RaycastHit hit,
                groundCheckDistance + 0.1f, groundMask);

            if (isGrounded && hit.normal != Vector3.up)
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > maxSlopeAngle)
                {
                    Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                    velocity += slideDirection * gravity * Time.deltaTime * 0.5f;
                }
            }
        }

        private void HandleMovementInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector2 moveInput = new Vector2(horizontal, vertical);

            bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && moveInput.y > 0;
            bool wantsCrouch = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

            UpdateMovementState(moveInput, wantsSprint, wantsCrouch);

            float currentSpeed = GetCurrentSpeed();
            if (IsADS) currentSpeed *= adsSpeedMultiplier;

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            Vector3 horizontalVelocity = moveDirection * currentSpeed;
            controller.Move(horizontalVelocity * Time.deltaTime);
        }

        private void UpdateMovementState(Vector2 moveInput, bool sprint, bool crouch)
        {
            if (!isGrounded && velocity.y > 0)
            {
                CurrentState = MovementState.Jumping;
            }
            else if (!isGrounded && velocity.y < -2f)
            {
                CurrentState = MovementState.Falling;
            }
            else if (IsADS)
            {
                CurrentState = MovementState.AimDownSights;
            }
            else if (crouch)
            {
                CurrentState = MovementState.Crouching;
                targetHeight = crouchHeight;
            }
            else if (sprint && moveInput.sqrMagnitude > 0.1f)
            {
                CurrentState = MovementState.Sprinting;
                targetHeight = standingHeight;
            }
            else if (moveInput.sqrMagnitude > 0.01f)
            {
                CurrentState = MovementState.Walking;
                targetHeight = standingHeight;
            }
            else
            {
                CurrentState = MovementState.Idle;
                targetHeight = standingHeight;
            }
        }

        private float GetCurrentSpeed()
        {
            switch (CurrentState)
            {
                case MovementState.Sprinting: return sprintSpeed;
                case MovementState.Crouching: return crouchSpeed;
                case MovementState.AimDownSights: return walkSpeed * adsSpeedMultiplier;
                default: return walkSpeed;
            }
        }

        private void ApplyGravity()
        {
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            if (Input.GetButtonDown("Jump") && isGrounded &&
                CurrentState != MovementState.Crouching)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                CurrentState = MovementState.Jumping;
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
        }

        private void HandleCrouchTransition()
        {
            float currentHeight = controller.height;
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);

            if (Mathf.Abs(newHeight - currentHeight) > 0.001f)
            {
                float heightDiff = newHeight - currentHeight;
                controller.height = newHeight;
                controller.center = new Vector3(0, newHeight / 2f, 0);
                transform.position += Vector3.up * (heightDiff / 2f);
            }
        }

        public void SetMovementState(MovementState state)
        {
            CurrentState = state;
        }

        public float GetNormalizedSpeed()
        {
            float horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
            return Mathf.Clamp01(horizontalSpeed / sprintSpeed);
        }
    }
}
