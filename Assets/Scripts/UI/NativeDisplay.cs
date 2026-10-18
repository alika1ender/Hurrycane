using UnityEngine;

/// <summary>Prevent a saved low-resolution fullscreen mode from stretching the title screen.</summary>
public static class NativeDisplay
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        int width = Display.main.systemWidth;
        int height = Display.main.systemHeight;
        if (width < 640 || height < 480) { width = Screen.currentResolution.width; height = Screen.currentResolution.height; }
        if (width >= 640 && height >= 480) Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
#endif
    }
}
