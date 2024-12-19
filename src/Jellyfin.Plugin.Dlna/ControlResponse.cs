using System.Collections.Generic;

namespace Jellyfin.Plugin.Dlna;

/// <summary>
/// Defines the <see cref="ControlResponse" />.
/// </summary>
public class ControlResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControlRequest"/> class.
    /// </summary>
    /// <param name="xml">The XML.</param>
    /// <param name="isSuccessful">A value indicating wether the triggering action is successful or not.</param>
    public ControlResponse(string xml, bool isSuccessful)
    {
        Headers = new Dictionary<string, string>();
        Xml = xml;
        IsSuccessful = isSuccessful;
    }

    /// <summary>
    /// Gets the headers dictionary.
    /// </summary>
    public IDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets or sets the XML.
    /// </summary>
    public string Xml { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the triggering action is successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Xml;
    }
}
