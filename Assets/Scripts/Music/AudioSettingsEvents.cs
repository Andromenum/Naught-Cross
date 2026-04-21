using System;

public static class AudioSettingsEvents
{
    public static event Action<bool> MusicEnabledChanged;
    public static event Action<bool> SfxEnabledChanged;
    public static event Action<float> MusicVolumeChanged;
    public static event Action<float> SfxVolumeChanged;

    public static void RaiseMusicEnabledChanged(bool value)
    {
        MusicEnabledChanged?.Invoke(value);
    }

    public static void RaiseSfxEnabledChanged(bool value)
    {
        SfxEnabledChanged?.Invoke(value);
    }

    public static void RaiseMusicVolumeChanged(float value)
    {
        MusicVolumeChanged?.Invoke(value);
    }

    public static void RaiseSfxVolumeChanged(float value)
    {
        SfxVolumeChanged?.Invoke(value);
    }
}