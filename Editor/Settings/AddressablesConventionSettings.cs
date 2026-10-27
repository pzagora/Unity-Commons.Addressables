using UnityEditor;
using UnityEngine;

namespace Commons.Addressables.Editor
{
    /// <summary>Project-wide Commons.Addressables editor settings (Project Settings ▸ Commons Addressables).</summary>
    [FilePath(SETTINGS_PATH, FilePathAttribute.Location.ProjectFolder)]
    internal sealed class AddressablesConventionSettings : ScriptableSingleton<AddressablesConventionSettings>
    {
        private const string SETTINGS_PATH = "ProjectSettings/CommonsAddressablesSettings.asset";
        private const string DEFAULT_CONVENTION_FOLDER = "Addressables";
        private const string DEFAULT_KEYS_CLASS = "AddressableKeys";

        [SerializeField] private bool applyConventions = true;
        [SerializeField] private string conventionFolder = DEFAULT_CONVENTION_FOLDER;
        [SerializeField] private bool generateKeys = true;
        [SerializeField] private string keysPath = string.Empty;
        [SerializeField] private string keysNamespace = string.Empty;
        [SerializeField] private string keysClass = DEFAULT_KEYS_CLASS;
        [SerializeField] private bool validateOnBuild = true;
        [SerializeField] private bool checkDuplicateDependencies = false;
        [SerializeField] private bool failBuildOnErrors = true;

        public bool ApplyConventions => applyConventions;
        public bool ValidateOnBuild => validateOnBuild;
        public bool CheckDuplicateDependencies => checkDuplicateDependencies;
        public bool FailBuildOnErrors => failBuildOnErrors;
        public string KeysPath => keysPath?.Trim() ?? string.Empty;
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
