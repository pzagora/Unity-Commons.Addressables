using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Commons.Addressables
{
    /// <summary>
    /// Content-delivery operations as awaitable calls with progress and cancellation: initialise Addressables, update
    /// remote catalogs, query and download dependencies. Every call releases its own handle. For bootstrap pipelines see
    /// <see cref="AddressablesSteps"/>.
    /// </summary>
    public static class AddressablesContent
    {
        private const float COMPLETE_PROGRESS = 1f;
        private const string LOG_SOURCE = "Addressables";
        private const string CHECK_FAILED_FORMAT = "Couldn't check for catalog updates; continuing with the current catalogs. {0}";
        private const string UPDATE_FAILED_FORMAT = "Couldn't update the catalogs; continuing with the current catalogs. {0}";

        public static async UniTask InitializeAsync(IProgress<float> progress = null, CancellationToken ct = default)
        {
            var handle = UnityAddressables.InitializeAsync(false);
            try
            {
                await AsyncOperationHandleAwaiter.WaitAsync(handle, progress, ct);
            }
            finally
            {
                Release(handle);
            }
        }

        /// <summary>
        /// Checks for and applies remote catalog updates. Returns <c>true</c> when any catalog was updated. A failed check
        /// or update (offline, CDN unreachable) is logged as a warning and returns <c>false</c>, so the game keeps running
        /// on the catalogs it already has instead of failing to boot; only cancellation throws.
        /// </summary>
        public static async UniTask<bool> UpdateCatalogsAsync(IProgress<float> progress = null, CancellationToken ct = default)
        {
            List<string> catalogs;

            var check = UnityAddressables.CheckForCatalogUpdates(false);
            try
            {
                await AsyncOperationHandleAwaiter.WaitAsync(check, null, ct);
                catalogs = check.Result;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CommonsLog.Warning(LOG_SOURCE, string.Format(CHECK_FAILED_FORMAT, exception.InnerException?.Message ?? exception.Message));
                progress?.Report(COMPLETE_PROGRESS);
                return false;
            }
            finally
            {
                Release(check);
            }

            if (catalogs == null || catalogs.Count == 0)
            {
                progress?.Report(COMPLETE_PROGRESS);
                return false;
            }

            var update = UnityAddressables.UpdateCatalogs(catalogs, false);
            try
            {
                await AsyncOperationHandleAwaiter.WaitAsync(update, progress, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CommonsLog.Warning(LOG_SOURCE, string.Format(UPDATE_FAILED_FORMAT, exception.InnerException?.Message ?? exception.Message));
                progress?.Report(COMPLETE_PROGRESS);
                return false;
            }
            finally
            {
                Release(update);
            }

            return true;
        }

        /// <summary>Bytes still to download for the key or label; <c>0</c> when everything is cached.</summary>
        public static async UniTask<long> GetDownloadSizeAsync(object key, CancellationToken ct = default)
        {
            var handle = UnityAddressables.GetDownloadSizeAsync(key);
            try
            {
                await AsyncOperationHandleAwaiter.WaitAsync(handle, null, ct);
                return handle.Result;
            }
            finally
            {
                Release(handle);
            }
        }

        /// <summary>Downloads (or verifies cached) bundles for the key or label, reporting download progress.</summary>
        public static async UniTask DownloadAsync(object key, IProgress<float> progress = null, CancellationToken ct = default)
        {
            var handle = UnityAddressables.DownloadDependenciesAsync(key, false);
            try
            {
                await AsyncOperationHandleAwaiter.WaitAsync(handle, progress, ct, reportDownloadProgress: true);
            }
            finally
            {
                Release(handle);
            }
        }

        private static void Release(AsyncOperationHandle handle)
        {
            if (handle.IsValid())
                UnityAddressables.Release(handle);
        }
    }
}
