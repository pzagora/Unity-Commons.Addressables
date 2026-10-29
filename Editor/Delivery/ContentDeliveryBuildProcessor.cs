using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Commons.Addressables.Editor
{
    internal sealed class ContentDeliveryBuildProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
            => ContentDeliveryRegistry.SyncPreloadedAssets();
    }
}
