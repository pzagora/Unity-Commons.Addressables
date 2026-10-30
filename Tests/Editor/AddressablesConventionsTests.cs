using NUnit.Framework;

namespace Commons.Addressables.Editor.Tests
{
    public sealed class AddressablesConventionsTests
    {
        [Test]
        public void TryParse_MapsAssetToGroupAndExtensionlessAddress()
        {
            var parsed = AddressablesConventions.TryParse(
                "Assets/Game/Addressables/UI/Icons/Receipt.png",
                "Addressables",
                out var group,
                out var address);

            Assert.IsTrue(parsed);
            Assert.AreEqual("UI", group);
            Assert.AreEqual("UI/Icons/Receipt", address);
        }

        [Test]
        public void TryParse_UsesInnermostConventionFolder()
        {
            var parsed = AddressablesConventions.TryParse(
                "Assets/Addressables/Outer/Feature/Addressables/Inner/Item.prefab",
                "Addressables",
                out var group,
                out var address);

            Assert.IsTrue(parsed);
            Assert.AreEqual("Inner", group);
            Assert.AreEqual("Inner/Item", address);
        }

        [TestCase("Packages/com.example/Addressables/UI/Icon.png")]
        [TestCase("Assets/Addressables/UI/Editor/Icon.png")]
        [TestCase("Assets/Addressables/UI/Resources/Icon.png")]
        [TestCase("Assets/Addressables/UI/Widget.cs")]
        [TestCase("Assets/Addressables/UI")]
        public void TryParse_RejectsUnsupportedPaths(string path)
        {
            Assert.IsFalse(AddressablesConventions.TryParse(
                path,
                "Addressables",
                out _,
                out _));
        }
    }
}
