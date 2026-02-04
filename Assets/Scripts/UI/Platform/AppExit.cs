using System;
using UnityEngine;

namespace UDA2.Platform
{
    public static class AppExit
    {
        /// <summary>
        /// Attempts to close the application.
        /// On Android, also finishes the Activity (and optionally removes it from Recents).
        /// Note: OS behavior may vary by device/vendor.
        /// </summary>
        public static void Quit(bool removeFromRecents = true)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            TryFinishAndroidActivity(removeFromRecents);
            Application.Quit();
            return;
#else
            Application.Quit();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void TryFinishAndroidActivity(bool removeFromRecents)
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null)
                    return;

                if (removeFromRecents)
                {
                    int sdk;
                    try
                    {
                        using var version = new AndroidJavaClass("android.os.Build$VERSION");
                        sdk = version.GetStatic<int>("SDK_INT");
                    }
                    catch
                    {
                        sdk = 0;
                    }

                    if (sdk >= 21)
                        activity.Call("finishAndRemoveTask");
                    else
                        activity.Call("finish");
                }
                else
                {
                    activity.Call<bool>("moveTaskToBack", true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AppExit] Android Activity finish failed: {e.Message}");
            }
        }
#endif
    }
}
