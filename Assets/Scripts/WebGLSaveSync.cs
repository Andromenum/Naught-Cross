using System.Runtime.InteropServices;

public static class WebGLSaveSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SyncFilesToIndexedDB();
#endif

    public static void Flush()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SyncFilesToIndexedDB();
#endif
    }
}