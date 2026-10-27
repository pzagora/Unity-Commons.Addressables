using System.Collections.Generic;
using UnityEngine;

namespace Commons.Addressables
{
    /// <summary>
    /// Live <see cref="AssetScope"/>s for the Addressables debugger. Registration compiles out of player builds
    /// (<c>[Conditional("UNITY_EDITOR")]</c>), so only the Editor pays for tracking.
    /// </summary>
    internal static class AssetScopeRegistry
    {
        private static readonly List<AssetScope> Scopes = new();

        public static IReadOnlyList<AssetScope> All => Scopes;

        public static bool CaptureStackTraces { get; set; }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Register(AssetScope scope)
            => Scopes.Add(scope);

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Unregister(AssetScope scope)
            => Scopes.Remove(scope);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
            => Scopes.Clear();
    }
}
