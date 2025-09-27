using System.Collections.Generic;
using UnityEngine;

public static class PlayerManager
{
    private static Character _localPlayer;
    private static readonly List<Character> _otherPlayers = new List<Character>();
    private static readonly List<Character> _allPlayers = new List<Character>();
    private static float _lastUpdateTime;
    private const float CACHE_UPDATE_INTERVAL = 1.0f;

    private static void UpdatePlayerCache()
    {
        if (Time.time - _lastUpdateTime < CACHE_UPDATE_INTERVAL) return;
        _lastUpdateTime = Time.time;

        _localPlayer = null;
        _otherPlayers.Clear();
        _allPlayers.Clear();

        var allChars = Character.AllCharacters;
        if (allChars == null) return;

        foreach (var c in allChars)
        {
            if (c == null) continue;

            _allPlayers.Add(c);
            if (c.photonView != null && c.photonView.IsMine)
                _localPlayer = c;
            else
                _otherPlayers.Add(c);
        }
    }

    public static Character GetLocalPlayer()
    {
        UpdatePlayerCache();
        return _localPlayer;
    }

    public static IReadOnlyList<Character> GetOtherPlayers()
    {
        UpdatePlayerCache();
        return _otherPlayers;
    }

    public static IReadOnlyList<Character> GetAllPlayers()
    {
        UpdatePlayerCache();
        return _allPlayers;
    }

    public static List<Character> GetAlivePlayers()
    {
        UpdatePlayerCache();
        var alive = new List<Character>();
        foreach (var player in _allPlayers)
        {
            if (player != null && player.data != null && !player.data.dead)
                alive.Add(player);
        }
        return alive;
    }

    public static List<Character> GetDeadPlayers()
    {
        UpdatePlayerCache();
        var dead = new List<Character>();
        foreach (var player in _allPlayers)
        {
            if (player != null && player.data != null && player.data.dead)
                dead.Add(player);
        }
        return dead;
    }
}
