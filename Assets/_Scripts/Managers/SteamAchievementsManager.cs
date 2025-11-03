using Steamworks;
using UnityEngine;

public class SteamAchievementsManager : MonoBehaviour
{

    public static SteamAchievementsManager Instance { get; private set; }

    private int ratKillCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void CompleteAchievement(int id)
    {
        var ach = new Steamworks.Data.Achievement($"ACH_{id}");

        if (!ach.State)
        {
            ach.Trigger();
        }
    }

    public void RegisterRatDeath()
    {
        ratKillCount += 1;
        if (ratKillCount >= 5)
        {
            CompleteAchievement(22);
        }
    }

}
