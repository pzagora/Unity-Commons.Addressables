using UnityEngine;

namespace Commons.Addressables
{
    /// <summary>
    /// Base URL of remote Addressables content for each <see cref="ContentEnvironment"/>, and which environment the
    /// Editor, development builds and release builds use. Not Resources-bound: the Editor registers the active asset
    /// (Project Settings ▸ zagora.dev ▸ Commons Addressables) and builds ship it as a Preloaded Asset, where it registers
    /// itself. The live and development URLs are per project and start empty; without an asset only Local works.
    /// </summary>
    [CreateAssetMenu(menuName = "Scriptable Objects/Commons/Content Delivery Settings", fileName = "ContentDeliverySettings")]
    public sealed class ContentDeliverySettings : ScriptableObject
    {
        [Header("Base URLs")]
        [Tooltip("Released content, e.g. https://cdn.example.com/MyGame/addressables/live. Only a release from development publishes here.")]
        [SerializeField] private string liveUrl = string.Empty;
        [Tooltip("Content uploaded by builds, e.g. https://cdn.example.com/MyGame/addressables/development.")]
        [SerializeField] private string developmentUrl = string.Empty;
        [Tooltip("A local copy, e.g. http://192.168.1.10:8000 or file:///path. Empty: the project's ServerData folder in the Editor; development content in builds.")]
        [SerializeField] private string localUrl = string.Empty;

        [Header("Environment")]
        [Tooltip("Used in the Editor (Play mode with an existing Addressables build).")]
        [SerializeField] private ContentEnvironment editor = ContentEnvironment.Local;
        [Tooltip("Used by development builds.")]
        [SerializeField] private ContentEnvironment developmentBuilds = ContentEnvironment.Development;
        [Tooltip("Used by release builds.")]
        [SerializeField] private ContentEnvironment releaseBuilds = ContentEnvironment.Live;

        public static ContentDeliverySettings Active { get; set; }

        public ContentEnvironment Configured => Application.isEditor
            ? editor
            : EnvironmentFor(Debug.isDebugBuild);

        public ContentEnvironment EnvironmentFor(bool developmentBuild)
            => developmentBuild ? developmentBuilds : releaseBuilds;

        public string UrlFor(ContentEnvironment environment) => environment switch
        {
            ContentEnvironment.Live => liveUrl,
            ContentEnvironment.Development => developmentUrl,
            _ => localUrl
        };

        private void OnEnable()
        {
            if (!Application.isEditor && Active == null)
                Active = this;
        }
    }
}
