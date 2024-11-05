using System.Xml.Linq;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="UPnpNamespaces" />.
/// </summary>
public static class UPnpNamespaces
{
    /// <summary>
    /// Gets the Dc namespace.
    /// </summary>
    public static XNamespace Dc { get; } = "http://purl.org/dc/elements/1.1/";

    /// <summary>
    /// Gets the Ns namespace.
    /// </summary>
    public static XNamespace Ns { get; } = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";

    /// <summary>
    /// Gets the service namespace.
    /// </summary>
    public static XNamespace Svc { get; } = "urn:schemas-upnp-org:service-1-0";

    /// <summary>
    /// Gets the device namespace.
    /// </summary>
    public static XNamespace Ud { get; } = "urn:schemas-upnp-org:device-1-0";

    /// <summary>
    /// Gets the Upnp namespace.
    /// </summary>
    public static XNamespace UPnp { get; } = "urn:schemas-upnp-org:metadata-1-0/upnp/";

    /// <summary>
    /// Gets the RenderingControl namespace.
    /// </summary>
    public static XNamespace RenderingControl { get; } = "urn:schemas-upnp-org:service:RenderingControl:1";

    /// <summary>
    /// Gets the AvTransport namespace.
    /// </summary>
    public static XNamespace AvTransport { get; } = "urn:schemas-upnp-org:service:AVTransport:1";

    /// <summary>
    /// Gets the ContentDirectory namespace.
    /// </summary>
    public static XNamespace ContentDirectory { get; } = "urn:schemas-upnp-org:service:ContentDirectory:1";

    /// <summary>
    /// Gets the container element name.
    /// </summary>
    public static XName Containers { get; } = Ns + "container";

    /// <summary>
    /// Gets the item element name.
    /// </summary>
    public static XName Items { get; } = Ns + "item";

    /// <summary>
    /// Gets the title element name.
    /// </summary>
    public static XName Title { get; } = Dc + "title";

    /// <summary>
    /// Gets the creator element name.
    /// </summary>
    public static XName Creator { get; } = Dc + "creator";

    /// <summary>
    /// Gets the artist element name.
    /// </summary>
    public static XName Artist { get; } = UPnp + "artist";

    /// <summary>
    /// Gets the id element name.
    /// </summary>
    public static XName Id { get; } = "id";

    /// <summary>
    /// Gets the parent id element name.
    /// </summary>
    public static XName ParentId { get; } = "parentID";

    /// <summary>
    /// Gets the class element name.
    /// </summary>
    public static XName Class { get; } = UPnp + "class";

    /// <summary>
    /// Gets the artwork element name.
    /// </summary>
    public static XName Artwork { get; } = UPnp + "albumArtURI";

    /// <summary>
    /// Gets the description element name.
    /// </summary>
    public static XName Description { get; } = Dc + "description";

    /// <summary>
    /// Gets the long description element name.
    /// </summary>
    public static XName LongDescription { get; } = UPnp + "longDescription";

    /// <summary>
    /// Gets the album element name.
    /// </summary>
    public static XName Album { get; } = UPnp + "album";

    /// <summary>
    /// Gets the author element name.
    /// </summary>
    public static XName Author { get; } = UPnp + "author";

    /// <summary>
    /// Gets the director element name.
    /// </summary>
    public static XName Director { get; } = UPnp + "director";

    /// <summary>
    /// Gets the playback count element name.
    /// </summary>
    public static XName PlayCount { get; } = UPnp + "playbackCount";

    /// <summary>
    /// Gets the track number element name.
    /// </summary>
    public static XName Tracknumber { get; } = UPnp + "originalTrackNumber";

    /// <summary>
    /// Gets the resolution element name.
    /// </summary>
    public static XName Res { get; } = Ns + "res";

    /// <summary>
    /// Gets the duration element name.
    /// </summary>
    public static XName Duration { get; } = "duration";

    /// <summary>
    /// Gets the protocol info element name.
    /// </summary>
    public static XName ProtocolInfo { get; } = "protocolInfo";

    /// <summary>
    /// Gets the service state table element name.
    /// </summary>
    public static XName ServiceStateTable { get; } = Svc + "serviceStateTable";

    /// <summary>
    /// Gets the state variable element name.
    /// </summary>
    public static XName StateVariable { get; } = Svc + "stateVariable";
}
