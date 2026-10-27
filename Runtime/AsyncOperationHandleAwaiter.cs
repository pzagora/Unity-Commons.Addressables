using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Commons.Addressables
{
    internal static class AsyncOperationHandleAwaiter
    {
        private const string FAILED_FORMAT = "Addressables operation failed: {0}";
        private const float COMPLETE_PROGRESS = 1f;

        /// <summary>
        /// Polls the handle to completion, reporting progress and honouring cancellation. <paramref name="isAborted"/> is
        /// checked before the handle is touched each frame, so a handle released mid-wait (e.g. by a disposed scope) is
        /// never read. Throws when the operation fails; never releases the handle — that stays with the caller.
        /// </summary>
        public static async UniTask WaitAsync(
            AsyncOperationHandle handle,
            IProgress<float> progress,
            CancellationToken ct,
            Func<bool> isAborted = null,
            bool reportDownloadProgress = false)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (isAborted != null && isAborted())
                    throw new OperationCanceledException();

                if (handle.IsDone)
                    break;

                progress?.Report(reportDownloadProgress ? handle.GetDownloadStatus().Percent : handle.PercentComplete);
                await UniTask.Yield();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new InvalidOperationException(string.Format(FAILED_FORMAT, handle.DebugName), handle.OperationException);

            progress?.Report(COMPLETE_PROGRESS);
        }
    }
}
