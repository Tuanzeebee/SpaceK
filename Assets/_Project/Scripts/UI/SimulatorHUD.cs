using System.Collections.Generic;
using System.Text;
using SpaceK.Building;
using SpaceK.CameraSystem;
using SpaceK.Crafting;
using SpaceK.Inventory;
using SpaceK.Player;
using TMPro;
using UnityEngine;

namespace SpaceK.UI
{
    public sealed class SimulatorHUD : MonoBehaviour
    {
        public PlayerInputReader Input;
        public InventoryComponent Inventory;
        public PlayerCameraRig CameraRig;
        public BuildingController Building;
        public CraftingStation[] Stations;
        public TMP_Text Prompt, CameraMode, Toast, BuildStatus, InventorySummary, CraftStatus, TerminalText;
        public GameObject InventoryPanel, CraftingPanel, TerminalPanel, BuildPanel;
        public RectTransform InventoryContent, RecipeContent;
        public UnityEngine.UI.Button RowPrefab, CancelCraftButton;
        public UnityEngine.UI.Slider CraftProgress;
        readonly List<GameObject> inventoryRows = new(), recipeRows = new();
        CraftingStation station;
        float toastUntil;
        public bool ModalOpen => InventoryPanel.activeSelf || CraftingPanel.activeSelf || TerminalPanel.activeSelf;
        void Start()
        {
            Inventory.Model.Changed += RefreshInventory;
            CameraRig.ModeChanged += OnCameraChanged;
            CancelCraftButton.onClick.AddListener(CancelCraft);
            OnCameraChanged(CameraRig.ThirdPerson); RefreshInventory();
        }
        void OnDestroy()
        {
            if (Inventory != null) Inventory.Model.Changed -= RefreshInventory;
            if (CameraRig != null) CameraRig.ModeChanged -= OnCameraChanged;
        }
        void Update()
        {
            bool escape = Input.RawPressed("Pause");
            if (escape)
            {
                if (ModalOpen) ClosePanels();
                else if (Building.IsPlacing) Building.Cancel();
                else Input.SetCursor(!Input.CursorReleased);
            }
            if (Input.RawPressed("Inventory")) { if (InventoryPanel.activeSelf) ClosePanels(); else OpenInventory(); }
            if (Input.RawPressed("Crafting"))
            {
                if (CraftingPanel.activeSelf) ClosePanels();
                else
                {
                    var nearest = FindStation();
                    if (nearest != null) OpenCrafting(nearest); else Notify("Move closer to a fabrication workbench");
                }
            }
            if (!ModalOpen && Input.CursorReleased && Input.RawPressed("Place") && !escape) Input.SetCursor(false);
            if (station != null && CraftingPanel.activeSelf)
            {
                if (!InRange(station)) ClosePanels();
                else
                {
                    CraftProgress.value = station.Job.Progress;
                    CraftStatus.text = station.Job.Message + (station.Job.IsRunning ? "  " + Mathf.RoundToInt(station.Job.Progress * 100) + "%" : "");
                    CancelCraftButton.interactable = station.Job.IsRunning;
                }
            }
            BuildPanel.SetActive(Building.IsPlacing && !ModalOpen);
            if (Building.IsPlacing && Building.Selected != null)
                BuildStatus.text = "ASSEMBLY MODE / " + Building.Selected.DisplayName.ToUpperInvariant() + "\n" + Building.Feedback + "\nR Rotate   F Mount side   Q Next   LMB Place   X Remove   Z Undo   RMB / B Exit";
            if (Time.unscaledTime > toastUntil) Toast.text = "";
        }
        bool InRange(CraftingStation candidate) => Vector3.Distance(Input.transform.position, candidate.transform.position) <= candidate.UseRange;
        CraftingStation FindStation()
        {
            CraftingStation closest = null; float distance = float.MaxValue;
            foreach (var candidate in Stations)
            {
                if (candidate == null || !InRange(candidate)) continue;
                float next = Vector3.SqrMagnitude(candidate.transform.position - Input.transform.position);
                if (next < distance) { distance = next; closest = candidate; }
            }
            return closest;
        }
        void SetModal(GameObject panel)
        {
            InventoryPanel.SetActive(false); CraftingPanel.SetActive(false); TerminalPanel.SetActive(false);
            panel.SetActive(true); Building.Cancel(); Input.GameplayBlocked = true; Input.SetCursor(true);
            SetPrompt("");
        }
        public void OpenInventory() { SetModal(InventoryPanel); RefreshInventory(); }
        public void OpenCrafting(CraftingStation target)
        {
            if (target == null || !InRange(target)) { Notify("Workbench out of reach"); return; }
            station = target; SetModal(CraftingPanel); RefreshRecipes();
        }
        public void OpenTerminal(string status) { SetModal(TerminalPanel); TerminalText.text = status; }
        public void ClosePanels()
        {
            InventoryPanel.SetActive(false); CraftingPanel.SetActive(false); TerminalPanel.SetActive(false);
            Input.GameplayBlocked = false; Input.SetCursor(false);
        }
        public void SetPrompt(string value) { Prompt.text = ModalOpen ? "" : value; }
        public void Notify(string value) { Toast.text = value; toastUntil = Time.unscaledTime + 3.5f; }
        void OnCameraChanged(bool third) { CameraMode.text = "CAM / " + (third ? "THIRD PERSON" : "FIRST PERSON") + "   [V]"; }
        void RefreshInventory()
        {
            Clear(inventoryRows);
            InventorySummary.text = "CARGO / " + Inventory.Model.Stacks.Count + " OF " + Inventory.Model.SlotCapacity + " SLOTS";
            foreach (var stack in Inventory.Model.Stacks)
            {
                var row = CreateRow(InventoryContent, inventoryRows, stack.Item.Icon);
                row.interactable = false;
                row.GetComponentInChildren<TMP_Text>().text = stack.Item.DisplayName + "   x" + stack.Amount + "\n<size=14>" + stack.Item.Description + "</size>";
            }
            if (Inventory.Model.Stacks.Count == 0) CreateRow(InventoryContent, inventoryRows, null).GetComponentInChildren<TMP_Text>().text = "Cargo hold empty";
            if (CraftingPanel.activeSelf) RefreshRecipes();
        }
        void RefreshRecipes()
        {
            Clear(recipeRows);
            if (station == null) return;
            foreach (var recipe in station.Recipes)
            {
                if (recipe == null || !recipe.IsValid) continue;
                var row = CreateRow(RecipeContent, recipeRows, recipe.Output.Item.Icon);
                var text = new StringBuilder(recipe.DisplayName + "  /  " + recipe.Duration.ToString("0.#") + "s\n<size=14>");
                foreach (var material in recipe.Materials) text.Append(material.Item.DisplayName).Append(' ').Append(Inventory.Model.Count(material.Item)).Append('/').Append(material.Amount).Append("   ");
                row.GetComponentInChildren<TMP_Text>().text = text.Append("</size>").ToString();
                var selected = recipe;
                row.onClick.AddListener(() =>
                {
                    if (!InRange(station)) { ClosePanels(); return; }
                    if (!station.Job.Start(selected, Inventory.Model)) Notify(station.Job.IsRunning ? "Workbench is already fabricating" : station.Job.Message);
                });
            }
        }
        UnityEngine.UI.Button CreateRow(Transform parent, List<GameObject> rows, Sprite icon)
        {
            var row = Instantiate(RowPrefab, parent, false); row.gameObject.SetActive(true); rows.Add(row.gameObject);
            var image = row.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>(); image.sprite = icon; image.enabled = icon != null;
            return row;
        }
        static void Clear(List<GameObject> rows)
        {
            foreach (var row in rows) { row.SetActive(false); Destroy(row); } rows.Clear();
        }
        void CancelCraft() { if (station != null) station.Job.Cancel(); }
    }
}
