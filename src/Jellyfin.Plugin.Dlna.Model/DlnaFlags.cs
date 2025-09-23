#pragma warning disable CA1711, CA1028

using System;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="DlnaFlags" />.
/// </summary>
[Flags]
public enum DlnaFlags : ulong
{
    /// <summary>
    /// Defines the transfer mode.
    /// </summary>
    /// <remarks>
    /// <i>Background</i> transfer mode.
    /// For use with upload and download transfers to and from the server.
    /// The primary difference between DH_TransferMode_Interactive and DH_TransferMode_Bulk is that the latter assumes that the user is not relying on the transfer
    /// for immediately rendering the content and there are no issues with causing a buffer overflow if the receiver uses TCP flow control to reduce total throughput.
    /// </remarks>
    BackgroundTransferMode = 1 << 22,

    /// <summary>
    /// Byte based seek.
    /// </summary>
    ByteBasedSeek = 1 << 29,

    /// <summary>
    /// Connection stall.
    /// </summary>
    ConnectionStall = 1 << 21,

    /// <summary>
    /// DLNA v1.5.
    /// </summary>
    DlnaV15 = 1 << 20,

    /// <summary>
    /// Interactive transfer mode.
    /// </summary>
    /// <remarks>
    /// <i>Interactive</i> transfer mode.
    /// For best effort transfer of images and non-real-time transfers.
    /// URIs with image content usually support \ref DH_TransferMode_Bulk too.
    /// The primary difference between DH_TransferMode_Interactive and DH_TransferMode_Bulk is that the former assumes that the transfer is intended for immediate rendering.
    /// </remarks>
    InteractiveTransferMode = 1 << 23,

    /// <summary>
    /// Play container.
    /// </summary>
    PlayContainer = 1 << 28,

    /// <summary>
    /// RTSP pause.
    /// </summary>
    RtspPause = 1 << 25,

    /// <summary>
    /// S0 increase.
    /// </summary>
    S0Increase = 1 << 27,

    /// <summary>
    /// Sender paced.
    /// </summary>
    SenderPaced = 1L << 31,

    /// <summary>
    /// Sn increase.
    /// </summary>
    SnIncrease = 1 << 26,

    /// <summary>
    /// Byte based seek.
    /// </summary>
    /// <remarks>
    /// <i>Streaming</i> transfer mode.
    /// The server transmits at a throughput sufficient for real-time playback of audio or video.
    /// URIs with audio or video often support the DH_TransferMode_Interactive and DH_TransferMode_Bulk transfer modes.
    /// The most well-known exception to this general claim is for live streams.
    /// </remarks>
    StreamingTransferMode = 1 << 24,

    /// <summary>
    /// Time based seek.
    /// </summary>
    TimeBasedSeek = 1 << 30
}
