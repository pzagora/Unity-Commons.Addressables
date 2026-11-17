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

        [Test]
        public void Write_PascalCaseConvertsAllClassesAndPreservesConstantsAndAddresses()
        {
            var root = new KeyNode("addressable keys");
            root.Child("localization").Child("front-end").Child("login_panel")
                .AddConstant("language_key", "localization/front-end/login_panel/language_key");

            var builder = new StringBuilder();
            root.Write(builder, 0, true);
            var source = builder.ToString();

            StringAssert.Contains("public static class AddressableKeys", source);
            StringAssert.Contains("public static class Localization", source);
            StringAssert.Contains("public static class FrontEnd", source);
            StringAssert.Contains("public static class LoginPanel", source);
            StringAssert.Contains("public const string language_key = \"localization/front-end/login_panel/language_key\";", source);
        }

        [Test]
        public void Write_PascalCaseResolvesCollisionsAfterConversion()
        {
            var root = new KeyNode("keys");
            root.AddConstant("FrontEnd", "original-address");
            root.Child("front end");
            root.Child("front-end");
            root.Child("keys");

            var builder = new StringBuilder();
            root.Write(builder, 0, true);
            var source = builder.ToString();

            StringAssert.Contains("public static class FrontEnd_2", source);
            StringAssert.Contains("public static class FrontEnd_3", source);
            StringAssert.Contains("public static class Keys_2", source);
        }

        [TestCase("123 icons", "_123Icons")]
        [TestCase("class", "Class")]
        [TestCase("---", "_")]
        [TestCase("loginPanel", "LoginPanel")]
        [TestCase("UI", "UI")]
        public void Write_PascalCaseProducesSafeClassNames(string name, string expected)
        {
            var builder = new StringBuilder();
            new KeyNode(name).Write(builder, 0, true);

            StringAssert.Contains("public static class " + expected, builder.ToString());
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
