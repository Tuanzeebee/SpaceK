using UnityEngine;

namespace SpaceK.Interaction
{
    /// <summary>The door control stays on the jamb while its panel clears the aperture.</summary>
    public sealed class WorkshopHangarDoor : MonoBehaviour, IInteractable
    {
        public Transform Panel;
        public Vector3 ClosedLocalPosition = new(0, 3.5f, 0);
        public Vector3 OpenOffset = new(0, 7.4f, 0);
        public float Speed = 2.5f;
        public bool StartsOpen = true;
        public bool IsOpen { get; private set; }
        public string Prompt => IsOpen ? "Close hangar" : "Open hangar";
        public bool CanInteract(InteractionContext context) => Panel != null;
        public void Interact(InteractionContext context) => IsOpen = !IsOpen;
        void Awake()
        {
            IsOpen = StartsOpen;
            if (Panel != null) Panel.localPosition = ClosedLocalPosition + (IsOpen ? OpenOffset : Vector3.zero);
        }
        void Update()
        {
            if (Panel != null) Panel.localPosition = Vector3.MoveTowards(Panel.localPosition, ClosedLocalPosition + (IsOpen ? OpenOffset : Vector3.zero), Speed * Time.deltaTime);
        }
    }
}

