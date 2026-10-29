using System.Collections.Generic;
using System.Linq;
using Commons.Editor;
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

        private const string REMOTE_GROUPS_HEADER = "Remote groups";
        private const string CATALOG_HEADER = "Catalog";
        private const string DELIVERY_HEADER = "Content delivery";
        private const string SETUP_HEADER = "Setup";
        private const string REMOTE_GROUPS_HELP =
            "Remote Only keeps every group on the CDN: the app downloads content when it needs it and keeps it in the " +
            "device cache until it changes there. Validation then fails builds that would put a group into the app, " +
            "except the groups listed below. Only builds are affected; in the Editor, Use Asset Database and Local " +
            "content keep working.";
        private const string DELIVERY_HELP =
            "Remote groups load from the base URL of the current environment: Live (released content), Development " +
            "(uploaded by builds) or Local (the project's ServerData folder in the Editor, or a configured URL). The Editor, " +
            "development builds and release builds each pick an environment; code can switch with ContentDelivery.Override " +
            "before Addressables initialises.";
        private const string NO_ADDRESSABLES_SETTINGS = "This project has no Addressables settings yet.";
        private const string NO_ASSET_WARNING =
            "No settings asset: remote content has no Live or Development URL. Create one and set this project's URLs.";
        private const string MISSING_URLS_FORMAT = "Set the {0} URL; remote content can't load from {0} without it.";
        private const string SETTINGS_ASSET_LABEL = "Settings Asset";
        private const string CREATE_BUTTON = "Create";
        private const float CREATE_BUTTON_WIDTH = 70f;
        private const string EDITOR_SOURCE_FORMAT = "The Editor loads remote content from {0} ({1}).";
        private const string CATALOG_HELP_FORMAT =
            "The remote catalog is catalog_{0}. Unversioned, every release overwrites it and all installed builds get the " +
            "newest content. Versioned (catalog_1, catalog_2, …), raise the version when content needs code that older " +
            "builds lack: they keep loading the last content of their version.";
        private const string SETUP_APPLIED = "The Addressables settings use Content Delivery (remote load path, remote catalog, catalog name and remote group timeouts).";
        private const string SETUP_MISSING =
            "The Addressables settings don't match Content Delivery yet: remote load path, remote catalog (paths, and on " +
            "with Remote Only), catalog name or remote group timeouts.";
        private const string APPLY_SETUP_BUTTON = "Apply Content Delivery Setup";
        private const string SETUP_APPLIED_FORMAT = "Content Delivery setup applied: {0} change(s).";
        private const string LOCAL_GROUPS_FORMAT = "Remote Only: these groups would be built into the app: {0}.";
        private const string MAKE_REMOTE_BUTTON = "Make All Groups Remote";
        private const string MADE_REMOTE_FORMAT = "{0} group(s) now build to and load from the remote paths.";
        private const string GROUP_SEPARATOR = ", ";

        private const string REMOTE_ONLY_PROPERTY = "remoteOnly";
        private const string VERSIONED_CATALOGS_PROPERTY = "versionedCatalogs";
        private const string CATALOG_VERSION_PROPERTY = "catalogVersion";
        private const string GROUPS_ALLOWED_IN_APP_PROPERTY = "groupsAllowedInApp";

        private static readonly string[] Keywords = { "Addressables", "Conventions", "Keys", "Validation", "Groups", "Content Delivery", "CDN" };

        private static readonly string[] DeliveryProperties =
            { "liveUrl", "developmentUrl", "localUrl", "editor", "developmentBuilds", "releaseBuilds" };

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

            EditorHelper.CenteredLabel(CONVENTIONS_HEADER);
            EditorGUILayout.HelpBox(string.Format(CONVENTIONS_HELP, settings.ConventionFolder), MessageType.None);
            EditorGUILayout.PropertyField(serialized.FindProperty(APPLY_CONVENTIONS_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(CONVENTION_FOLDER_PROPERTY));
            if (GUILayout.Button(APPLY_BUTTON))
                CommonsLog.Event(LOG_SOURCE, string.Format(APPLIED_FORMAT, AddressablesConventions.ApplyAll()));

            EditorHelper.CenteredLabel(KEYS_HEADER);
            EditorGUILayout.HelpBox(KEYS_HELP, MessageType.None);
            EditorGUILayout.PropertyField(serialized.FindProperty(GENERATE_KEYS_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(KEYS_PATH_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(KEYS_NAMESPACE_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(KEYS_CLASS_PROPERTY));
            if (GUILayout.Button(GENERATE_BUTTON))
                CommonsLog.Event(LOG_SOURCE, AddressableKeysGenerator.Generate() ? GENERATED_MESSAGE : UNCHANGED_MESSAGE);

            EditorHelper.CenteredLabel(VALIDATION_HEADER);
            EditorGUILayout.PropertyField(serialized.FindProperty(VALIDATE_ON_BUILD_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(CHECK_DUPLICATES_PROPERTY));
            EditorGUILayout.PropertyField(serialized.FindProperty(FAIL_BUILD_PROPERTY));
            if (GUILayout.Button(VALIDATE_BUTTON))
                AddressablesValidator.Validate(out _);

            DrawDelivery();

            EditorHelper.CenteredLabel(REMOTE_GROUPS_HEADER);
            EditorGUILayout.HelpBox(REMOTE_GROUPS_HELP, MessageType.None);
            EditorGUILayout.PropertyField(serialized.FindProperty(REMOTE_ONLY_PROPERTY));
            if (settings.RemoteOnly)
                EditorGUILayout.PropertyField(serialized.FindProperty(GROUPS_ALLOWED_IN_APP_PROPERTY), true);

            EditorHelper.CenteredLabel(CATALOG_HEADER);
            EditorGUILayout.PropertyField(serialized.FindProperty(VERSIONED_CATALOGS_PROPERTY));
            if (settings.VersionedCatalogs)
                EditorGUILayout.PropertyField(serialized.FindProperty(CATALOG_VERSION_PROPERTY));

            if (serialized.ApplyModifiedPropertiesWithoutUndo())
                settings.SaveSettings();

            EditorGUILayout.HelpBox(string.Format(CATALOG_HELP_FORMAT, ContentDeliverySetup.CatalogName), MessageType.None);

            DrawSetup(settings);
        }

        private static void DrawDelivery()
        {
            EditorHelper.CenteredLabel(DELIVERY_HEADER);
            EditorGUILayout.HelpBox(DELIVERY_HELP, MessageType.None);

            var active = ContentDeliveryRegistry.Active;
            using (new EditorGUILayout.HorizontalScope())
            {
                var selected = (ContentDeliverySettings)EditorGUILayout.ObjectField(
                    SETTINGS_ASSET_LABEL, active, typeof(ContentDeliverySettings), false);
                if (selected != active)
                    ContentDeliveryRegistry.SetActive(selected);

                if (active == null && GUILayout.Button(CREATE_BUTTON, GUILayout.Width(CREATE_BUTTON_WIDTH)))
                    ContentDeliveryRegistry.CreateAndActivate();
            }

            var settings = ContentDeliveryRegistry.Active;
            if (settings == null)
            {
                EditorGUILayout.HelpBox(NO_ASSET_WARNING, MessageType.Warning);
            }
            else
            {
                var serialized = new SerializedObject(settings);
                serialized.Update();
                foreach (var property in DeliveryProperties)
                    EditorGUILayout.PropertyField(serialized.FindProperty(property));
                serialized.ApplyModifiedProperties();

                foreach (var environment in new[] { ContentEnvironment.Live, ContentEnvironment.Development })
                {
                    if (string.IsNullOrWhiteSpace(settings.UrlFor(environment)))
                        EditorGUILayout.HelpBox(string.Format(MISSING_URLS_FORMAT, environment), MessageType.Warning);
                }
            }

            EditorGUILayout.HelpBox(string.Format(EDITOR_SOURCE_FORMAT, ContentDelivery.UrlFor(ContentDelivery.Environment), ContentDelivery.Environment), MessageType.Info);
        }

        private static void DrawSetup(AddressablesConventionSettings settings)
        {
            EditorHelper.CenteredLabel(SETUP_HEADER);
            if (!ContentDeliverySetup.HasSettings)
            {
                EditorGUILayout.HelpBox(NO_ADDRESSABLES_SETTINGS, MessageType.None);
                return;
            }

            if (ContentDeliverySetup.IsApplied)
            {
                EditorGUILayout.HelpBox(SETUP_APPLIED, MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(SETUP_MISSING, MessageType.Warning);
                if (GUILayout.Button(APPLY_SETUP_BUTTON))
                    CommonsLog.Event(LOG_SOURCE, string.Format(SETUP_APPLIED_FORMAT, ContentDeliverySetup.Apply()));
            }

            if (!settings.RemoteOnly)
                return;

            var localGroups = ContentDeliverySetup.LocalGroups();
            if (localGroups.Count == 0)
                return;

            EditorGUILayout.HelpBox(string.Format(LOCAL_GROUPS_FORMAT, string.Join(GROUP_SEPARATOR, localGroups.Select(group => group.Name))), MessageType.Warning);
            if (GUILayout.Button(MAKE_REMOTE_BUTTON))
                CommonsLog.Event(LOG_SOURCE, string.Format(MADE_REMOTE_FORMAT, ContentDeliverySetup.MakeGroupsRemote()));
        }
    }
}
