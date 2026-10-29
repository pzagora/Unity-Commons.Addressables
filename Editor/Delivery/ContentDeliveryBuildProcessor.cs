using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Commons.Addressables.Editor
{
    /// <summary>
    /// Ships the active <see cref="ContentDeliverySettings"/> with the player and, when remote content loads through
    /// Content Delivery, fails the build if that content couldn't load: no settings asset, no URL for the environment
    /// this build uses, or (with Remote Only) Addressables settings that don't match Content Delivery.
    /// </summary>
    internal sealed class ContentDeliveryBuildProcessor : IPreprocessBuildWithReport
    {
        private const string SETTINGS_PAGE = "Project Settings ▸ zagora.dev ▸ Commons Addressables";
        private const string DEVELOPMENT_BUILD = "development";
        private const string RELEASE_BUILD = "release";
        private const string NO_SETTINGS =
            "Remote Addressables content loads through Content Delivery, but no Content Delivery settings asset is selected (" +
            SETTINGS_PAGE + ").";
        private const string NO_URL_FORMAT =
            "This {0} build loads remote Addressables content from the {1} environment, but its URL is empty in {2}.";
        private const string SETUP_OUTDATED =
            "Content Delivery is Remote Only, but the Addressables settings don't match it (remote load path, remote catalog " +
            "or catalog name). Use Apply Content Delivery Setup (" + SETTINGS_PAGE + ").";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            ContentDeliveryRegistry.SyncPreloadedAssets();
            if (!ContentDeliverySetup.InUse)
                return;

            if (AddressablesConventionSettings.instance.RemoteOnly && !ContentDeliverySetup.IsApplied)
                throw new BuildFailedException(SETUP_OUTDATED);

            var settings = ContentDeliveryRegistry.Active;
            if (settings == null)
                throw new BuildFailedException(NO_SETTINGS);

            var development = (report.summary.options & BuildOptions.Development) != 0;
            var environment = settings.EnvironmentFor(development);
            if (environment == ContentEnvironment.Local && string.IsNullOrWhiteSpace(settings.UrlFor(environment)))
                environment = ContentEnvironment.Development;

            if (string.IsNullOrWhiteSpace(settings.UrlFor(environment)))
                throw new BuildFailedException(string.Format(NO_URL_FORMAT,
                    development ? DEVELOPMENT_BUILD : RELEASE_BUILD, environment, AssetDatabase.GetAssetPath(settings)));
        }
    }
}
