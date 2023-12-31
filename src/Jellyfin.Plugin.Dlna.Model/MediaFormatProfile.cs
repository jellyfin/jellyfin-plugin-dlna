#pragma warning disable CS1591, CA1707

namespace Jellyfin.Plugin.Dlna.Model;

public enum MediaFormatProfile
{
    MP3,
    WMA_BASE,
    WMA_FULL,
    LPCM16_44_MONO,
    LPCM16_44_STEREO,
    LPCM16_48_MONO,
    LPCM16_48_STEREO,
    AAC_ISO,
    AAC_ISO_320,
    AAC_ADTS,
    AAC_ADTS_320,
    FLAC,
    OGG,

    JPEG_SM,
    JPEG_MED,
    JPEG_LRG,
    JPEG_TN,
    PNG_LRG,
    PNG_TN,
    GIF_LRG,
    RAW,

    MPEG1,
    MPEG_PS_PAL,
    MPEG_PS_NTSC,
    MPEG_TS_SD_EU,
    MPEG_TS_SD_EU_ISO,
    MPEG_TS_SD_EU_T,
    MPEG_TS_SD_NA,
    MPEG_TS_SD_NA_ISO,
    MPEG_TS_SD_NA_T,
    MPEG_TS_SD_KO,
    MPEG_TS_SD_KO_ISO,
    MPEG_TS_SD_KO_T,
    MPEG_TS_JP_T,
    AVI,
    MATROSKA,
    FLV,
    DVR_MS,
    WTV,
    OGV,
    AVC_MP4_MP_SD_AAC_MULT5,
    AVC_MP4_MP_SD_MPEG1_L3,
    AVC_MP4_MP_SD_AC3,
    AVC_MP4_MP_HD_720p_AAC,
    AVC_MP4_MP_HD_1080i_AAC,
    AVC_MP4_HP_HD_AAC,
    AVC_TS_MP_HD_AAC_MULT5,
    AVC_TS_MP_HD_AAC_MULT5_T,
    AVC_TS_MP_HD_AAC_MULT5_ISO,
    AVC_TS_MP_HD_MPEG1_L3,
    AVC_TS_MP_HD_MPEG1_L3_T,
    AVC_TS_MP_HD_MPEG1_L3_ISO,
    AVC_TS_MP_HD_AC3,
    AVC_TS_MP_HD_AC3_T,
    AVC_TS_MP_HD_AC3_ISO,
    AVC_TS_HP_HD_MPEG1_L2_T,
    AVC_TS_HP_HD_MPEG1_L2_ISO,
    AVC_TS_MP_SD_AAC_MULT5,
    AVC_TS_MP_SD_AAC_MULT5_T,
    AVC_TS_MP_SD_AAC_MULT5_ISO,
    AVC_TS_MP_SD_MPEG1_L3,
    AVC_TS_MP_SD_MPEG1_L3_T,
    AVC_TS_MP_SD_MPEG1_L3_ISO,
    AVC_TS_HP_SD_MPEG1_L2_T,
    AVC_TS_HP_SD_MPEG1_L2_ISO,
    AVC_TS_MP_SD_AC3,
    AVC_TS_MP_SD_AC3_T,
    AVC_TS_MP_SD_AC3_ISO,
    AVC_TS_HD_DTS_T,
    AVC_TS_HD_DTS_ISO,
    WMVMED_BASE,
    WMVMED_FULL,
    WMVMED_PRO,
    WMVHIGH_FULL,
    WMVHIGH_PRO,
    VC1_ASF_AP_L1_WMA,
    VC1_ASF_AP_L2_WMA,
    VC1_ASF_AP_L3_WMA,
    VC1_TS_AP_L1_AC3_ISO,
    VC1_TS_AP_L2_AC3_ISO,
    VC1_TS_HD_DTS_ISO,
    VC1_TS_HD_DTS_T,
    MPEG4_P2_MP4_ASP_AAC,
    MPEG4_P2_MP4_SP_L6_AAC,
    MPEG4_P2_MP4_NDSD,
    MPEG4_P2_TS_ASP_AAC,
    MPEG4_P2_TS_ASP_AAC_T,
    MPEG4_P2_TS_ASP_AAC_ISO,
    MPEG4_P2_TS_ASP_MPEG1_L3,
    MPEG4_P2_TS_ASP_MPEG1_L3_T,
    MPEG4_P2_TS_ASP_MPEG1_L3_ISO,
    MPEG4_P2_TS_ASP_MPEG2_L2,
    MPEG4_P2_TS_ASP_MPEG2_L2_T,
    MPEG4_P2_TS_ASP_MPEG2_L2_ISO,
    MPEG4_P2_TS_ASP_AC3,
    MPEG4_P2_TS_ASP_AC3_T,
    MPEG4_P2_TS_ASP_AC3_ISO,
    AVC_TS_HD_50_LPCM_T,
    AVC_MP4_LPCM,
    MPEG4_P2_3GPP_SP_L0B_AAC,
    MPEG4_P2_3GPP_SP_L0B_AMR,
    AVC_3GPP_BL_QCIF15_AAC,
    MPEG4_H263_3GPP_P0_L10_AMR,
    MPEG4_H263_MP4_P0_L10_AAC
}
