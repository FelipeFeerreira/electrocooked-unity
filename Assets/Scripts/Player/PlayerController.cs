using ElectroCooked.Core;
using UnityEngine;

namespace ElectroCooked.Player
{
    /// <summary>
    /// Movimento simples top-down/terceira pessoa, estilo Overcooked:
    /// WASD/analógico move, o personagem gira para a direção do movimento.
    /// O input é lido pelo InputReader (Input System novo).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        public float moveSpeed = 5f;
        public float rotationSpeed = 14f;
        public float gravity = -18f;

        [Header("Câmera (opcional)")]
        [Tooltip("Se preenchido, o movimento fica relativo à câmera — como no Overcooked, " +
                 "onde 'pra cima' é pra cima na tela, não no eixo Z do mundo.")]
        public Transform cameraTransform;

        CharacterController _controller;
        Vector3 _verticalVelocity;

        public Vector3 Forward => transform.forward;
        public Vector3 Position => transform.position;
        /// <summary>Velocidade horizontal atual em 0..1 — útil para animação.</summary>
        public float NormalizedSpeed { get; private set; }

        void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            Vector2 raw = InputReader.Move;
            Vector3 input = new Vector3(raw.x, 0f, raw.y);

            if (cameraTransform != null && input.sqrMagnitude > 0.001f)
            {
                Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
                Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
                input = camForward * raw.y + camRight * raw.x;
            }

            NormalizedSpeed = Mathf.Clamp01(input.magnitude);
            Vector3 move = input * moveSpeed;

            if (input.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(input, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            if (_controller.isGrounded && _verticalVelocity.y < 0f)
                _verticalVelocity.y = -2f;
            _verticalVelocity.y += gravity * Time.deltaTime;

            _controller.Move((move + _verticalVelocity) * Time.deltaTime);
        }
    }
}
