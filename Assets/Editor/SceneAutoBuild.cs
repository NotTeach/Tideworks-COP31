using System;
using UnityEditor;

/// <summary>
/// Sahne kuruculari icin otomatik kurulum zamanlayicisi.
/// Unity Play modundayken ya da derleme/import surerken kurmaz; Play'den cikinca veya is bitince tekrar dener.
/// Basarili denemeden sonra (veya kullanici kaydet sorusunda Cancel derse) bu oturumda bir daha sormaz.
/// </summary>
internal static class SceneAutoBuild
{
    public static void Request(string sessionKey, Action build)
    {
        if (SessionState.GetBool(sessionKey, false)) return;
        EditorApplication.delayCall += () => TryBuild(sessionKey, build);
    }

    static void TryBuild(string sessionKey, Action build)
    {
        if (SessionState.GetBool(sessionKey, false)) return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            void Retry(PlayModeStateChange state)
            {
                if (state != PlayModeStateChange.EnteredEditMode) return;
                EditorApplication.playModeStateChanged -= Retry;
                EditorApplication.delayCall += () => TryBuild(sessionKey, build);
            }
            EditorApplication.playModeStateChanged += Retry;
            return;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += () => TryBuild(sessionKey, build);
            return;
        }

        SessionState.SetBool(sessionKey, true);
        build();
    }
}
