using SpaceK.Inventory;
using SpaceK.Player;
using SpaceK.UI;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceK.Building
{
    public sealed class BuildingController : MonoBehaviour
    {
        public PlayerInputReader Input;
        public InventoryComponent Inventory;
        public UnityEngine.Camera View;
        public SimulatorHUD HUD;
        public Transform AssemblyRoot;
        public BuildModuleDefinition[] Modules;
        public Material ValidGhostMaterial, InvalidGhostMaterial;
        [Min(.1f)] public float GridSize = 1;
        [Min(1)] public float Reach = 18;
        [Min(.05f)] public float AttachmentHeightGrid = .25f;
        public LayerMask SurfaceMask = ~4;
        public bool IsPlacing { get; private set; }
        public bool PlacementValid { get; private set; }
        public string Feedback { get; private set; } = "Aim at the assembly bay";
        public int SelectedIndex { get; private set; }
        public BuildModuleDefinition Selected => Modules != null && Modules.Length > 0 ? Modules[SelectedIndex % Modules.Length] : null;
        GameObject ghost;
        Renderer[] ghostRenderers;
        int quarterTurns;
        int mountSideTurns;
        bool? previousGhostValidity;
        Vector3 position;
        Quaternion rotation;
        PlacedShipModule support;
        readonly List<PlacedShipModule> history = new();
        void Update()
        {
            if (Input.Pressed("Build")) { if (IsPlacing) Cancel(); else Begin(); }
            if (!IsPlacing) return;
            if (Input.Pressed("Cancel")) { Cancel(); return; }
            if (Input.Pressed("NextModule")) Select((SelectedIndex + 1) % Modules.Length);
            if (Input.Pressed("Rotate")) quarterTurns++;
            if (Input.Pressed("NextMountSide") && Selected.Mount == ModuleMount.Radial) mountSideTurns++;
            if (!Input.WorldEnabled) { ghost.SetActive(false); return; }
            if (Input.Pressed("UndoBuild")) { UndoLastPlacement(); return; }
            if (Input.Pressed("RemoveModule")) { TryRemoveAimed(); return; }
            EvaluatePlacement();
            if (Input.Pressed("Place")) TryPlace();
        }
        public void Begin()
        {
            if (Selected == null || !Selected.IsValid) { HUD.Notify("No build module configured"); return; }
            IsPlacing = true; quarterTurns = 0; mountSideTurns = 0; CreateGhost();
        }
        public void Select(int index)
        {
            if (Modules == null || index < 0 || index >= Modules.Length || Modules[index] == null || !Modules[index].IsValid) return;
            SelectedIndex = index;
            quarterTurns = 0;
            mountSideTurns = 0;
            if (IsPlacing) CreateGhost();
        }
        void CreateGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = new GameObject("Module Ghost"); ghost.layer = 2;
            var lodGroup = Selected.Prefab.GetComponent<LODGroup>();
            var lods = lodGroup != null ? lodGroup.GetLODs() : null;
            var previewRenderers = lods != null && lods.Length > 0 ? lods[0].renderers : null;
            // Copy render meshes only: ghost previews must never run prefab gameplay scripts.
            foreach (var source in Selected.Prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (previewRenderers != null && !System.Array.Exists(previewRenderers, renderer => renderer == source.GetComponent<Renderer>())) continue;
                var child = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
                child.layer = 2; child.transform.SetParent(ghost.transform, false);
                child.transform.localPosition = Selected.Prefab.transform.InverseTransformPoint(source.transform.position);
                child.transform.localRotation = Quaternion.Inverse(Selected.Prefab.transform.rotation) * source.transform.rotation;
                child.transform.localScale = source.transform.lossyScale;
                child.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            }
            ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
            ghost.AddComponent<SpaceK.Rendering.DemoRendererCompatibility>();
            previousGhostValidity = null;
        }
        public void EvaluatePlacement()
        {
            PlacementValid = false;
            support = null;
            bool found = Aim(out var hit);
            bool supported = false;
            if (found)
            {
                support = hit.collider.GetComponentInParent<PlacedShipModule>();
                Vector3 mountNormal = hit.normal;
                Vector3 mountPoint = hit.point;
                // A fitted fin must not hide the hull's other radial connectors from the crosshair.
                if (Owns(support) && Selected.Mount == ModuleMount.Radial && support.Definition != null
                    && support.Definition.Mount == ModuleMount.Radial && Owns(support.Support))
                {
                    mountPoint = support.transform.position;
                    support = support.Support;
                    mountNormal = Vector3.ProjectOnPlane(mountPoint - support.transform.position, Vector3.up).normalized;
                }
                if (Owns(support))
                    supported = ModuleAttachment.TryPose(support, Selected, mountPoint, mountNormal, quarterTurns,
                        AttachmentHeightGrid, out position, out rotation, mountSideTurns);
                else if (hit.normal.y > .95f && hit.collider.GetComponentInParent<PlacementSurface>() != null)
                {
                    support = null;
                    position = GridPlacement.Snap(hit.point, GridSize) + Vector3.up * (Selected.Size.y * .5f + .015f);
                    rotation = GridPlacement.Rotation(quarterTurns);
                    supported = GridPlacement.FitsOnSurface(hit.collider as BoxCollider, position, Selected.Size, rotation);
                }
                else found = false;
            }
            ghost.SetActive(found);
            if (!found) { Feedback = "Aim at the assembly bay or an installed module"; return; }
            if (!supported && support != null)
            {
                ghost.SetActive(false);
                Feedback = Selected.Mount == ModuleMount.Radial ? "Aim at a hull side to attach this part" : "This connector cannot support this module";
                return;
            }
            ghost.transform.SetPositionAndRotation(position, rotation);
            bool occupied = Physics.CheckBox(position, Vector3.Max(Selected.Size * .5f - Vector3.one * .01f, Vector3.one * .002f), rotation, ~4, QueryTriggerInteraction.Ignore);
            bool hasItem = Inventory.Model.Count(Selected.Cost.Item) >= Selected.Cost.Amount;
            PlacementValid = supported && !occupied && hasItem;
            Feedback = !supported ? "Module must fit fully on the support surface" : occupied ? "Space occupied" : !hasItem ? "Craft this module at the workbench" : "Ready to install";
            if (previousGhostValidity == PlacementValid) return;
            previousGhostValidity = PlacementValid;
            foreach (var renderer in ghostRenderers)
            {
                var materials = new Material[renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount];
                for (int i = 0; i < materials.Length; i++) materials[i] = PlacementValid ? ValidGhostMaterial : InvalidGhostMaterial;
                renderer.sharedMaterials = materials;
            }
        }
        public bool TryPlace()
        {
            if (!IsPlacing || Selected == null || !Selected.IsValid || ghost == null || !Input.WorldEnabled) return false;
            EvaluatePlacement();
            if (!PlacementValid || !Inventory.Model.TryRemove(Selected.Cost.Item, Selected.Cost.Amount)) return false;
            var placed = Instantiate(Selected.Prefab, position, rotation, AssemblyRoot);
            var module = placed.GetComponent<PlacedShipModule>();
            if (module == null) module = placed.AddComponent<PlacedShipModule>();
            module.Definition = Selected;
            module.Support = support;
            module.PaidCost = Selected.Cost;
            history.Add(module);
            Physics.SyncTransforms(); HUD.Notify(Selected.DisplayName + " installed"); return true;
        }
        bool Aim(out RaycastHit hit)
        {
            Vector3 playerCenter = Input.transform.position + Vector3.up;
            float offset = Vector3.Distance(View.transform.position, playerCenter);
            return Physics.Raycast(View.ViewportPointToRay(new Vector3(.5f, .5f)), out hit, Reach + offset, SurfaceMask, QueryTriggerInteraction.Ignore)
                && Vector3.SqrMagnitude(hit.point - playerCenter) <= Reach * Reach;
        }
        bool Owns(PlacedShipModule module) => module != null && module.gameObject.activeInHierarchy
            && AssemblyRoot != null && module.transform.IsChildOf(AssemblyRoot);
        public bool TryRemoveAimed()
        {
            if (!IsPlacing || !Input.WorldEnabled || !Aim(out var hit)) return false;
            return TryRemove(hit.collider.GetComponentInParent<PlacedShipModule>());
        }
        public bool UndoLastPlacement()
        {
            if (!IsPlacing || !Input.WorldEnabled) return false;
            history.RemoveAll(module => !Owns(module));
            if (history.Count == 0) { HUD.Notify("No installations to undo"); return false; }
            return TryRemove(history[history.Count - 1]);
        }
        public bool TryRemove(PlacedShipModule module)
        {
            if (!IsPlacing || !Input.WorldEnabled || !Owns(module)) return false;
            foreach (var attached in AssemblyRoot.GetComponentsInChildren<PlacedShipModule>())
                if (attached.Support == module) { HUD.Notify("Remove attached modules first"); return false; }
            var refund = module.PaidCost;
            if (refund.Item == null || refund.Amount <= 0) return false;
            if (!Inventory.Model.TryAdd(refund.Item, refund.Amount)) { HUD.Notify("Cargo full - make room before removing this module"); return false; }
            history.Remove(module);
            module.gameObject.SetActive(false);
            Destroy(module.gameObject);
            Physics.SyncTransforms(); HUD.Notify("Module removed - item returned to cargo");
            return true;
        }
        public void Cancel()
        {
            IsPlacing = false; PlacementValid = false;
            if (ghost != null) Destroy(ghost);
        }
        void OnDisable() { Cancel(); }
    }
}
