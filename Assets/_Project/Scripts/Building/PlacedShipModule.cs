using UnityEngine;

namespace SpaceK.Building
{
    public sealed class PlacedShipModule : MonoBehaviour
    {
        public BuildModuleDefinition Definition;
        public PlacedShipModule Support;
        public SpaceK.Inventory.ItemAmount PaidCost;
        public Vector3[] LocalSockets => new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back, Vector3.up, Vector3.down };
    }
}
