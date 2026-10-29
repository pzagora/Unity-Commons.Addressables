using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Commons.Addressables.Editor
{
    /// <summary>
    /// Keeps the Addressables settings in line with Content Delivery: every profile's remote load path uses
    /// <see cref="ContentDelivery.RemoteUrl"/>, the remote catalog (always on with Remote Only) builds and loads from the
    /// remote paths, and the catalog is named <c>catalog_content</c> (overwritten by every release) or, with versioned
    /// catalogs, <c>catalog_&lt;version&gt;</c>, and remote groups get a request timeout and retries (a hung download would
    /// otherwise stall loading forever). Groups reference profile variables by id, so the existing Remote.* variables
    /// (Remote* in projects created before Addressables 1.19) are updated and only created when missing.
    /// </summary>
    internal static class ContentDeliverySetup
    {
        public const string UNVERSIONED_CATALOG = "content";

        private const string LEGACY_REMOTE_LOAD_PATH = "RemoteLoadPath";
        private const string LEGACY_REMOTE_BUILD_PATH = "RemoteBuildPath";
        private const int DEFAULT_TIMEOUT_SECONDS = 30;
        private const int DEFAULT_RETRY_COUNT = 2;

        public static bool HasSettings => AddressableAssetSettingsDefaultObject.Settings != null;

        public static string CatalogName
        {
            get
            {
                var config = AddressablesConventionSettings.instance;
                return config.VersionedCatalogs ? config.CatalogVersion.ToString() : UNVERSIONED_CATALOG;
            }
        }

        public static bool IsApplied
        {
            get
            {
                var loadPath = LoadPathVariable();
                return loadPath != null
                    && ProfileIds().All(id => Profiles.GetValueByName(id, loadPath) == ContentDelivery.REMOTE_LOAD_PATH)
                    && Settings.OverridePlayerVersion == CatalogName
                    && (!AddressablesConventionSettings.instance.RemoteOnly || Settings.BuildRemoteCatalog)
                    && CatalogApplied(loadPath, BuildPathVariable())
                    && RemoteSchemas(loadPath).All(HasRequestLimits);
            }
        }

        public static int Apply()
        {
            var loadPath = LoadPathVariable() ?? Create(AddressableAssetSettings.kRemoteLoadPath, ContentDelivery.REMOTE_LOAD_PATH);
            var buildPath = BuildPathVariable() ?? Create(AddressableAssetSettings.kRemoteBuildPath, AddressableAssetSettings.kRemoteBuildPathValue);

            var changed = 0;
            foreach (var id in ProfileIds())
            {
                if (Profiles.GetValueByName(id, loadPath) == ContentDelivery.REMOTE_LOAD_PATH)
                    continue;

                Profiles.SetValue(id, loadPath, ContentDelivery.REMOTE_LOAD_PATH);
                changed++;
            }

            if (Settings.OverridePlayerVersion != CatalogName)
            {
                Settings.OverridePlayerVersion = CatalogName;
                changed++;
            }

            if (AddressablesConventionSettings.instance.RemoteOnly && !Settings.BuildRemoteCatalog)
            {
                Settings.BuildRemoteCatalog = true;
                changed++;
            }

            if (!CatalogApplied(loadPath, buildPath))
            {
                Settings.RemoteCatalogBuildPath.SetVariableByName(Settings, buildPath);
                Settings.RemoteCatalogLoadPath.SetVariableByName(Settings, loadPath);
                changed++;
            }

            changed += ApplyRequestLimits(loadPath);

            if (changed > 0)
                Save(Settings);

            return changed;
        }

        /// <summary>
        /// Groups whose bundles would be built into the player instead of uploaded (local build or load path), except the
        /// ones listed under Groups Allowed In App.
        /// </summary>
        public static IReadOnlyList<AddressableAssetGroup> LocalGroups()
        {
            if (!HasSettings)
                return Array.Empty<AddressableAssetGroup>();

            var config = AddressablesConventionSettings.instance;
            var loadPath = LoadPathVariable();
            var buildPath = BuildPathVariable();
            return Settings.groups
                .Where(group => group != null && group.HasSchema<BundledAssetGroupSchema>() && !config.IsAllowedInApp(group.Name))
                .Where(group =>
                {
                    var schema = group.GetSchema<BundledAssetGroupSchema>();
                    return schema.LoadPath.GetName(Settings) != loadPath || schema.BuildPath.GetName(Settings) != buildPath;
                })
                .ToList();
        }

        public static int MakeGroupsRemote()
        {
            var groups = LocalGroups();
            if (groups.Count == 0)
                return 0;

            var loadPath = LoadPathVariable() ?? Create(AddressableAssetSettings.kRemoteLoadPath, ContentDelivery.REMOTE_LOAD_PATH);
            var buildPath = BuildPathVariable() ?? Create(AddressableAssetSettings.kRemoteBuildPath, AddressableAssetSettings.kRemoteBuildPathValue);

            foreach (var group in groups)
            {
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                schema.BuildPath.SetVariableByName(Settings, buildPath);
                schema.LoadPath.SetVariableByName(Settings, loadPath);
                Save(schema);
            }

            ApplyRequestLimits(loadPath);
            return groups.Count;
        }

        private static IEnumerable<BundledAssetGroupSchema> RemoteSchemas(string loadPath)
            => Settings.groups
                .Where(group => group != null && group.HasSchema<BundledAssetGroupSchema>())
                .Select(group => group.GetSchema<BundledAssetGroupSchema>())
                .Where(schema => schema.LoadPath.GetName(Settings) == loadPath);

        private static bool HasRequestLimits(BundledAssetGroupSchema schema)
            => schema.Timeout > 0 && schema.RetryCount > 0;

        private static int ApplyRequestLimits(string loadPath)
        {
            var changed = 0;
            foreach (var schema in RemoteSchemas(loadPath).Where(schema => !HasRequestLimits(schema)).ToList())
            {
                if (schema.Timeout <= 0)
                    schema.Timeout = DEFAULT_TIMEOUT_SECONDS;

                if (schema.RetryCount <= 0)
                    schema.RetryCount = DEFAULT_RETRY_COUNT;

                Save(schema);
                changed++;
            }

            return changed;
        }

        private static AddressableAssetSettings Settings => AddressableAssetSettingsDefaultObject.Settings;

        private static AddressableAssetProfileSettings Profiles => Settings.profileSettings;

        private static string LoadPathVariable()
            => Variable(AddressableAssetSettings.kRemoteLoadPath, LEGACY_REMOTE_LOAD_PATH);

        private static string BuildPathVariable()
            => Variable(AddressableAssetSettings.kRemoteBuildPath, LEGACY_REMOTE_BUILD_PATH);

        private static bool CatalogApplied(string loadPath, string buildPath)
            => !Settings.BuildRemoteCatalog || buildPath == null
            || (Settings.RemoteCatalogLoadPath.GetName(Settings) == loadPath && Settings.RemoteCatalogBuildPath.GetName(Settings) == buildPath);

        private static string Variable(params string[] candidates)
        {
            if (!HasSettings)
                return null;

            var names = Profiles.GetVariableNames();
            return candidates.FirstOrDefault(names.Contains);
        }

        private static string Create(string variable, string value)
        {
            Profiles.CreateValue(variable, value);
            return variable;
        }

        private static void Save(UnityEngine.Object asset)
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }

        private static string[] ProfileIds()
            => HasSettings ? Profiles.GetAllProfileNames().Select(Profiles.GetProfileId).ToArray() : Array.Empty<string>();
    }
}
