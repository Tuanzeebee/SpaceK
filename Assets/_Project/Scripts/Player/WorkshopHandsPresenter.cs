using SpaceK.Building;
using SpaceK.CameraSystem;
using SpaceK.Interaction;
using SpaceK.UI;
using UnityEngine;

namespace SpaceK.Player
{
    public enum WorkshopAction { Idle, Install, Rotate, Pickup, Remove }

    [DefaultExecutionOrder(11000)]
    public sealed class WorkshopHandsPresenter : MonoBehaviour
    {
        public BuildingController Building;
        public PlayerCameraRig CameraRig;
        public PlayerMotor Motor;
        public PlayerInteractor Interactor;
        public SimulatorHUD HUD;
        public UnityEngine.Camera HandsCamera;
        public Animator Animator;
        public Transform MotionRoot, CarrySocket, ToolBit;
        public Light ToolLight;
        [Min(.05f)] public float HeldPartSize = .28f;
        public WorkshopAction LastAction { get; private set; }
        public int ActionRevision { get; private set; }
        GameObject heldPart;
        BuildModuleDefinition heldDefinition;
        float toolUntil;

        void OnEnable()
        {
            if (Building == null) return;
            Building.SelectionChanged += RebuildHeldPart;
            Building.ModeChanged += OnBuildMode;
            Building.ModulePlaced += OnInstall;
            Building.Rotated += OnRotate;
            Building.ModuleRemoved += OnRemove;
            if (Interactor != null) Interactor.Interacted += OnInteract;
        }
        void Start()
        {
            if (HUD == null) HUD = Building.HUD;
            RebuildHeldPart(Building.Selected); OnBuildMode(Building.IsPlacing);
        }
        void OnDisable()
        {
            if (Building != null)
            {
                Building.SelectionChanged -= RebuildHeldPart;
                Building.ModeChanged -= OnBuildMode;
                Building.ModulePlaced -= OnInstall;
                Building.Rotated -= OnRotate;
                Building.ModuleRemoved -= OnRemove;
            }
            if (Interactor != null) Interactor.Interacted -= OnInteract;
            if (HandsCamera != null) HandsCamera.enabled = false;
            if (ToolLight != null) ToolLight.enabled = false;
        }
        void OnBuildMode(bool active) { if (Animator != null) Animator.SetBool("Carrying", active); }
        void OnInstall(PlacedShipModule module) { Play(WorkshopAction.Install); }
        void OnRotate() { Play(WorkshopAction.Rotate); }
        void OnRemove() { Play(WorkshopAction.Remove); }
        void OnInteract(IInteractable target) { if (target is PickupInteractable) Play(WorkshopAction.Pickup); }
        void Play(WorkshopAction action)
        {
            if (!isActiveAndEnabled || CameraRig.ThirdPerson || HUD.ModalOpen) return;
            LastAction = action; ActionRevision++;
            Animator.CrossFadeInFixedTime(action.ToString(), .06f, 0, 0);
            if (action == WorkshopAction.Install || action == WorkshopAction.Remove) toolUntil = Time.time + .55f;
        }
        void LateUpdate()
        {
            bool visible = !CameraRig.ThirdPerson && !HUD.ModalOpen && !CameraRig.Input.CursorReleased;
            HandsCamera.enabled = visible;
            if (heldPart != null)
                heldPart.SetActive(Building.IsPlacing && heldDefinition != null && Building.Inventory.Model.Count(heldDefinition.Cost.Item) >= heldDefinition.Cost.Amount);
            bool running = visible && Time.time < toolUntil;
            if (ToolLight != null) ToolLight.enabled = running;
            if (running && ToolBit != null) ToolBit.Rotate(Vector3.forward, 1800 * Time.deltaTime, Space.Self);
            float speed = Mathf.Clamp01(new Vector2(Motor.Velocity.x, Motor.Velocity.z).magnitude / Motor.RunSpeed);
            float lower = Physics.Raycast(HandsCamera.transform.position, HandsCamera.transform.forward, .7f, Building.SurfaceMask, QueryTriggerInteraction.Ignore) ? -.12f : 0;
            Vector3 sway = new(Mathf.Sin(Time.time * 7) * .005f * speed, lower + Mathf.Abs(Mathf.Sin(Time.time * 7)) * .008f * speed, lower * .4f);
            MotionRoot.localPosition = Vector3.Lerp(MotionRoot.localPosition, sway, 1 - Mathf.Exp(-12 * Time.deltaTime));
        }
        void RebuildHeldPart(BuildModuleDefinition definition)
        {
            if (heldPart != null) { heldPart.SetActive(false); Destroy(heldPart); }
            heldDefinition = definition;
            if (definition == null || definition.Prefab == null) return;
            heldPart = new GameObject("Held " + definition.DisplayName); heldPart.transform.SetParent(CarrySocket, false);
            float scale = HeldPartSize / Mathf.Max(definition.Size.x, definition.Size.y, definition.Size.z);
            heldPart.transform.localScale = Vector3.one * scale;
            var group = definition.Prefab.GetComponent<LODGroup>();
            var lods = group != null ? group.GetLODs() : null;
            var high = lods != null && lods.Length > 0 ? lods[0].renderers : null;
            foreach (var source in definition.Prefab.GetComponentsInChildren<MeshFilter>())
            {
                var original = source.GetComponent<Renderer>();
                if (original == null || (high != null && !System.Array.Exists(high, r => r == original))) continue;
                var child = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
                child.layer = HandsCamera.gameObject.layer; child.transform.SetParent(heldPart.transform, false);
                child.transform.localPosition = definition.Prefab.transform.InverseTransformPoint(source.transform.position);
                child.transform.localRotation = Quaternion.Inverse(definition.Prefab.transform.rotation) * source.transform.rotation;
                child.transform.localScale = source.transform.lossyScale;
                child.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var renderer = child.GetComponent<MeshRenderer>(); renderer.sharedMaterials = original.sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            heldPart.AddComponent<SpaceK.Rendering.DemoRendererCompatibility>();
        }
    }
}
