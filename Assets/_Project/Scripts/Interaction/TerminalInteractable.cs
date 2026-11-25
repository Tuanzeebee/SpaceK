using UnityEngine;

namespace SpaceK.Interaction
{
    public sealed class TerminalInteractable : MonoBehaviour, IInteractable
    {
        [TextArea] public string Status = "HANGAR 07 / SYSTEMS ONLINE\nCollect alloy. Fabricate hull modules at the workbench.\nPress B inside the assembly bay to install modules.";
        public string Prompt => "Access hangar terminal";
        public bool CanInteract(InteractionContext context) => true;
        public void Interact(InteractionContext context) { context.HUD?.OpenTerminal(Status); }
    }
}
