using SpaceK.Player;
using Unity.Cinemachine;
using UnityEngine;

namespace SpaceK.CameraSystem
{
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        public PlayerInputReader Input;
        public PlayerMotor Motor;
        public Transform AimTarget;
        public CinemachineCamera FirstPersonCamera, ThirdPersonCamera;
        public Renderer[] BodyRenderers;
        [Min(.01f)] public float MouseSensitivity = .12f;
        [Min(1)] public float GamepadDegreesPerSecond = 150;
        [Min(.5f)] public float ThirdPersonDistance = 4;
        public Vector2 PitchLimits = new(-75, 75);
        public bool ThirdPerson { get; private set; }
        public float Yaw { get; private set; }
        float pitch;
        CinemachineThirdPersonFollow follow;
        public event System.Action<bool> ModeChanged;
        void Awake() { Yaw = transform.eulerAngles.y; follow = ThirdPersonCamera.GetComponent<CinemachineThirdPersonFollow>(); }
        void Start() { ApplyMode(); }
        void Update()
        {
            if (Input.Pressed("SwitchCamera")) ToggleMode();
            Vector2 look = Input.Look * (Input.GamepadLook ? GamepadDegreesPerSecond * Time.deltaTime : MouseSensitivity);
            Yaw = Mathf.Repeat(Yaw + look.x, 360); pitch = Mathf.Clamp(pitch - look.y, PitchLimits.x, PitchLimits.y);
            AimTarget.position = transform.position + Vector3.up * (Motor.IsCrouched ? Motor.CrouchedHeight - .15f : Motor.StandingHeight - .15f);
            AimTarget.rotation = Quaternion.Euler(pitch, Yaw, 0);
            follow.CameraDistance = ThirdPersonDistance;
        }
        public void ToggleMode() { ThirdPerson = !ThirdPerson; ApplyMode(); ModeChanged?.Invoke(ThirdPerson); }
        void ApplyMode()
        {
            FirstPersonCamera.Priority = ThirdPerson ? 0 : 20;
            ThirdPersonCamera.Priority = ThirdPerson ? 20 : 0;
            foreach (var body in BodyRenderers) if (body != null) body.enabled = ThirdPerson;
        }
    }
}
