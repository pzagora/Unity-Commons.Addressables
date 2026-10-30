using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Commons.Addressables.Tests
{
    public sealed class ContentDeliverySettingsTests
    {
        private ContentDeliverySettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<ContentDeliverySettings>();
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("liveUrl").stringValue = "https://cdn.example/live";
            serialized.FindProperty("developmentUrl").stringValue = "https://cdn.example/development";
            serialized.FindProperty("localUrl").stringValue = "http://localhost:8000";
            serialized.FindProperty("developmentBuilds").enumValueIndex = (int)ContentEnvironment.Local;
            serialized.FindProperty("releaseBuilds").enumValueIndex = (int)ContentEnvironment.Development;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(_settings);

        [Test]
        public void EnvironmentFor_UsesConfiguredBuildKind()
        {
            Assert.AreEqual(ContentEnvironment.Local, _settings.EnvironmentFor(developmentBuild: true));
            Assert.AreEqual(ContentEnvironment.Development, _settings.EnvironmentFor(developmentBuild: false));
        }

        [TestCase(ContentEnvironment.Live, "https://cdn.example/live")]
        [TestCase(ContentEnvironment.Development, "https://cdn.example/development")]
        [TestCase(ContentEnvironment.Local, "http://localhost:8000")]
        public void UrlFor_ReturnsEnvironmentUrl(ContentEnvironment environment, string expected)
            => Assert.AreEqual(expected, _settings.UrlFor(environment));
    }
}
