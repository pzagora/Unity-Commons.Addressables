using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using UnityAddressables = UnityEngine.AddressableAssets.Addressables;

namespace Commons.Addressables
{
    /// <summary>
    /// Owns the Addressables handles of one lifetime — a window, a view model, a scene, a service — and releases every
    /// one of them on <see cref="Dispose"/>, so loads can't leak or be released twice. Loads are awaitable (UniTask) with
    /// progress and cancellation; a load that is cancelled, fails, or outlives the scope releases its own handle.
    /// Give it an <c>owner</c> so the Addressable Scopes debugger can flag a scope whose owner was destroyed without
    /// disposing it.
    /// </summary>
    public sealed class AssetScope : IDisposable
    {
        private const string DEFAULT_NAME = "AssetScope";
        private const string DISPOSED_FORMAT = "AssetScope '{0}' is disposed.";
        private const int STACK_FRAMES_TO_SKIP = 1;

        private readonly List<AssetScopeEntry> _entries = new();
        private readonly bool _hasOwner;

        public AssetScope(string name = null, Object owner = null)
        {
            _hasOwner = owner != null;
            Owner = owner;
            Name = !string.IsNullOrEmpty(name) ? name : _hasOwner ? owner.name : DEFAULT_NAME;
            CreatedAt = Time.realtimeSinceStartup;

            AssetScopeRegistry.Register(this);
        }

        public string Name { get; }
        public Object Owner { get; }
        public float CreatedAt { get; }
        public bool IsDisposed { get; private set; }
        public int HandleCount => _entries.Count;

        /// <summary><c>true</c> when the scope was given an owner that has since been destroyed.</summary>
        public bool IsOwnerDestroyed => _hasOwner && Owner == null;

        internal IReadOnlyList<AssetScopeEntry> Entries => _entries;

        public async UniTask<T> LoadAsync<T>(object key, IProgress<float> progress = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var handle = UnityAddressables.LoadAssetAsync<T>(key);
            await WaitAsync(Track(key, typeof(T), handle, false), progress, ct);

            return handle.Result;
        }

        public async UniTask<IList<T>> LoadAllAsync<T>(object key, IProgress<float> progress = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var handle = UnityAddressables.LoadAssetsAsync(key, (Action<T>)null);
            await WaitAsync(Track(key, typeof(T), handle, false), progress, ct);

            return handle.Result;
        }

        public async UniTask<GameObject> InstantiateAsync(
            object key,
            Transform parent = null,
            bool worldPositionStays = false,
            IProgress<float> progress = null,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var handle = UnityAddressables.InstantiateAsync(key, parent, worldPositionStays);
            await WaitAsync(Track(key, typeof(GameObject), handle, true), progress, ct);

            return handle.Result;
        }

        /// <summary>Releases one asset or instance this scope loaded, ahead of the scope's own disposal.</summary>
        public bool Release(Object asset)
        {
            if (IsDisposed || asset == null)
                return false;

            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (!entry.Handle.IsValid() || !ReferenceEquals(entry.Handle.Result, asset))
                    continue;

                _entries.RemoveAt(i);
                ReleaseHandle(entry);
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            for (var i = _entries.Count - 1; i >= 0; i--)
                ReleaseHandle(_entries[i]);

            _entries.Clear();
            AssetScopeRegistry.Unregister(this);
        }

        private AssetScopeEntry Track(object key, Type assetType, AsyncOperationHandle handle, bool isInstance)
        {
            var stackTrace = AssetScopeRegistry.CaptureStackTraces
                ? new System.Diagnostics.StackTrace(STACK_FRAMES_TO_SKIP, true).ToString()
                : null;

            var entry = new AssetScopeEntry(key, assetType, handle, isInstance, stackTrace);
            _entries.Add(entry);
            return entry;
        }

        private async UniTask WaitAsync(AssetScopeEntry entry, IProgress<float> progress, CancellationToken ct)
        {
            try
            {
                await AsyncOperationHandleAwaiter.WaitAsync(entry.Handle, progress, ct, () => IsDisposed);
            }
            catch
            {
                Untrack(entry);
                throw;
            }
        }

        private void Untrack(AssetScopeEntry entry)
        {
            if (_entries.Remove(entry))
                ReleaseHandle(entry);
        }

        private static void ReleaseHandle(AssetScopeEntry entry)
        {
            if (!entry.Handle.IsValid())
                return;

            if (entry.IsInstance)
                UnityAddressables.ReleaseInstance(entry.Handle);
            else
                UnityAddressables.Release(entry.Handle);
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(Name, string.Format(DISPOSED_FORMAT, Name));
        }
    }
}
