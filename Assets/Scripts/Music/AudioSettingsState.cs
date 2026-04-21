using UnityEngine;

public static class AudioSettingsState
{
    private const string MusicEnabledKey = "MusicEnabled";
    private const string SfxEnabledKey = "SfxEnabled";
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";

    public static bool GetMusicEnabled()
    {
        return PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
    }

    public static void SetMusicEnabled(bool value)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool GetSfxEnabled()
    {
        return PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
    }

    public static void SetSfxEnabled(bool value)
    {
        PlayerPrefs.SetInt(SfxEnabledKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
    }

    public static void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    public static float GetSfxVolume()
    {
        return PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
    }

    public static void SetSfxVolume(float value)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }
}