namespace Commons.Addressables
{
    /// <summary>Where remote Addressables content comes from; each has its own base URL in <see cref="ContentDeliverySettings"/>.</summary>
    public enum ContentEnvironment
    {
        /// <summary>Released content. Only a release (development → live) publishes it.</summary>
        Live,

        /// <summary>Content uploaded by builds.</summary>
        Development,

        /// <summary>A local copy: the project's ServerData folder in the Editor, or a configured URL such as a LAN server.</summary>
        Local
    }
}
