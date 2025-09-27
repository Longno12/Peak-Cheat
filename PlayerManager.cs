using System.Collections.Generic;
using UnityEngine;

public static class PlayerManager
{
    private static Character _localPlayer;
    private static readonly List<Character> _otherPlayers = new List<Character>();
    private static readonly List<Character> _allPlayers = new List<Character>();
    private static float _timeOfLastSearch = -1f;
    private const float CACHE_UPDATE_INTERVAL = 1.0f;

    private static void UpdatePlayerCache()
    {
        if (Time.time - _timeOfLastSearch < CACHE_UPDATE_INTERVAL) return;
        _timeOfLastSearch = Time.time;
        _otherPlayers.Clear();
        _allPlayers.Clear();
        _localPlayer = null;
        var allCharactersInSession = Character.AllCharacters;
        if (allCharactersInSession == null) return;
        _allPlayers.AddRange(allCharactersInSession);
        foreach (Character character in allCharactersInSession)
        {
            if (character == null) continue;

            if (character.photonView != null && character.photonView.IsMine)
                _localPlayer = character;
            else
                _otherPlayers.Add(character);
        }
    }

    public static Character GetLocalPlayer()
    {
        UpdatePlayerCache();
        return _localPlayer;
    }

    public static IReadOnlyList<Character> GetTargets()
    {
        UpdatePlayerCache();
        return _otherPlayers;
    }

    public static IReadOnlyList<Character> GetAllCharacters()
    {
        UpdatePlayerCache();
        return _allPlayers;
    }
}
