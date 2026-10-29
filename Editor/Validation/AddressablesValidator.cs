using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Commons.Addressables.Editor
{
    /// <summary>
    /// Checks the Addressables setup for problems that otherwise surface as bloated or broken builds: entries pointing
    /// at missing assets, assets that are both addressable and in a <c>Resources</c> folder, ambiguous addresses, assets
    /// that drifted from the folder convention, (optionally) duplicated bundle dependencies and, with Content Delivery
    /// set to Remote Only, groups that would build into the app. Logs every issue with the asset as context and returns
    /// the error count.
    /// </summary>
    internal static class AddressablesValidator
    {
        private const string LOG_SOURCE = "Addressables";
        private const string RESOURCES_SEGMENT = "/Resources/";

        private const string MISSING_ASSET_FORMAT = "Entry '{0}' in group '{1}' points to a missing asset (GUID {2}).";
        private const string IN_RESOURCES_FORMAT = "'{0}' is addressable and inside a Resources folder: it ships twice. Move it out of Resources.";
        private const string DUPLICATE_ADDRESS_FORMAT = "Address '{0}' is used by {1} entries ({2}); loading it by address is ambiguous.";
        private const string CONVENTION_DRIFT_FORMAT = "'{0}' should be in group '{1}' with address '{2}' (folder convention). Run Apply Conventions.";
        private const string NOT_ADDRESSABLE_FORMAT = "'{0}' is under a convention folder but isn't addressable. Run Apply Conventions.";
        private const string DUPLICATE_DEPENDENCY_FORMAT = "Duplicate bundle dependency: {0}";
        private const string LOCAL_GROUP_FORMAT =
            "Group '{0}' builds its bundles into the app, but Content Delivery is Remote Only. Use Make All Groups Remote " +
            "(Project Settings ▸ zagora.dev ▸ Commons Addressables).";
        private const string SETUP_OUTDATED =
            "The Addressables settings don't match Content Delivery (remote load path, remote catalog or catalog name). " +
            "Use Apply Content Delivery Setup (Project Settings ▸ zagora.dev ▸ Commons Addressables).";
        private const string SUMMARY_FORMAT = "Validation finished: {0} error(s), {1} warning(s).";
        private const string ENTRY_LIST_SEPARATOR = ", ";

        public static int Validate(out int warnings)
        {
            var errors = 0;
            warnings = 0;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return errors;

            var config = AddressablesConventionSettings.instance;
            var addresses = new Dictionary<string, List<string>>();

            foreach (var group in settings.groups)
            {
                if (group == null || group.HasSchema<PlayerDataGroupSchema>())
                    continue;

                foreach (var entry in group.entries)
                {
                    var path = AssetDatabase.GUIDToAssetPath(entry.guid);

                    if (string.IsNullOrEmpty(path) || (!File.Exists(path) && !AssetDatabase.IsValidFolder(path)))
                    {
                        CommonsLog.Error(LOG_SOURCE, string.Format(MISSING_ASSET_FORMAT, entry.address, group.Name, entry.guid));
                        errors++;
                        continue;
                    }

                    var asset = AssetDatabase.LoadMainAssetAtPath(path);

                    if (path.Contains(RESOURCES_SEGMENT))
                    {
                        CommonsLog.Error(LOG_SOURCE, string.Format(IN_RESOURCES_FORMAT, path), asset);
                        errors++;
                    }

                    if (config.ApplyConventions
                        && AddressablesConventions.TryParse(path, config.ConventionFolder, out var expectedGroup, out var expectedAddress)
                        && (group.Name != expectedGroup || entry.address != expectedAddress))
                    {
                        CommonsLog.Warning(LOG_SOURCE, string.Format(CONVENTION_DRIFT_FORMAT, path, expectedGroup, expectedAddress), asset);
                        warnings++;
                    }

                    if (!addresses.TryGetValue(entry.address, out var paths))
                        addresses[entry.address] = paths = new List<string>();

                    paths.Add(path);
                }
            }

            foreach (var address in addresses)
            {
                if (address.Value.Count <= 1)
                    continue;

                CommonsLog.Warning(LOG_SOURCE, string.Format(DUPLICATE_ADDRESS_FORMAT, address.Key, address.Value.Count, string.Join(ENTRY_LIST_SEPARATOR, address.Value)));
                warnings++;
            }

            if (config.ApplyConventions)
                warnings += ReportUnmarkedConventionAssets(settings, config.ConventionFolder);

            if (config.CheckDuplicateDependencies)
                warnings += ReportDuplicateDependencies(settings);

            if (config.RemoteOnly)
            {
                foreach (var group in ContentDeliverySetup.LocalGroups())
                {
                    CommonsLog.Error(LOG_SOURCE, string.Format(LOCAL_GROUP_FORMAT, group.Name), group);
                    errors++;
                }

                if (!ContentDeliverySetup.IsApplied)
                {
                    CommonsLog.Warning(LOG_SOURCE, SETUP_OUTDATED);
                    warnings++;
                }
            }

            var summary = string.Format(SUMMARY_FORMAT, errors, warnings);
            if (errors > 0)
                CommonsLog.Error(LOG_SOURCE, summary);
            else
                CommonsLog.Event(LOG_SOURCE, summary);

            return errors;
        }

        private static int ReportUnmarkedConventionAssets(AddressableAssetSettings settings, string folder)
        {
            var warnings = 0;

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (AssetDatabase.IsValidFolder(path) || !AddressablesConventions.TryParse(path, folder, out _, out _))
                    continue;

                if (settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)) != null)
                    continue;

                CommonsLog.Warning(LOG_SOURCE, string.Format(NOT_ADDRESSABLE_FORMAT, path), AssetDatabase.LoadMainAssetAtPath(path));
                warnings++;
            }

            return warnings;
        }

        private static int ReportDuplicateDependencies(AddressableAssetSettings settings)
        {
            var warnings = 0;
            var rule = new CheckBundleDupeDependencies();

            foreach (var result in rule.RefreshAnalysis(settings))
            {
                if (result.severity != MessageType.Warning && result.severity != MessageType.Error)
                    continue;

                CommonsLog.Warning(LOG_SOURCE, string.Format(DUPLICATE_DEPENDENCY_FORMAT, result.resultName));
                warnings++;
            }

            rule.ClearAnalysis();
            return warnings;
        }
    }
}
