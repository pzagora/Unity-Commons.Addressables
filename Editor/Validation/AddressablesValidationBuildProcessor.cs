using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Commons.Addressables.Editor
{
    /// <summary>Validates the Addressables setup before every player build; fails the build on errors when configured to.</summary>
    internal sealed class AddressablesValidationBuildProcessor : IPreprocessBuildWithReport
    {
        private const int CALLBACK_ORDER = -100;
        private const string FAILED_FORMAT =
            "Addressables validation found {0} error(s) - see the Console. To build anyway, turn off " +
            "'Fail Build On Errors' in Project Settings > zagora.dev > Commons Addressables.";

        public int callbackOrder => CALLBACK_ORDER;

        public void OnPreprocessBuild(BuildReport report)
        {
            var config = AddressablesConventionSettings.instance;
            if (!config.ValidateOnBuild)
                return;

            var errors = AddressablesValidator.Validate(out _);
            if (errors > 0 && config.FailBuildOnErrors)
                throw new BuildFailedException(string.Format(FAILED_FORMAT, errors));
        }
    }
}
