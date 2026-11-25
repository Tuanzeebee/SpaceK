using SpaceK.CameraSystem;
using UnityEngine;

namespace SpaceK.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public PlayerInputReader Input;
        public PlayerCameraRig CameraRig;
        public Transform VisualRoot;
        public Animator Animator;
        public bool FaceCameraInThirdPerson;
        [Min(0)] public float WalkSpeed = 2, RunSpeed = 4, SprintSpeed = 6.5f, CrouchSpeed = 1.8f;
        [Min(0)] public float JumpHeight = 1.2f;
        public float Gravity = -22;
        [Min(0.8f)] public float StandingHeight = 1.8f, CrouchedHeight = 1.1f;
        public LayerMask HeadroomMask = ~4;
        CharacterController controller;
        float verticalSpeed;
        public bool IsCrouched { get; private set; }
        public bool IsGrounded
        {
            get
            {
                if (controller == null || verticalSpeed > 0) return false;
                if (controller.isGrounded) return true;
                // isGrounded can flicker at tiny frame deltas; probe with a normal test before accepting a jump.
                return Physics.SphereCast(transform.position + Vector3.up * (controller.radius + .08f), controller.radius * .9f,
                    Vector3.down, out var hit, .16f, HeadroomMask, QueryTriggerInteraction.Ignore) && hit.normal.y >= Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
            }
        }
        public Vector3 Velocity => controller == null ? Vector3.zero : controller.velocity;
        void Awake() { controller = GetComponent<CharacterController>(); if (Input == null) Input = GetComponent<PlayerInputReader>(); }
        void Update()
        {
            bool crouch = Input.Held("Crouch");
            if (!crouch && IsCrouched)
            {
                Vector3 foot = transform.position;
                crouch = Physics.CheckCapsule(foot + Vector3.up * (CrouchedHeight - controller.radius),
                    foot + Vector3.up * (StandingHeight - controller.radius), controller.radius * .95f, HeadroomMask, QueryTriggerInteraction.Ignore);
            }
            IsCrouched = crouch;
            controller.height = crouch ? CrouchedHeight : StandingHeight;
            controller.center = Vector3.up * (controller.height * .5f);
            if (VisualRoot != null) VisualRoot.localScale = new Vector3(1, controller.height / StandingHeight, 1);
            Vector2 axis = Vector2.ClampMagnitude(Input.Move, 1);
            Vector3 direction = Quaternion.Euler(0, CameraRig.Yaw, 0) * new Vector3(axis.x, 0, axis.y);
            float speed = crouch ? CrouchSpeed : Input.Held("Sprint") ? SprintSpeed : Input.Held("Walk") ? WalkSpeed : RunSpeed;
            if (IsGrounded && verticalSpeed < 0) verticalSpeed = -2;
            if (IsGrounded && !crouch && Input.Pressed("Jump")) verticalSpeed = Mathf.Sqrt(JumpHeight * -2 * Gravity);
            verticalSpeed += Gravity * Time.deltaTime;
            controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (VisualRoot != null)
            {
                Vector3 facing = (!CameraRig.ThirdPerson || FaceCameraInThirdPerson) ? Quaternion.Euler(0, CameraRig.Yaw, 0) * Vector3.forward : direction;
                if (facing.sqrMagnitude > .001f)
                    VisualRoot.rotation = Quaternion.Slerp(VisualRoot.rotation, Quaternion.LookRotation(facing), 1 - Mathf.Exp(-15 * Time.deltaTime));
            }
            if (Animator != null)
            {
                Animator.SetFloat("Speed", new Vector2(Velocity.x, Velocity.z).magnitude);
                Animator.SetBool("Grounded", IsGrounded); Animator.SetBool("Crouched", IsCrouched);
                Animator.SetFloat("VerticalSpeed", verticalSpeed);
            }
        }
    }
}
