#pragma warning disable CA1707

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="MediaFormatProfile" />.
/// </summary>
public enum MediaFormatProfile
{
    /// <summary>
    /// MP3.
    /// </summary>
    MP3,

    /// <summary>
    /// WMA_BASE.
    /// </summary>
    WMA_BASE,

    /// <summary>
    /// WMA_FULL.
    /// </summary>
    WMA_FULL,

    /// <summary>
    /// LPCM16_44_MONO.
    /// </summary>
    LPCM16_44_MONO,

    /// <summary>
    /// LPCM16_44_STEREO.
    /// </summary>
    LPCM16_44_STEREO,

    /// <summary>
    /// LPCM16_48_MONO.
    /// </summary>
    LPCM16_48_MONO,

    /// <summary>
    /// LPCM16_48_STEREO.
    /// </summary>
    LPCM16_48_STEREO,

    /// <summary>
    /// AAC_ISO.
    /// </summary>
    AAC_ISO,

    /// <summary>
    /// AAC_ISO_320.
    /// </summary>
    AAC_ISO_320,

    /// <summary>
    /// AAC_ADTS.
    /// </summary>
    AAC_ADTS,

    /// <summary>
    /// AAC_ADTS_320.
    /// </summary>
    AAC_ADTS_320,

    /// <summary>
    /// FLAC.
    /// </summary>
    FLAC,

    /// <summary>
    /// OGG.
    /// </summary>
    OGG,

    /// <summary>
    /// JPEG_SM.
    /// </summary>
    JPEG_SM,

    /// <summary>
    /// JPEG_MED.
    /// </summary>
    JPEG_MED,

    /// <summary>
    /// JPEG_LRG.
    /// </summary>
    JPEG_LRG,

    /// <summary>
    /// JPEG_TN.
    /// </summary>
    JPEG_TN,

    /// <summary>
    /// PNG_LRG.
    /// </summary>
    PNG_LRG,

    /// <summary>
    /// PNG_TN.
    /// </summary>
    PNG_TN,

    /// <summary>
    /// GIF_LRG.
    /// </summary>
    GIF_LRG,

    /// <summary>
    /// RAW.
    /// </summary>
    RAW,

    /// <summary>
    /// MPEG1.
    /// </summary>
    MPEG1,

    /// <summary>
    /// MPEG_PS_PAL.
    /// </summary>
    MPEG_PS_PAL,

    /// <summary>
    /// MPEG_PS_NTSC.
    /// </summary>
    MPEG_PS_NTSC,

    /// <summary>
    /// MPEG_TS_SD_EU.
    /// </summary>
    MPEG_TS_SD_EU,

    /// <summary>
    /// MPEG_TS_SD_EU_ISO.
    /// </summary>
    MPEG_TS_SD_EU_ISO,

    /// <summary>
    /// MPEG_TS_SD_EU_T.
    /// </summary>
    MPEG_TS_SD_EU_T,

    /// <summary>
    /// MPEG_TS_SD_NA.
    /// </summary>
    MPEG_TS_SD_NA,

    /// <summary>
    /// MPEG_TS_SD_NA_ISO.
    /// </summary>
    MPEG_TS_SD_NA_ISO,

    /// <summary>
    /// MPEG_TS_SD_NA_T.
    /// </summary>
    MPEG_TS_SD_NA_T,

    /// <summary>
    /// MPEG_TS_SD_KO.
    /// </summary>
    MPEG_TS_SD_KO,

    /// <summary>
    /// MPEG_TS_SD_KO_ISO.
    /// </summary>
    MPEG_TS_SD_KO_ISO,

    /// <summary>
    /// MPEG_TS_SD_KO_T.
    /// </summary>
    MPEG_TS_SD_KO_T,

    /// <summary>
    /// MPEG_TS_JP_T.
    /// </summary>
    MPEG_TS_JP_T,

    /// <summary>
    /// AVI.
    /// </summary>
    AVI,

    /// <summary>
    /// MATROSKA.
    /// </summary>
    MATROSKA,

    /// <summary>
    /// FLV.
    /// </summary>
    FLV,

    /// <summary>
    /// DVR_MS.
    /// </summary>
    DVR_MS,

    /// <summary>
    /// WTV.
    /// </summary>
    WTV,

    /// <summary>
    /// OGV.
    /// </summary>
    OGV,

    /// <summary>
    /// AVC_MP4_MP_SD_AAC_MULT5.
    /// </summary>
    AVC_MP4_MP_SD_AAC_MULT5,

    /// <summary>
    /// AVC_MP4_MP_SD_MPEG1_L3.
    /// </summary>
    AVC_MP4_MP_SD_MPEG1_L3,

    /// <summary>
    /// AVC_MP4_MP_SD_AC3.
    /// </summary>
    AVC_MP4_MP_SD_AC3,

    /// <summary>
    /// AVC_MP4_MP_HD_720p_AAC.
    /// </summary>
    AVC_MP4_MP_HD_720p_AAC,

    /// <summary>
    /// AVC_MP4_MP_HD_1080i_AAC.
    /// </summary>
    AVC_MP4_MP_HD_1080i_AAC,

    /// <summary>
    /// AVC_MP4_HP_HD_AAC.
    /// </summary>
    AVC_MP4_HP_HD_AAC,

    /// <summary>
    /// AVC_TS_MP_HD_AAC_MULT5.
    /// </summary>
    AVC_TS_MP_HD_AAC_MULT5,

    /// <summary>
    /// AVC_TS_MP_HD_AAC_MULT5_T.
    /// </summary>
    AVC_TS_MP_HD_AAC_MULT5_T,

    /// <summary>
    /// AVC_TS_MP_HD_AAC_MULT5_ISO.
    /// </summary>
    AVC_TS_MP_HD_AAC_MULT5_ISO,

    /// <summary>
    /// AVC_TS_MP_HD_MPEG1_L3.
    /// </summary>
    AVC_TS_MP_HD_MPEG1_L3,

    /// <summary>
    /// AVC_TS_MP_HD_MPEG1_L3_T.
    /// </summary>
    AVC_TS_MP_HD_MPEG1_L3_T,

    /// <summary>
    /// AVC_TS_MP_HD_MPEG1_L3_ISO.
    /// </summary>
    AVC_TS_MP_HD_MPEG1_L3_ISO,

    /// <summary>
    /// AVC_TS_MP_HD_AC3.
    /// </summary>
    AVC_TS_MP_HD_AC3,

    /// <summary>
    /// AVC_TS_MP_HD_AC3_T.
    /// </summary>
    AVC_TS_MP_HD_AC3_T,

    /// <summary>
    /// AVC_TS_MP_HD_AC3_ISO.
    /// </summary>
    AVC_TS_MP_HD_AC3_ISO,

    /// <summary>
    /// AVC_TS_HP_HD_MPEG1_L2_T.
    /// </summary>
    AVC_TS_HP_HD_MPEG1_L2_T,

    /// <summary>
    /// AVC_TS_HP_HD_MPEG1_L2_ISO.
    /// </summary>
    AVC_TS_HP_HD_MPEG1_L2_ISO,

    /// <summary>
    /// AVC_TS_MP_SD_AAC_MULT5.
    /// </summary>
    AVC_TS_MP_SD_AAC_MULT5,

    /// <summary>
    /// AVC_TS_MP_SD_AAC_MULT5_T.
    /// </summary>
    AVC_TS_MP_SD_AAC_MULT5_T,

    /// <summary>
    /// AVC_TS_MP_SD_AAC_MULT5_ISO.
    /// </summary>
    AVC_TS_MP_SD_AAC_MULT5_ISO,

    /// <summary>
    /// AVC_TS_MP_SD_MPEG1_L3.
    /// </summary>
    AVC_TS_MP_SD_MPEG1_L3,

    /// <summary>
    /// AVC_TS_MP_SD_MPEG1_L3_T.
    /// </summary>
    AVC_TS_MP_SD_MPEG1_L3_T,

    /// <summary>
    /// AVC_TS_MP_SD_MPEG1_L3_ISO.
    /// </summary>
    AVC_TS_MP_SD_MPEG1_L3_ISO,

    /// <summary>
    /// AVC_TS_HP_SD_MPEG1_L2_T.
    /// </summary>
    AVC_TS_HP_SD_MPEG1_L2_T,

    /// <summary>
    /// AVC_TS_HP_SD_MPEG1_L2_ISO.
    /// </summary>
    AVC_TS_HP_SD_MPEG1_L2_ISO,

    /// <summary>
    /// AVC_TS_MP_SD_AC3.
    /// </summary>
    AVC_TS_MP_SD_AC3,

    /// <summary>
    /// AVC_TS_MP_SD_AC3_T.
    /// </summary>
    AVC_TS_MP_SD_AC3_T,

    /// <summary>
    /// AVC_TS_MP_SD_AC3_ISO.
    /// </summary>
    AVC_TS_MP_SD_AC3_ISO,

    /// <summary>
    /// AVC_TS_HD_DTS_T.
    /// </summary>
    AVC_TS_HD_DTS_T,

    /// <summary>
    /// AVC_TS_HD_DTS_ISO.
    /// </summary>
    AVC_TS_HD_DTS_ISO,

    /// <summary>
    /// WMVMED_BASE.
    /// </summary>
    WMVMED_BASE,

    /// <summary>
    /// WMVMED_FULL.
    /// </summary>
    WMVMED_FULL,

    /// <summary>
    /// WMVMED_PRO.
    /// </summary>
    WMVMED_PRO,

    /// <summary>
    /// WMVHIGH_FULL.
    /// </summary>
    WMVHIGH_FULL,

    /// <summary>
    /// WMVHIGH_PRO.
    /// </summary>
    WMVHIGH_PRO,

    /// <summary>
    /// VC1_ASF_AP_L1_WMA.
    /// </summary>
    VC1_ASF_AP_L1_WMA,

    /// <summary>
    /// VC1_ASF_AP_L2_WMA.
    /// </summary>
    VC1_ASF_AP_L2_WMA,

    /// <summary>
    /// VC1_ASF_AP_L3_WMA.
    /// </summary>
    VC1_ASF_AP_L3_WMA,

    /// <summary>
    /// VC1_TS_AP_L1_AC3_ISO.
    /// </summary>
    VC1_TS_AP_L1_AC3_ISO,

    /// <summary>
    /// VC1_TS_AP_L2_AC3_ISO.
    /// </summary>
    VC1_TS_AP_L2_AC3_ISO,

    /// <summary>
    /// VC1_TS_HD_DTS_ISO.
    /// </summary>
    VC1_TS_HD_DTS_ISO,

    /// <summary>
    /// VC1_TS_HD_DTS_T.
    /// </summary>
    VC1_TS_HD_DTS_T,

    /// <summary>
    /// MPEG4_P2_MP4_ASP_AAC.
    /// </summary>
    MPEG4_P2_MP4_ASP_AAC,

    /// <summary>
    /// MPEG4_P2_MP4_SP_L6_AAC.
    /// </summary>
    MPEG4_P2_MP4_SP_L6_AAC,

    /// <summary>
    /// MPEG4_P2_MP4_NDSD.
    /// </summary>
    MPEG4_P2_MP4_NDSD,

    /// <summary>
    /// MPEG4_P2_TS_ASP_AAC.
    /// </summary>
    MPEG4_P2_TS_ASP_AAC,

    /// <summary>
    /// MPEG4_P2_TS_ASP_AAC_T.
    /// </summary>
    MPEG4_P2_TS_ASP_AAC_T,

    /// <summary>
    /// MPEG4_P2_TS_ASP_AAC_ISO.
    /// </summary>
    MPEG4_P2_TS_ASP_AAC_ISO,

    /// <summary>
    /// MPEG4_P2_TS_ASP_MPEG1_L3.
    /// </summary>
    MPEG4_P2_TS_ASP_MPEG1_L3,

    /// <summary>
    /// MPEG4_P2_TS_ASP_MPEG1_L3_T.
    /// </summary>
    MPEG4_P2_TS_ASP_MPEG1_L3_T,

    /// <summary>
    /// MPEG4_P2_TS_ASP_MPEG1_L3_ISO.
    /// </summary>
    MPEG4_P2_TS_ASP_MPEG1_L3_ISO,

    /// <summary>
    /// MPEG4_P2_TS_ASP_MPEG2_L2.
    /// </summary>
    MPEG4_P2_TS_ASP_MPEG2_L2,

    /// <summary>
    /// MPEG4_P2_TS_ASP_MPEG2_L2_T.
    /// </summary>
    MPEG4_P2_TS_ASP_MPEG2_L2_T,

    /// <summary>
    /// MPEG4_P2_TS_ASP_MPEG2_L2_ISO.
    /// </summary>
    MPEG4_P2_TS_ASP_MPEG2_L2_ISO,

    /// <summary>
    /// MPEG4_P2_TS_ASP_AC3.
    /// </summary>
    MPEG4_P2_TS_ASP_AC3,

    /// <summary>
    /// MPEG4_P2_TS_ASP_AC3_T.
    /// </summary>
    MPEG4_P2_TS_ASP_AC3_T,

    /// <summary>
    /// MPEG4_P2_TS_ASP_AC3_ISO.
    /// </summary>
    MPEG4_P2_TS_ASP_AC3_ISO,

    /// <summary>
    /// AVC_TS_HD_50_LPCM_T.
    /// </summary>
    AVC_TS_HD_50_LPCM_T,

    /// <summary>
    /// AVC_MP4_LPCM.
    /// </summary>
    AVC_MP4_LPCM,

    /// <summary>
    /// MPEG4_P2_3GPP_SP_L0B_AAC.
    /// </summary>
    MPEG4_P2_3GPP_SP_L0B_AAC,

    /// <summary>
    /// MPEG4_P2_3GPP_SP_L0B_AMR.
    /// </summary>
    MPEG4_P2_3GPP_SP_L0B_AMR,

    /// <summary>
    /// AVC_3GPP_BL_QCIF15_AAC.
    /// </summary>
    AVC_3GPP_BL_QCIF15_AAC,

    /// <summary>
    /// MPEG4_H263_3GPP_P0_L10_AMR.
    /// </summary>
    MPEG4_H263_3GPP_P0_L10_AMR,

    /// <summary>
    /// MPEG4_H263_MP4_P0_L10_AAC.
    /// </summary>
    MPEG4_H263_MP4_P0_L10_AAC
}
