using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceK.Player
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public InputActionAsset Actions;
        InputActionAsset instance;
        InputActionMap map;
        public bool GameplayBlocked { get; set; }
        public bool CursorReleased { get; private set; }
        public bool WorldEnabled => !GameplayBlocked && !CursorReleased;
        public Vector2 Move => WorldEnabled ? Action("Move").ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => WorldEnabled ? Action("Look").ReadValue<Vector2>() : Vector2.zero;
        public bool GamepadLook => Action("Look").activeControl?.device is Gamepad;
        public bool Held(string name) => WorldEnabled && Action(name).IsPressed();
        public bool Pressed(string name) => WorldEnabled && RawPressed(name);
        public bool RawPressed(string name) => map != null && Action(name).WasPressedThisFrame();
        InputAction Action(string name) => map.FindAction(name, true);
        void Awake()
        {
            if (Actions == null) { Debug.LogError("Player input actions are missing.", this); enabled = false; return; }
            instance = Instantiate(Actions); map = instance.FindActionMap("Gameplay", true);
        }
        void OnEnable() { map?.Enable(); SetCursor(false); }
        void OnDisable() { map?.Disable(); SetCursor(true); }
        void OnDestroy() { if (instance != null) Destroy(instance); }
        public void SetCursor(bool released)
        {
            CursorReleased = released;
            Cursor.lockState = released ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = released;
        }
        void OnApplicationFocus(bool focused) { if (!focused) SetCursor(true); }
    }
}
