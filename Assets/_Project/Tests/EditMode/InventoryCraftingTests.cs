using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpaceK.Building;
using SpaceK.Crafting;
using SpaceK.Inventory;
using UnityEngine;

namespace SpaceK.Tests
{
    public class InventoryCraftingTests
    {
        readonly List<UnityEngine.Object> allocated = new();
        ItemData Item(string id, int stack = 5)
        {
            var item = ScriptableObject.CreateInstance<ItemData>(); item.Id = id; item.DisplayName = id; item.MaxStack = stack; allocated.Add(item); return item;
        }
        CraftingRecipe Recipe(ItemData material, ItemData output, float duration = 2)
        {
            var recipe = ScriptableObject.CreateInstance<CraftingRecipe>(); allocated.Add(recipe);
            recipe.Materials = new[] { new ItemAmount(material, 2), new ItemAmount(material, 1) };
            recipe.Output = new ItemAmount(output, 1); recipe.Duration = duration; return recipe;
        }
        [TearDown] public void Cleanup() { foreach (var value in allocated) UnityEngine.Object.DestroyImmediate(value); allocated.Clear(); }
        [Test] public void StackSplitsAcrossSlotsAndSearchesById()
        {
            var item = Item("alloy"); var inventory = new InventoryModel(2);
            Assert.That(inventory.TryAdd(item, 8), Is.True);
            Assert.That(inventory.Stacks.Count, Is.EqualTo(2)); Assert.That(inventory.Stacks[0].Amount, Is.EqualTo(5));
            Assert.That(inventory.Count(Item("alloy")), Is.EqualTo(8)); Assert.That(inventory.Count("alloy"), Is.EqualTo(8));
        }
        [Test] public void CapacityFailureDoesNotPartiallyAddOrFireChanged()
        {
            var item = Item("alloy"); var inventory = new InventoryModel(1); int changes = 0; inventory.Changed += () => changes++;
            inventory.TryAdd(item, 4); Assert.That(inventory.TryAdd(item, 2), Is.False);
            Assert.That(inventory.Count(item), Is.EqualTo(4)); Assert.That(changes, Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(-1)] [TestCase(int.MinValue)]
        public void InvalidQuantitiesCannotMutateInventory(int amount)
        {
            var inventory = new InventoryModel(); var item = Item("a"); inventory.TryAdd(item, 2);
            Assert.That(inventory.TryAdd(item, amount), Is.False); Assert.That(inventory.TryRemove(item, amount), Is.False); Assert.That(inventory.Count(item), Is.EqualTo(2));
        }
        [Test] public void NonStackableUsesOneSlotPerItemAndRemovalIsAtomic()
        {
            var inventory = new InventoryModel(2); var item = Item("tool", 1); Assert.That(inventory.TryAdd(item, 2), Is.True);
            Assert.That(inventory.Stacks.Count, Is.EqualTo(2)); Assert.That(inventory.TryRemove(item, 3), Is.False); Assert.That(inventory.Count(item), Is.EqualTo(2));
            Assert.That(inventory.TryRemove(item, 2), Is.True); Assert.That(inventory.Stacks, Is.Empty);
        }
        [Test] public void DuplicateRecipeMaterialsCannotSpendTheSameItemsTwice()
        {
            var material = Item("a"); var output = Item("b"); var recipe = Recipe(material, output); var inventory = new InventoryModel(); inventory.TryAdd(material, 2);
            Assert.That(new CraftingJob().Start(recipe, inventory), Is.False); Assert.That(inventory.Count(material), Is.EqualTo(2));
        }
        [Test] public void TimedCraftCommitsOnlyAfterDurationAndOnlyOnce()
        {
            var a = Item("a"); var b = Item("b"); var inventory = new InventoryModel(1); inventory.TryAdd(a, 3); var job = new CraftingJob();
            Assert.That(job.Start(Recipe(a, b), inventory), Is.True); job.Tick(1);
            Assert.That(inventory.Count(a), Is.EqualTo(3)); Assert.That(job.Progress, Is.EqualTo(.5f)); job.Tick(1); job.Tick(100);
            Assert.That(job.Succeeded, Is.True); Assert.That(inventory.Count(a), Is.Zero); Assert.That(inventory.Count(b), Is.EqualTo(1));
        }
        [Test] public void CancelRetainsMaterialsAndDoesNotProduceOutput()
        {
            var a = Item("a"); var b = Item("b"); var inventory = new InventoryModel(); inventory.TryAdd(a, 3); var job = new CraftingJob();
            job.Start(Recipe(a, b), inventory); job.Tick(.5f); job.Cancel(); job.Tick(10);
            Assert.That(inventory.Count(a), Is.EqualTo(3)); Assert.That(inventory.Count(b), Is.Zero); Assert.That(job.IsRunning, Is.False);
        }
        [Test] public void OutputCapacityIsValidatedBeforeCraftBegins()
        {
            var a = Item("a"); var b = Item("b"); var inventory = new InventoryModel(1); inventory.TryAdd(a, 4);
            Assert.That(new CraftingJob().Start(Recipe(a, b), inventory), Is.False); Assert.That(inventory.Count(a), Is.EqualTo(4));
        }
        [Test] public void InventoryChangesDuringCraftNeverCausePartialConsumption()
        {
            var a = Item("a"); var b = Item("b"); var inventory = new InventoryModel(); inventory.TryAdd(a, 3); var job = new CraftingJob();
            job.Start(Recipe(a, b), inventory); inventory.TryRemove(a, 1); job.Tick(2);
            Assert.That(job.Succeeded, Is.False); Assert.That(inventory.Count(a), Is.EqualTo(2)); Assert.That(inventory.Count(b), Is.Zero);
        }
        [Test] public void ActiveJobCannotBeReplacedAndInvalidTimeDoesNotAdvanceIt()
        {
            var a = Item("a"); var b = Item("b"); var inventory = new InventoryModel(); inventory.TryAdd(a, 3); var job = new CraftingJob(); var recipe = Recipe(a, b);
            Assert.That(job.Start(recipe, inventory), Is.True); Assert.That(job.Start(recipe, inventory), Is.False);
            job.Tick(float.NaN); job.Tick(-1); job.Tick(float.PositiveInfinity); Assert.That(job.Progress, Is.Zero);
        }
        [Test] public void GridSnapPreservesHeightAndQuarterTurnRotation()
        {
            Assert.That(GridPlacement.Snap(new Vector3(1.2f, 3, -1.7f), .5f), Is.EqualTo(new Vector3(1, 3, -1.5f)));
            Assert.That(Vector3.Distance(GridPlacement.Rotation(1) * Vector3.forward, Vector3.right), Is.LessThan(.001f));
            Assert.Throws<ArgumentOutOfRangeException>(() => GridPlacement.Snap(Vector3.one, 0));
        }
        [Test] public void RotatedFootprintsMustFitTheEntireSupportSurface()
        {
            var surface = new GameObject("Support"); allocated.Add(surface); var box = surface.AddComponent<BoxCollider>(); box.size = new(4, .1f, 2);
            Assert.That(GridPlacement.FitsOnSurface(box, Vector3.zero, new(3, 1, 1), Quaternion.identity), Is.True);
            Assert.That(GridPlacement.FitsOnSurface(box, Vector3.zero, new(3, 1, 1), GridPlacement.Rotation(1)), Is.False);
            Assert.That(GridPlacement.FitsOnSurface(box, new(2, 1, 0), Vector3.one, Quaternion.identity), Is.False);
            Assert.That(GridPlacement.FitsOnSurface(box, new(1.5f, 1, 0), Vector3.one, Quaternion.identity), Is.True);
        }
    }
}
