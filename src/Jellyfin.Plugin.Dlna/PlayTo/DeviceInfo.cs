using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.Model;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="DeviceInfo" />.
/// </summary>
public class DeviceInfo
{
    private string _baseUrl = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceInfo"/> class.
    /// </summary>
    public DeviceInfo()
    {
        UUID = string.Empty;
        Name = "Generic Device";
        ModelName = string.Empty;
        ModelNumber = string.Empty;
        ModelDescription = string.Empty;
        ModelUrl = string.Empty;
        Manufacturer = string.Empty;
        ManufacturerUrl = string.Empty;
        SerialNumber = string.Empty;
        PresentationUrl = string.Empty;
        Services = [];
    }

    /// <summary>
    /// Gets or sets the UUID.
    /// </summary>
    public string UUID { get; set; }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string ModelName { get; set; }

    /// <summary>
    /// Gets or sets the model number.
    /// </summary>
    public string ModelNumber { get; set; }

    /// <summary>
    /// Gets or sets the model description.
    /// </summary>
    public string ModelDescription { get; set; }

    /// <summary>
    /// Gets or sets the model URL.
    /// </summary>
    public string ModelUrl { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer.
    /// </summary>
    public string Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer URL.
    /// </summary>
    public string ManufacturerUrl { get; set; }

    /// <summary>
    /// Gets or sets the serial number.
    /// </summary>
    public string SerialNumber { get; set; }

    /// <summary>
    /// Gets or sets the presentation URL.
    /// </summary>
    public string PresentationUrl { get; set; }

    /// <summary>
    /// Gets or sets the base URL.
    /// </summary>
    public string BaseUrl
    {
        get => _baseUrl;
        set => _baseUrl = value;
    }

    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    public DeviceIcon? Icon { get; set; }

    /// <summary>
    /// Gets or sets the device services.
    /// </summary>
    public IReadOnlyList<DeviceService> Services { get; set; }

    /// <summary>
    /// Gets the <see cref="DeviceIdentification"/>.
    /// </summary>
    public DeviceIdentification ToDeviceIdentification()
    {
        return new DeviceIdentification
        {
            Manufacturer = Manufacturer,
            ModelName = ModelName,
            ModelNumber = ModelNumber,
            FriendlyName = Name,
            ManufacturerUrl = ManufacturerUrl,
            ModelUrl = ModelUrl,
            ModelDescription = ModelDescription,
            SerialNumber = SerialNumber
        };
    }
}
