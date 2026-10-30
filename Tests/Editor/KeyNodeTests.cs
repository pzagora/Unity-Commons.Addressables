using System.Text;
using NUnit.Framework;

namespace Commons.Addressables.Editor.Tests
{
    public sealed class KeyNodeTests
    {
        [Test]
        public void Write_ProducesSafeUniqueIdentifiersAndEscapedValues()
        {
            var root = new KeyNode("123 keys");
            root.AddConstant("class", "ui/class");
            root.AddConstant("receipt icon", "ui/receipt\"icon\\large");
            root.AddConstant("receipt-icon", "ui/receipt-icon");
            root.Child("receipt icon");

            var builder = new StringBuilder();
            root.Write(builder, 0);
            var source = builder.ToString();

            StringAssert.Contains("public static class _123_keys", source);
            StringAssert.Contains("public const string @class", source);
            StringAssert.Contains("public const string receipt_icon", source);
            StringAssert.Contains("public const string receipt_icon_2", source);
            StringAssert.Contains("public static class receipt_icon_3", source);
            StringAssert.Contains("ui/receipt\\\"icon\\\\large", source);
        }

        [Test]
        public void Write_IsDeterministicRegardlessOfInsertionOrder()
        {
            var first = SourceWithConstants(("z", "last"), ("a", "first"));
            var second = SourceWithConstants(("a", "first"), ("z", "last"));

            Assert.AreEqual(first, second);
        }

        private static string SourceWithConstants(params (string name, string value)[] constants)
        {
            var root = new KeyNode("Keys");
            foreach (var (name, value) in constants)
                root.AddConstant(name, value);

            var builder = new StringBuilder();
            root.Write(builder, 0);
            return builder.ToString();
        }
    }
}
