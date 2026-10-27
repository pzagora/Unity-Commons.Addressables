using System.Collections.Generic;
using UnityEditor;

namespace Commons.Addressables.Editor
{
    /// <summary>
    /// Keeps the folder convention applied as assets are imported or moved. Work is batched and deferred to
    /// <see cref="EditorApplication.delayCall"/>, so Addressables groups are never created mid-import.
    /// </summary>
    internal sealed class AddressablesConventionPostprocessor : AssetPostprocessor
    {
        private static readonly List<string> PendingPaths = new();
        private static readonly List<string> PendingMovedTo = new();
        private static readonly List<string> PendingMovedFrom = new();

        private static bool _scheduled;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!AddressablesConventionSettings.instance.ApplyConventions)
                return;

            PendingPaths.AddRange(importedAssets);
            PendingPaths.AddRange(movedAssets);
            PendingMovedTo.AddRange(movedAssets);
            PendingMovedFrom.AddRange(movedFromAssetPaths);

            if (_scheduled)
                return;

            _scheduled = true;
            EditorApplication.delayCall += Flush;
        }

        private static void Flush()
        {
            _scheduled = false;

            AddressablesConventions.Apply(PendingPaths, PendingMovedTo, PendingMovedFrom);

            PendingPaths.Clear();
            PendingMovedTo.Clear();
            PendingMovedFrom.Clear();
        }
    }
}
