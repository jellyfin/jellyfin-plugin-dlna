using System.IO;
using System.Text;

namespace Jellyfin.Plugin.Dlna.Didl;

/// <summary>
/// Defines the <see cref="StringWriterWithEncoding" />.
/// </summary>
public class StringWriterWithEncoding : StringWriter
{
    private readonly Encoding? _encoding;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringWriterWithEncoding"/> class.
    /// </summary>
    /// <param name="encoding">The <see cref="Encoding"/>.</param>
    public StringWriterWithEncoding(Encoding encoding)
    {
        _encoding = encoding;
    }

    /// <inheritdoc />
    public override Encoding Encoding => _encoding ?? base.Encoding;
}
