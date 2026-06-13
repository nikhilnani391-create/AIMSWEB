using UnityEngine;
using System.Collections.Generic;

namespace FreeFire.Networking
{
    public struct InputCommand
    {
        public int SequenceNumber;
        public float DeltaTime;
        public Vector2 MoveInput;
        public bool Jump;
        public bool Sprint;
        public bool Crouch;
        public float YawRotation;
    }

    public class ClientPrediction : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float jumpForce = 6f;
        [SerializeField] private float gravity = -20f;

        private CharacterController characterController;
        private readonly List<InputCommand> pendingInputs = new List<InputCommand>();
        private int currentSequence;

        private Vector3 velocity;
        private Vector3 lastServerPosition;
        private int lastProcessedSequence;

        private const float RECONCILIATION_THRESHOLD = 0.5f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        public InputCommand SampleInput()
        {
            var cmd = new InputCommand
            {
                SequenceNumber = currentSequence++,
                DeltaTime = Time.deltaTime,
                MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                Jump = Input.GetButtonDown("Jump"),
                Sprint = Input.GetKey(KeyCode.LeftShift),
                Crouch = Input.GetKey(KeyCode.LeftControl),
                YawRotation = transform.eulerAngles.y
            };

            pendingInputs.Add(cmd);
            ApplyInput(cmd);

            return cmd;
        }

        public void OnServerStateReceived(Vector3 serverPosition, int processedSequence)
        {
            lastServerPosition = serverPosition;
            lastProcessedSequence = processedSequence;

            pendingInputs.RemoveAll(i => i.SequenceNumber <= processedSequence);

            float error = Vector3.Distance(transform.position, serverPosition);

            if (error > RECONCILIATION_THRESHOLD)
            {
                Reconcile(serverPosition);
            }
        }

        private void Reconcile(Vector3 serverPosition)
        {
            transform.position = serverPosition;
            velocity = Vector3.zero;

            foreach (var input in pendingInputs)
            {
                ApplyInput(input);
            }
        }

        private void ApplyInput(InputCommand cmd)
        {
            float speed = moveSpeed;
            if (cmd.Sprint) speed *= sprintMultiplier;
            if (cmd.Crouch) speed *= 0.5f;

            Vector3 forward = Quaternion.Euler(0, cmd.YawRotation, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, cmd.YawRotation, 0) * Vector3.right;

            Vector3 moveDirection = (forward * cmd.MoveInput.y + right * cmd.MoveInput.x).normalized;
            Vector3 horizontalMove = moveDirection * speed * cmd.DeltaTime;

            if (characterController.isGrounded)
            {
                velocity.y = -2f;
                if (cmd.Jump)
                {
                    velocity.y = jumpForce;
                }
            }

            velocity.y += gravity * cmd.DeltaTime;
            Vector3 totalMove = horizontalMove + Vector3.up * velocity.y * cmd.DeltaTime;

            characterController.Move(totalMove);
        }

        public byte[] SerializeInput(InputCommand cmd)
        {
            var data = new byte[24];
            int offset = 0;

            WriteInt(data, ref offset, cmd.SequenceNumber);
            WriteFloat(data, ref offset, cmd.DeltaTime);
            WriteFloat(data, ref offset, cmd.MoveInput.x);
            WriteFloat(data, ref offset, cmd.MoveInput.y);

            byte flags = 0;
            if (cmd.Jump) flags |= 0x01;
            if (cmd.Sprint) flags |= 0x02;
            if (cmd.Crouch) flags |= 0x04;
            data[offset++] = flags;

            WriteFloat(data, ref offset, cmd.YawRotation);

            return data;
        }

        private static void WriteInt(byte[] buffer, ref int offset, int value)
        {
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
        }

        private static void WriteFloat(byte[] buffer, ref int offset, float value)
        {
            byte[] bytes = System.BitConverter.GetBytes(value);
            System.Array.Copy(bytes, 0, buffer, offset, 4);
            offset += 4;
        }
    }
}
