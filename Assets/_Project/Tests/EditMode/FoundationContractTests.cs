using System;
using System.Linq;
using NUnit.Framework;

namespace SpaceK.Tests
{
    public class FoundationContractTests
    {
        [TestCase("SpaceK.Inventory.InventoryModel")]
        [TestCase("SpaceK.Crafting.CraftingJob")]
        [TestCase("SpaceK.Building.GridPlacement")]
        public void RequiredFoundationTypeExists(string name)
        {
            Assert.That(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name))
                .FirstOrDefault(t => t != null), Is.Not.Null, name + " has not been implemented.");
        }
    }
}
