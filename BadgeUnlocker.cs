using System;
using UnityEngine;
using Zorro.Core;
using Zorro.Core.CLI;

[ConsoleClassCustomizer("BadgeUnlocker")]
public class BadgeUnlocker : MonoBehaviour
{
    [ConsoleCommand]
    public static void GiveAllBadges()
    {
        try
        {
            Singleton<AchievementManager>.Instance.DebugGetAllAchievements();
            UnityEngine.Debug.Log("[BadgeUnlocker] All badges have been granted!");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("[BadgeUnlocker] Failed to grant badges: " + ex);
        }
    }
}
