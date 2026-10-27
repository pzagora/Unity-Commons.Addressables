using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Commons.Addressables.Editor
{
    /// <summary>
    /// The folder convention: any asset under <c>&lt;anything&gt;/Addressables/&lt;Group&gt;/…</c> (folder name configurable)
    /// is addressable in group <c>&lt;Group&gt;</c>, with address <c>&lt;Group&gt;/&lt;path below the group&gt;</c> (no
    /// extension) and label <c>&lt;Group&gt;</c>. The innermost convention folder wins; scripts, assemblies and anything
    /// under <c>Editor</c> or <c>Resources</c> folders are skipped.
    /// </summary>
    internal static class AddressablesConventions
    {
        private const string ASSETS_ROOT = "Assets/";
        private const char SEPARATOR = '/';
        private const string EDITOR_SEGMENT = "Editor";
        private const string RESOURCES_SEGMENT = "Resources";

        private static readonly HashSet<string> ExcludedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".asmdef", ".asmref", ".dll", ".rsp"
        };

        public static bool TryParse(string assetPath, string folderName, out string group, out string address)
        {
            group = null;
            address = null;

            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(ASSETS_ROOT, StringComparison.Ordinal))
                return false;

            var segments = assetPath.Split(SEPARATOR);
            var index = Array.LastIndexOf(segments, folderName);
            if (index <= 0 || index + 2 >= segments.Length)
                return false;

            if (segments.Contains(EDITOR_SEGMENT) || segments.Contains(RESOURCES_SEGMENT))
                return false;

            var relative = string.Join(SEPARATOR.ToString(), segments, index + 2, segments.Length - index - 2);
            var extension = Path.GetExtension(relative);
            if (ExcludedExtensions.Contains(extension))
                return false;

            group = segments[index + 1];
            address = group + SEPARATOR + relative.Substring(0, relative.Length - extension.Length);
            return true;
        }

        /// <summary>Applies the convention to every asset in the project. Returns the number of entries changed.</summary>
        public static int ApplyAll()
            => Apply(AssetDatabase.GetAllAssetPaths(), Array.Empty<string>(), Array.Empty<string>());

        /// <summary>
        /// Applies the convention to <paramref name="paths"/>, and removes convention entries for assets moved out of a
        /// convention folder (<paramref name="movedTo"/>[i] was <paramref name="movedFrom"/>[i]).
        /// </summary>
        public static int Apply(IEnumerable<string> paths, IReadOnlyList<string> movedTo, IReadOnlyList<string> movedFrom)
        {
            var config = AddressablesConventionSettings.instance;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!config.ApplyConventions || settings == null)
                return 0;

            var folder = config.ConventionFolder;
            var changed = 0;

            foreach (var path in paths)
            {
                if (AssetDatabase.IsValidFolder(path) || !TryParse(path, folder, out var group, out var address))
                    continue;

                if (ApplyEntry(settings, path, group, address))
                    changed++;
            }

            for (var i = 0; i < movedTo.Count && i < movedFrom.Count; i++)
            {
                if (!TryParse(movedFrom[i], folder, out var previousGroup, out _) || TryParse(movedTo[i], folder, out _, out _))
                    continue;

                if (RemoveConventionEntry(settings, movedTo[i], previousGroup))
                    changed++;
            }

            if (changed > 0)
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);

            return changed;
        }

        private static bool ApplyEntry(AddressableAssetSettings settings, string path, string groupName, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return false;

            var group = settings.FindGroup(groupName)
                ?? settings.CreateGroup(groupName, false, false, false, settings.DefaultGroup?.Schemas);

            var changed = false;
            var entry = settings.FindAssetEntry(guid);

            if (entry == null || entry.parentGroup != group)
            {
                entry = settings.CreateOrMoveEntry(guid, group, false, false);
                changed = true;
            }

            if (entry.address != address)
            {
                entry.SetAddress(address, false);
                changed = true;
            }

            if (!entry.labels.Contains(groupName))
            {
                settings.AddLabel(groupName, false);
                entry.SetLabel(groupName, true, true, false);
                changed = true;
            }

            return changed;
        }

        private static bool RemoveConventionEntry(AddressableAssetSettings settings, string path, string previousGroup)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = string.IsNullOrEmpty(guid) ? null : settings.FindAssetEntry(guid);
            if (entry == null || entry.parentGroup == null || entry.parentGroup.Name != previousGroup)
                return false;

            return settings.RemoveAssetEntry(guid, false);
        }
    }
}
