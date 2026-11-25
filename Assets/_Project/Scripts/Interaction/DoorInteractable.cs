using UnityEngine;

namespace SpaceK.Interaction
{
    public sealed class DoorInteractable : MonoBehaviour, IInteractable
    {
        public Transform Panel;
        public Vector3 OpenOffset = new(0, 3.1f, 0);
        public float Speed = 3;
        public bool IsOpen { get; private set; }
        Vector3 closed;
        public string Prompt => IsOpen ? "Close airlock" : "Open airlock";
        public bool CanInteract(InteractionContext context) => Panel != null;
        public void Interact(InteractionContext context) { IsOpen = !IsOpen; }
        void Awake() { if (Panel != null) closed = Panel.localPosition; }
        void Update() { if (Panel != null) Panel.localPosition = Vector3.MoveTowards(Panel.localPosition, closed + (IsOpen ? OpenOffset : Vector3.zero), Speed * Time.deltaTime); }
    }
}
