using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Commons.Addressables.Editor
{
    /// <summary>
    /// Live view of every <see cref="AssetScope"/> and the handles it holds — who loaded what, for how long — to pinpoint
    /// leaks. Flags scopes whose owner was destroyed without disposing them. Complements Unity's Addressables Profiler
    /// module, which shows bundles and reference counts but not which code holds them.
    /// </summary>
    internal sealed class AddressableScopesWindow : EditorWindow
    {
        private const string MENU_PATH = "Window/Analysis/Addressable Scopes";
        private const string TITLE = "Addressable Scopes";
        private const string PROFILER_MENU_PATH = "Window/Analysis/Profiler";
        private const string CAPTURE_STACKS_PREF = "Commons.Addressables.CaptureStackTraces";

        private const string CAPTURE_STACKS_LABEL = "Capture Load Stacks";
        private const string SUSPECTS_ONLY_LABEL = "Suspected Leaks Only";
        private const string BY_KEY_LABEL = "By Key";
        private const string PROFILER_LABEL = "Unity Profiler";
        private const string SUMMARY_FORMAT = "{0} scope(s) · {1} handle(s) · {2} suspected leak(s)";
        private const string SCOPE_HEADER_FORMAT = "{0}  —  {1} handle(s), {2:0.0}s";
        private const string ENTRY_FORMAT = "{0}   [{1}]   {2} {3:P0}{4}   {5:0.0}s";
        private const string KEY_COUNT_FORMAT = "{0}   ×{1}";
        private const string INSTANCE_SUFFIX = "   (instance)";
        private const string OWNER_DESTROYED_LABEL = "Owner destroyed — scope never disposed (leak)";
        private const string NO_OWNER_LABEL = "No owner";
        private const string RELEASED_STATUS = "Released";
        private const string LOAD_STACK_LABEL = "Load stack";
        private const string EMPTY_PLAYING_HELP = "No live AssetScopes.";
        private const string EMPTY_EDIT_HELP = "Enter Play mode to inspect AssetScopes. Turn on 'Capture Load Stacks' first to record where each handle was loaded.";
        private const string EDIT_MODE_HELP = "Scopes still listed outside Play mode were never disposed (or belong to editor tooling).";

        private const double REPAINT_INTERVAL_SECONDS = 0.5;
        private const int GUID_LENGTH = 32;
        private const float STACK_HEIGHT = 120f;

        private readonly HashSet<AssetScope> _expandedScopes = new();
        private readonly HashSet<AssetScopeEntry> _expandedStacks = new();

        private Vector2 _scroll;
        private string _search = string.Empty;
        private bool _suspectsOnly;
        private bool _showByKey;
        private double _lastRepaint;

        [MenuItem(MENU_PATH)]
        private static void Open()
            => GetWindow<AddressableScopesWindow>(TITLE);

        [InitializeOnLoadMethod]
        private static void RestoreCaptureSetting()
            => AssetScopeRegistry.CaptureStackTraces = EditorPrefs.GetBool(CAPTURE_STACKS_PREF, false);

        private void OnEnable()
            => EditorApplication.update += RepaintPeriodically;

        private void OnDisable()
            => EditorApplication.update -= RepaintPeriodically;

        private void RepaintPeriodically()
        {
            if (EditorApplication.timeSinceStartup - _lastRepaint < REPAINT_INTERVAL_SECONDS)
                return;

            _lastRepaint = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            var scopes = AssetScopeRegistry.All.Where(Matches).ToList();
            var handles = scopes.Sum(scope => scope.HandleCount);
            var suspects = scopes.Count(scope => scope.IsOwnerDestroyed);

            EditorGUILayout.LabelField(string.Format(SUMMARY_FORMAT, scopes.Count, handles, suspects), EditorStyles.miniBoldLabel);

            if (scopes.Count == 0)
            {
                EditorGUILayout.HelpBox(EditorApplication.isPlaying ? EMPTY_PLAYING_HELP : EMPTY_EDIT_HELP, MessageType.Info);
                return;
            }

            if (!EditorApplication.isPlaying)
                EditorGUILayout.HelpBox(EDIT_MODE_HELP, MessageType.Warning);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_showByKey)
                DrawByKey(scopes);
            else
                foreach (var scope in scopes)
                    DrawScope(scope);

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var capture = GUILayout.Toggle(AssetScopeRegistry.CaptureStackTraces, CAPTURE_STACKS_LABEL, EditorStyles.toolbarButton);
                if (capture != AssetScopeRegistry.CaptureStackTraces)
                {
                    AssetScopeRegistry.CaptureStackTraces = capture;
                    EditorPrefs.SetBool(CAPTURE_STACKS_PREF, capture);
                }

                _suspectsOnly = GUILayout.Toggle(_suspectsOnly, SUSPECTS_ONLY_LABEL, EditorStyles.toolbarButton);
                _showByKey = GUILayout.Toggle(_showByKey, BY_KEY_LABEL, EditorStyles.toolbarButton);

                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(PROFILER_LABEL, EditorStyles.toolbarButton))
                    EditorApplication.ExecuteMenuItem(PROFILER_MENU_PATH);
            }
        }

        private bool Matches(AssetScope scope)
        {
            if (_suspectsOnly && !scope.IsOwnerDestroyed)
                return false;

            if (string.IsNullOrEmpty(_search))
                return true;

            return Contains(scope.Name) || scope.Entries.Any(entry => Contains(DescribeKey(entry.Key)));
        }

        private bool Contains(string text)
            => text != null && text.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;

        private void DrawScope(AssetScope scope)
        {
            var age = Time.realtimeSinceStartup - scope.CreatedAt;
            var expanded = _expandedScopes.Contains(scope);
            var header = string.Format(SCOPE_HEADER_FORMAT, scope.Name, scope.HandleCount, age);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var nowExpanded = EditorGUILayout.Foldout(expanded, header, true);
                if (nowExpanded != expanded)
                {
                    if (nowExpanded)
                        _expandedScopes.Add(scope);
                    else
                        _expandedScopes.Remove(scope);
                }

                DrawOwner(scope);

                if (!nowExpanded)
                    return;

                EditorGUI.indentLevel++;
                foreach (var entry in scope.Entries)
                    DrawEntry(entry);
                EditorGUI.indentLevel--;
            }
        }

        private static void DrawOwner(AssetScope scope)
        {
            if (scope.IsOwnerDestroyed)
            {
                EditorGUILayout.HelpBox(OWNER_DESTROYED_LABEL, MessageType.Error);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                if (scope.Owner != null)
                    EditorGUILayout.ObjectField(scope.Owner, typeof(UnityEngine.Object), true);
                else
                    EditorGUILayout.LabelField(NO_OWNER_LABEL, EditorStyles.miniLabel);
            }
        }

        private void DrawEntry(AssetScopeEntry entry)
        {
            var valid = entry.Handle.IsValid();
            var status = valid ? entry.Handle.Status.ToString() : RELEASED_STATUS;
            var progress = valid ? entry.Handle.PercentComplete : 0f;
            var age = Time.realtimeSinceStartup - entry.LoadedAt;
            var label = string.Format(ENTRY_FORMAT,
                DescribeKey(entry.Key), entry.AssetType.Name, status, progress, entry.IsInstance ? INSTANCE_SUFFIX : string.Empty, age);

            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            if (string.IsNullOrEmpty(entry.StackTrace))
                return;

            var expanded = _expandedStacks.Contains(entry);
            var nowExpanded = EditorGUILayout.Foldout(expanded, LOAD_STACK_LABEL, true);
            if (nowExpanded != expanded)
            {
                if (nowExpanded)
                    _expandedStacks.Add(entry);
                else
                    _expandedStacks.Remove(entry);
            }

            if (nowExpanded)
                EditorGUILayout.SelectableLabel(entry.StackTrace, EditorStyles.textArea, GUILayout.Height(STACK_HEIGHT));
        }

        private static void DrawByKey(IEnumerable<AssetScope> scopes)
        {
            var counts = scopes
                .SelectMany(scope => scope.Entries)
                .GroupBy(entry => DescribeKey(entry.Key))
                .Select(group => (Key: group.Key, Count: group.Count()))
                .OrderByDescending(pair => pair.Count)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal);

            foreach (var (key, count) in counts)
                EditorGUILayout.LabelField(string.Format(KEY_COUNT_FORMAT, key, count), EditorStyles.miniLabel);
        }

        private static string DescribeKey(string key)
        {
            if (key == null || key.Length != GUID_LENGTH)
                return key;

            var path = AssetDatabase.GUIDToAssetPath(key);
            return string.IsNullOrEmpty(path) ? key : path;
        }
    }
}
