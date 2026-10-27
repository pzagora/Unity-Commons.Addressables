using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Commons.Addressables
{
    /// <summary>
    /// <see cref="AddressablesContent"/> operations shaped as <c>(IProgress&lt;float&gt;, CancellationToken) → UniTask</c>
    /// steps, ready for <c>AsyncPipeline.StepFor</c>:
    /// <code>
    /// AsyncPipeline.Start("Boot")
    ///     .Then("Catalogs", AsyncPipeline.StepFor(AddressablesSteps.UpdateCatalogs()))
    ///     .Then("Content", AsyncPipeline.StepFor(AddressablesSteps.Download(AddressableKeys.Labels.Core)));
    /// </code>
    /// </summary>
    public static class AddressablesSteps
    {
        public static Func<IProgress<float>, CancellationToken, UniTask> Initialize()
            => (progress, ct) => AddressablesContent.InitializeAsync(progress, ct);

        public static Func<IProgress<float>, CancellationToken, UniTask> UpdateCatalogs()
            => (progress, ct) => AddressablesContent.UpdateCatalogsAsync(progress, ct).AsUniTask();

        public static Func<IProgress<float>, CancellationToken, UniTask> Download(object key)
            => (progress, ct) => AddressablesContent.DownloadAsync(key, progress, ct);
    }
}
