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

        /// <summary>Checks for and applies remote catalog updates. Returns <c>true</c> when any catalog was updated.</summary>
        public static async UniTask<bool> UpdateCatalogsAsync(IProgress<float> progress = null, CancellationToken ct = default)
        {
            List<string> catalogs;

            var check = UnityAddressables.CheckForCatalogUpdates(false);
            try
            {
                await AsyncOperationHandleAwaiter.WaitAsync(check, null, ct);
                catalogs = check.Result;
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
