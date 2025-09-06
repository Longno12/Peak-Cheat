using System.Collections.Generic;
using UnityEngine;

public static class PlayerManager
{
    private static Character _localPlayer;
    private static List<Character> _otherPlayers;
    private static List<Character> _allPlayers;
    private static float _timeOfLastSearch = -1f;
    private const float CACHE_UPDATE_INTERVAL = 1.0f;

    private static void UpdatePlayerCache()
    {
        if (Time.time - _timeOfLastSearch < CACHE_UPDATE_INTERVAL) return;
        _timeOfLastSearch = Time.time;
        if (_otherPlayers == null) _otherPlayers = new List<Character>();
        if (_allPlayers == null) _allPlayers = new List<Character>();
        _otherPlayers.Clear();
        _allPlayers.Clear();
        _localPlayer = null;
        var allCharactersInSession = Character.AllCharacters;
        if (allCharactersInSession == null) return;
        _allPlayers.AddRange(allCharactersInSession);
        foreach (Character character in allCharactersInSession)
        {
            if (character == null) continue;

            if (character.photonView.IsMine)
            {
                _localPlayer = character;
            }
            else
            {
                _otherPlayers.Add(character);
            }
        }
    }
    public static Character GetLocalPlayer()
    {
        UpdatePlayerCache();
        return _localPlayer;
    }
    public static List<Character> GetTargets()
    {
        UpdatePlayerCache();
        return _otherPlayers;
    }
    public static List<Character> GetAllCharacters()
    {
        UpdatePlayerCache();
        return _allPlayers;
    }
}