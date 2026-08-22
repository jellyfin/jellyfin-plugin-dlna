using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Globalization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Dlna.Localization;

/// <summary>
/// Resolves the display names the plugin shows to DLNA clients.
/// </summary>
public class DlnaLocalization
{
    private const string DefaultCulture = "en-US";
    private const string ResourcePrefix = "Jellyfin.Plugin.Dlna.Localization.";

    private static readonly Assembly _assembly = typeof(DlnaLocalization).Assembly;

    private readonly IServerConfigurationManager _config;
    private readonly ILocalizationManager _localization;
    private readonly ILogger<DlnaLocalization> _logger;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _dictionaries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="DlnaLocalization"/> class.
    /// </summary>
    /// <param name="config">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{DlnaLocalization}"/> interface.</param>
    public DlnaLocalization(IServerConfigurationManager config, ILocalizationManager localization, ILogger<DlnaLocalization> logger)
    {
        _config = config;
        _localization = localization;
        _logger = logger;
    }

    /// <summary>
    /// Gets the localized string for a key, in the culture the server is configured to use.
    /// </summary>
    /// <param name="phrase">The key to translate.</param>
    /// <returns>The translation, or the key itself when nothing carries it.</returns>
    public string GetLocalizedString(string phrase)
    {
        var culture = _config.Configuration.UICulture;
        if (string.IsNullOrEmpty(culture))
        {
            culture = DefaultCulture;
        }

        if (TryGetShippedString(culture, phrase, out var value))
        {
            return value;
        }

        // A regional culture falls back to the plain language the plugin ships, so that "de-DE"
        // is served by "de".
        var separator = culture.IndexOf('-', StringComparison.Ordinal);
        if (separator > 0 && TryGetShippedString(culture[..separator], phrase, out value))
        {
            return value;
        }

        if (!string.Equals(culture, DefaultCulture, StringComparison.OrdinalIgnoreCase)
            && TryGetShippedString(DefaultCulture, phrase, out value))
        {
            return value;
        }

        // Everything the plugin does not ship is a string Jellyfin itself translates.
        return _localization.GetLocalizedString(phrase);
    }

    private bool TryGetShippedString(string culture, string phrase, out string value)
    {
        var dictionary = _dictionaries.GetOrAdd(culture, LoadCulture);

        return dictionary.TryGetValue(phrase, out value!);
    }

    private IReadOnlyDictionary<string, string> LoadCulture(string culture)
    {
        var resource = ResourcePrefix + culture + ".json";

        try
        {
            using var stream = _assembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                // Not every culture is translated, which the caller handles by falling back.
                return new Dictionary<string, string>();
            }

            return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing bundled translations for {Culture}", culture);

            return new Dictionary<string, string>();
        }
    }
}
