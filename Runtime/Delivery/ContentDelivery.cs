using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Scripting;

namespace Commons.Addressables
{
    /// <summary>
    /// Resolves the base URL of remote Addressables content at runtime, so one content build can be loaded from any
    /// <see cref="ContentEnvironment"/>. Remote groups need the profile's <c>Remote.LoadPath</c> set to
    /// <see cref="REMOTE_LOAD_PATH"/> (Project Settings ▸ zagora.dev ▸ Commons Addressables can set it). Call
    /// <see cref="Override"/> before Addressables initialises to switch environments, e.g. from a debug menu.
    /// </summary>
    [Preserve]
    public static class ContentDelivery
    {
        public const string REMOTE_LOAD_PATH = "{Commons.Addressables.ContentDelivery.RemoteUrl}/[BuildTarget]";

        private const string LOG_SOURCE = "Content Delivery";
        private const string LOCAL_FOLDER = "ServerData";
        private const string NO_LOCAL_URL = "No local content URL is set for this build; loading {0} content instead.";
        private const string MISSING_URL =
            "No {0} content URL is set, so remote Addressables content can't load. Set it in the Content Delivery " +
            "settings asset (Project Settings ▸ zagora.dev ▸ Commons Addressables).";

        private static readonly HashSet<ContentEnvironment> Reported = new();

        private static ContentEnvironment? _override;
        private static ContentDeliverySettings _defaults;
        private static string _remoteUrl;

        public static ContentEnvironment Environment => _override ?? Settings.Configured;

        /// <summary>
        /// Base URL of the current <see cref="Environment"/>, without a trailing slash. Evaluated by Addressables, possibly
        /// off the main thread, so it's resolved on the main thread before the first scene loads and on <see cref="Override"/>.
        /// </summary>
        [Preserve]
        public static string RemoteUrl => _remoteUrl ??= Resolve();

        /// <summary>
        /// Forces an environment (or clears the override with <c>null</c>). Call on the main thread before Addressables
        /// initialises: catalogs already loaded keep the URL they were loaded with.
        /// </summary>
        public static void Override(ContentEnvironment? environment)
        {
            _override = environment;
            _remoteUrl = Resolve();
        }

        public static string UrlFor(ContentEnvironment environment)
        {
            var url = Settings.UrlFor(environment);
            if (environment == ContentEnvironment.Local && string.IsNullOrWhiteSpace(url))
                url = LocalFallback();

            return url?.Trim().TrimEnd('/') ?? string.Empty;
        }

        private static string Resolve()
        {
            var environment = Environment;
            var url = UrlFor(environment);
            if (url.Length == 0 && Reported.Add(environment))
                CommonsLog.Error(LOG_SOURCE, string.Format(MISSING_URL, environment));

            return url;
        }

        private static ContentDeliverySettings Settings => ContentDeliverySettings.Active != null
            ? ContentDeliverySettings.Active
            : _defaults ??= ScriptableObject.CreateInstance<ContentDeliverySettings>();

        private static string LocalFallback()
        {
            if (Application.isEditor)
                return new Uri(Path.GetFullPath(Path.Combine(Application.dataPath, "..", LOCAL_FOLDER))).AbsoluteUri;

            if (Reported.Add(ContentEnvironment.Local))
                CommonsLog.Warning(LOG_SOURCE, string.Format(NO_LOCAL_URL, ContentEnvironment.Development));

            return Settings.UrlFor(ContentEnvironment.Development);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            _override = null;
            _remoteUrl = null;
            Reported.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResolveOnMainThread()
            => _remoteUrl ??= Resolve();
    }
}
