using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Commons.Addressables.Editor
{
    /// <summary>
    /// Tracks the active <see cref="ContentDeliverySettings"/> without Resources: stored as an EditorBuildSettings config
    /// object (Editor) and kept in Player Settings ▸ Preloaded Assets (builds), so the asset can live anywhere.
    /// </summary>
    [InitializeOnLoad]
    internal static class ContentDeliveryRegistry
    {
        private const string CONFIG_KEY = "com.zagoradev.unity.commons.addressables.delivery";
        private const string CREATE_TITLE = "Create Content Delivery Settings";
        private const string CREATE_NAME = "ContentDeliverySettings";
        private const string CREATE_EXTENSION = "asset";
        private const string CREATE_MESSAGE = "Choose where to save the content delivery settings asset.";

        static ContentDeliveryRegistry()
            => ContentDeliverySettings.Active = Load();

        public static ContentDeliverySettings Active => ContentDeliverySettings.Active;

        public static void SetActive(ContentDeliverySettings settings)
        {
            if (settings == null)
                EditorBuildSettings.RemoveConfigObject(CONFIG_KEY);
            else
                EditorBuildSettings.AddConfigObject(CONFIG_KEY, settings, true);

            ContentDeliverySettings.Active = settings;
            SyncPreloadedAssets();
        }

        public static void SyncPreloadedAssets()
        {
            var active = Load();
            var assets = PlayerSettings.GetPreloadedAssets().ToList();
            var changed = assets.RemoveAll(asset => asset is ContentDeliverySettings && asset != active) > 0;

            if (active != null && !assets.Contains(active))
            {
                assets.Add(active);
                changed = true;
            }

            if (changed)
                PlayerSettings.SetPreloadedAssets(assets.ToArray());
        }

        public static ContentDeliverySettings CreateAndActivate()
        {
            var path = EditorUtility.SaveFilePanelInProject(CREATE_TITLE, CREATE_NAME, CREATE_EXTENSION, CREATE_MESSAGE);
            if (string.IsNullOrEmpty(path))
                return null;

            var settings = ScriptableObject.CreateInstance<ContentDeliverySettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            SetActive(settings);
            return settings;
        }

        private static ContentDeliverySettings Load()
            => EditorBuildSettings.TryGetConfigObject(CONFIG_KEY, out ContentDeliverySettings settings) ? settings : null;
    }
}
