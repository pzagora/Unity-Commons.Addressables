using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Commons.Addressables
{
    internal sealed class AssetScopeEntry
    {
        private const string NULL_KEY = "<null>";

        public AssetScopeEntry(object key, Type assetType, AsyncOperationHandle handle, bool isInstance, string stackTrace)
        {
            Key = DescribeKey(key);
            AssetType = assetType;
            Handle = handle;
            IsInstance = isInstance;
            StackTrace = stackTrace;
            LoadedAt = Time.realtimeSinceStartup;
        }

        public string Key { get; }
        public Type AssetType { get; }
        public AsyncOperationHandle Handle { get; }
        public bool IsInstance { get; }
        public string StackTrace { get; }
        public float LoadedAt { get; }

        private static string DescribeKey(object key)
            => key switch
            {
                null => NULL_KEY,
                string text => text,
                IKeyEvaluator reference => reference.RuntimeKey?.ToString() ?? NULL_KEY,
                IResourceLocation location => location.PrimaryKey,
                _ => key.ToString()
            };
    }
}
