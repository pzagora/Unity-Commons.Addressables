using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Commons.Addressables.Editor
{
    internal static class AddressablesConventionSettingsProvider
    {
        private const string LOG_SOURCE = "Addressables";
        private const string SETTINGS_PATH = "Project/zagora.dev/Commons Addressables";
        private const string SETTINGS_LABEL = "Commons Addressables";
        private const string MENU_PATH = "Tools/Commons/Addressables Settings";

        private const string CONVENTIONS_HEADER = "Folder convention";
        private const string KEYS_HEADER = "Generated keys";
        private const string VALIDATION_HEADER = "Validation";

        private const string CONVENTIONS_HELP =
            "Any asset under <anything>/{0}/<Group>/... becomes addressable in group <Group>, with address " +
            "<Group>/<path below the group> (no extension) and label <Group>. The innermost '{0}' folder wins; " +
            "scripts and anything under Editor or Resources folders are skipped.";

        private const string KEYS_HELP =
            "Generates const string keys for every address and label. Put the file inside the assembly that uses the " +
            "keys (e.g. a folder covered by your game's .asmdef) - a file outside any .asmdef compiles into " +
            "Assembly-CSharp, which asmdef code can't reference. Leave the path empty to disable.";

        private const string APPLY_BUTTON = "Apply Conventions To All Assets";
        private const string GENERATE_BUTTON = "Generate Keys Now";
        private const string VALIDATE_BUTTON = "Validate Now";
        private const string APPLIED_FORMAT = "Folder convention applied: {0} entr(ies) changed.";
        private const string GENERATED_MESSAGE = "Keys generated.";
        private const string UNCHANGED_MESSAGE = "Keys are up to date (or no keys path is set).";

        private const string APPLY_CONVENTIONS_PROPERTY = "applyConventions";
        private const string CONVENTION_FOLDER_PROPERTY = "conventionFolder";
        private const string GENERATE_KEYS_PROPERTY = "generateKeys";
        private const string KEYS_PATH_PROPERTY = "keysPath";
        private const string KEYS_NAMESPACE_PROPERTY = "keysNamespace";
        private const string KEYS_CLASS_PROPERTY = "keysClass";
        private const string VALIDATE_ON_BUILD_PROPERTY = "validateOnBuild";
        private const string CHECK_DUPLICATES_PROPERTY = "checkDuplicateDependencies";
        private const string FAIL_BUILD_PROPERTY = "failBuildOnErrors";

        private static readonly string[] Keywords = { "Addressables", "Conventions", "Keys", "Validation", "Groups" };

        [SettingsProvider]
        public static SettingsProvider Create()
            => new(SETTINGS_PATH, SettingsScope.Project)
            {
                label = SETTINGS_LABEL,
                guiHandler = _ => Draw(),
                keywords = new HashSet<string>(Keywords)
            };

        [MenuItem(MENU_PATH)]
        private static void Open()
            => SettingsService.OpenProjectSettings(SETTINGS_PATH);

        private static void Draw()
        {
            var settings = AddressablesConventionSettings.instance;
            var serialized = new SerializedObject(settings);
            serialized.Update();

            EditorGUILayout.LabelField(CONVENTIONS_HEADER, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(string.Format(CONVENTIONS_HELP, settings.ConventionFolder), MessageType.None);
            EditorGUILayout.PropertyField(serialized.FindProperty(APPLY_CONVENTIONS_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(CONVENTION_FOLDER_PROPERTY));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(KEYS_HEADER, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(KEYS_HELP, MessageType.None);
            EditorGUILayout.PropertyField(serialized.FindProperty(GENERATE_KEYS_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(KEYS_PATH_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(KEYS_NAMESPACE_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(KEYS_CLASS_PROPERTY));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(VALIDATION_HEADER, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serialized.FindProperty(VALIDATE_ON_BUILD_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(CHECK_DUPLICATES_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(FAIL_BUILD_PROPERTY));

            if (serialized.ApplyModifiedPropertiesWithoutUndo())
                settings.SaveSettings();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(APPLY_BUTTON))
                    CommonsLog.Event(LOG_SOURCE, string.Format(APPLIED_FORMAT, AddressablesConventions.ApplyAll()));

                if (GUILayout.Button(GENERATE_BUTTON))
                    CommonsLog.Event(LOG_SOURCE, AddressableKeysGenerator.Generate() ? GENERATED_MESSAGE : UNCHANGED_MESSAGE);

                if (GUILayout.Button(VALIDATE_BUTTON))
                    AddressablesValidator.Validate(out _);
            }
        }
    }
}
