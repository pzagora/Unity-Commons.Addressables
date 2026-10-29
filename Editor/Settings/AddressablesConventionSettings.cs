using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Commons.Addressables.Editor
{
    /// <summary>Project-wide Commons.Addressables editor settings (Project Settings ▸ zagora.dev ▸ Commons Addressables).</summary>
    [FilePath(SETTINGS_PATH, FilePathAttribute.Location.ProjectFolder)]
    internal sealed class AddressablesConventionSettings : ScriptableSingleton<AddressablesConventionSettings>
    {
        private const string SETTINGS_PATH = "ProjectSettings/CommonsAddressablesSettings.asset";
        private const string DEFAULT_CONVENTION_FOLDER = "Addressables";
        private const string DEFAULT_KEYS_CLASS = "AddressableKeys";
        private const int FIRST_CATALOG_VERSION = 1;
        private const string SCRIPT_EXTENSION = ".cs";

        [SerializeField] private bool applyConventions = true;
        [SerializeField] private string conventionFolder = DEFAULT_CONVENTION_FOLDER;
        [SerializeField] private bool generateKeys = true;
        [SerializeField] private string keysPath = string.Empty;
        [SerializeField] private string keysNamespace = string.Empty;
        [SerializeField] private string keysClass = DEFAULT_KEYS_CLASS;
        [SerializeField] private bool validateOnBuild = true;
        [SerializeField] private bool checkDuplicateDependencies = false;
        [SerializeField] private bool failBuildOnErrors = true;
        [SerializeField] private bool remoteOnly = false;
        [SerializeField] private bool versionedCatalogs = false;
        [SerializeField] private int catalogVersion = FIRST_CATALOG_VERSION;
        [SerializeField] private List<string> groupsAllowedInApp = new();

        public bool ApplyConventions => applyConventions;
        public bool RemoteOnly => remoteOnly;
        public bool VersionedCatalogs => versionedCatalogs;
        public int CatalogVersion => Mathf.Max(FIRST_CATALOG_VERSION, catalogVersion);

        public bool IsAllowedInApp(string groupName)
            => groupsAllowedInApp.Any(allowed => string.Equals(allowed?.Trim(), groupName, StringComparison.OrdinalIgnoreCase));
        public bool ValidateOnBuild => validateOnBuild;
        public bool CheckDuplicateDependencies => checkDuplicateDependencies;
        public bool FailBuildOnErrors => failBuildOnErrors;
        public string KeysPath
        {
            get
            {
                var path = keysPath?.Trim().TrimEnd('/', '\\') ?? string.Empty;
                return path.Length == 0 || path.EndsWith(SCRIPT_EXTENSION, StringComparison.OrdinalIgnoreCase)
                    ? path
                    : $"{path}/{KeysClass}{SCRIPT_EXTENSION}";
            }
        }

        public string KeysNamespace => keysNamespace?.Trim() ?? string.Empty;
        public bool CanGenerateKeys => generateKeys && !string.IsNullOrEmpty(KeysPath);

        public string ConventionFolder => string.IsNullOrWhiteSpace(conventionFolder)
            ? DEFAULT_CONVENTION_FOLDER
            : conventionFolder.Trim();

        public string KeysClass => string.IsNullOrWhiteSpace(keysClass)
            ? DEFAULT_KEYS_CLASS
            : keysClass.Trim();

        public void SaveSettings()
            => Save(true);

        private void OnEnable()
            => hideFlags &= ~HideFlags.NotEditable;
    }
}
