// GUI.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyCoolMod
{
    public class ModGUI : MonoBehaviour
    {
        private Rect _windowRect = new Rect(20, 20, 680, 540);
        private bool _stylesInitialized = false;
        private enum Tab { Player, Self, Troll, ESP, Misc }
        private Tab _currentTab = Tab.Player;
        private readonly string[] _tabNames = { "Player", "Self", "Troll", "ESP", "Misc" };
        private enum PlayerAction { None, Revive, Kill, Stick, UnStick, TeleportToMe, Bees }
        private PlayerAction _pendingAction = PlayerAction.None;
        private static Dictionary<string, Character> _playerDict = new Dictionary<string, Character>();
        private string[] _cachedKeys = new string[0];
        private int _selectedPlayerIndex = -1;
        private string _playerSearchBuffer = "";
        private Vector2 _playerScrollPos;
        private GUIStyle _windowStyle, _labelStyle, _headerLabelStyle, _buttonStyle, _toggleStyle, _textFieldStyle, _boxStyle, _espLabelStyle;
        private static Texture2D _whiteTexture;

        private static class Theme
        {
            public static readonly Color Background = new Color(0.1f, 0.1f, 0.13f, 1f);
            public static readonly Color Primary = new Color(0.17f, 0.17f, 0.21f, 1f);
            public static readonly Color Accent = new Color(0.32f, 0.3f, 0.9f, 1f);
            public static readonly Color AccentActive = new Color(0.4f, 0.38f, 1.0f, 1f);
            public static readonly Color Text = new Color(0.9f, 0.9f, 0.9f, 1f);
            public static readonly Color ESP_TextBG = new Color(0, 0, 0, 0.5f);
        }

        void OnGUI()
        {
            if (!_stylesInitialized) InitializeStyles();
            if (Plugin.EspEnabled || Plugin.TracersEnabled || Plugin.BoxEspEnabled) DrawESP();
            if (!Plugin.IsGuiVisible) return;
            _windowRect = GUILayout.Window(12345, _windowRect, DrawWindow, $" {PluginInfo.PLUGIN_NAME} ", _windowStyle);
        }

        void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 25));
            GUILayout.BeginVertical();
            GUILayout.Label($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION}", _headerLabelStyle);
            DrawSeparator(15);
            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, _tabNames, _buttonStyle);
            GUILayout.Space(10);
            switch (_currentTab)
            {
                case Tab.Player: DrawPlayerTab(); break;
                case Tab.Self: DrawSelfTab(); break;
                case Tab.Troll: DrawTrollTab(); break;
                case Tab.ESP: DrawEspTab(); break;
                case Tab.Misc: DrawMiscTab(); break;
            }
            GUILayout.FlexibleSpace();
            DrawSeparator(10);
            GUILayout.Label("Press F12 to hide menu", _labelStyle);
            GUILayout.EndVertical();
        }

        private void DrawESP()
        {
            if (Character.AllCharacters == null || Camera.main == null) return;
            foreach (var character in Character.AllCharacters)
            {
                if (character == null || character.IsLocal || character.data.dead) continue;
                if (Plugin.BoxEspEnabled && character.refs.mainRenderer != null) DrawPlayerBox(character);
                Vector3 headPos = character.Head;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(headPos);
                if (screenPos.z > 0)
                {
                    screenPos.y = Screen.height - screenPos.y;
                    if (Plugin.EspEnabled)
                    {
                        string espText = $"<b>{character.characterName}</b>\n[{Vector3.Distance(Camera.main.transform.position, headPos):F1}m]";
                        if (character.data.passedOut) espText += "\n<color=yellow>Passed Out</color>";
                        GUI.Label(new Rect(screenPos.x + 8, screenPos.y, 200f, 150f), espText, _espLabelStyle);
                    }
                    if (Plugin.TracersEnabled)
                    {
                        DrawLine(new Vector2(Screen.width / 2, Screen.height), screenPos, Theme.Accent);
                    }
                }
            }
        }

        private void DrawPlayerTab()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(300));
            Section("Player Settings", () =>
            {
                Plugin.GodModeEnabled = GUILayout.Toggle(Plugin.GodModeEnabled, " God Mode", _toggleStyle);
                GUILayout.Space(10);
                GUILayout.Label($"Damage Multiplier: {Plugin.DamageMultiplier:F2}", _labelStyle);
                Plugin.DamageMultiplier = GUILayout.HorizontalSlider(Plugin.DamageMultiplier, 0.1f, 10.0f);
            });
            GUILayout.EndVertical();
            CreatePlayersVerticalSelect();
            GUILayout.EndHorizontal();
        }

        private void DrawSelfTab()
        {
            Section("Self Toggles", () =>
            {
                bool currentInfiniteStam = Character.localCharacter != null && Character.localCharacter.infiniteStam;
                if (GUILayout.Toggle(currentInfiniteStam, " Infinite Stamina", _toggleStyle) != currentInfiniteStam) ModUtilities.ToggleInfiniteStamina();
                bool currentStatusImmunity = Character.localCharacter != null && Character.localCharacter.statusesLocked;
                if (GUILayout.Toggle(currentStatusImmunity, " Status Immunity", _toggleStyle) != currentStatusImmunity) ModUtilities.ToggleStatusImmunity();
            });
            Section("Movement Modifiers", () =>
            {
                GUILayout.Label($"Speed Multiplier: {Plugin.SpeedMultiplier:F2}x", _labelStyle);
                Plugin.SpeedMultiplier = GUILayout.HorizontalSlider(Plugin.SpeedMultiplier, 1.0f, 5.0f);
                ModUtilities.SetSpeedMultiplier(Plugin.SpeedMultiplier);
                GUILayout.Space(10);
                GUILayout.Label($"Jump Multiplier: {Plugin.JumpMultiplier:F2}x", _labelStyle);
                Plugin.JumpMultiplier = GUILayout.HorizontalSlider(Plugin.JumpMultiplier, 1.0f, 10.0f);
                ModUtilities.SetJumpMultiplier(Plugin.JumpMultiplier);
            });
            Section("Self Actions", () =>
            {
                if (GUILayout.Button("Revive Self", _buttonStyle)) ModUtilities.ReviveSelf();
                if (GUILayout.Button("Kill Self", _buttonStyle)) ModUtilities.KillSelf();
                if (GUILayout.Button("Trip Self", _buttonStyle)) ModUtilities.TripSelf();
                if (GUILayout.Button("Attack Self with Bees", _buttonStyle)) ModUtilities.BeesSelf();
            });
        }

        private void DrawTrollTab()
        {
            if (_pendingAction == PlayerAction.None) DrawTrollActionButtons(); else DrawPlayerSelectionForPendingAction();
        }

        private void DrawTrollActionButtons()
        {
            GUILayout.BeginHorizontal();
            Section("Single Player Actions", () =>
            {
                if (GUILayout.Button("Revive Player...", _buttonStyle)) _pendingAction = PlayerAction.Revive;
                if (GUILayout.Button("Kill Player...", _buttonStyle)) _pendingAction = PlayerAction.Kill;
                if (GUILayout.Button("Stick Player...", _buttonStyle)) _pendingAction = PlayerAction.Stick;
                if (GUILayout.Button("Unstick Player...", _buttonStyle)) _pendingAction = PlayerAction.UnStick;
                if (GUILayout.Button("Teleport To Me...", _buttonStyle)) _pendingAction = PlayerAction.TeleportToMe;
                if (GUILayout.Button("Attack with Bees...", _buttonStyle)) _pendingAction = PlayerAction.Bees;
            });
            Section("All Players Actions (Excludes You)", () =>
            {
                if (GUILayout.Button("Revive All", _buttonStyle)) ModUtilities.ActionOnAllPlayers(ModUtilities.RevivePlayer);
                if (GUILayout.Button("Kill All", _buttonStyle)) ModUtilities.ActionOnAllPlayers(ModUtilities.KillPlayer);
                if (GUILayout.Button("Stick All", _buttonStyle)) ModUtilities.ActionOnAllPlayers(ModUtilities.StickPlayer);
                if (GUILayout.Button("Unstick All", _buttonStyle)) ModUtilities.ActionOnAllPlayers(ModUtilities.UnStickPlayer);
                if (GUILayout.Button("Bees All", _buttonStyle)) ModUtilities.BeesAll();
                if (GUILayout.Button("Teleport All To Me", _buttonStyle)) ModUtilities.TeleportAllPlayersToMe();
                if (GUILayout.Button("Pass Out All", _buttonStyle)) ModUtilities.PassOutAll();
            });
            GUILayout.EndHorizontal();
        }

        private void DrawPlayerSelectionForPendingAction()
        {
            Section($"Select Player to {_pendingAction}...", () =>
            {
                if (_playerDict.Count == 0) RefreshPlayerDict();
                if (_playerDict.Count == 0) GUILayout.Label("No players found.");
                _playerScrollPos = GUILayout.BeginScrollView(_playerScrollPos, GUILayout.Height(300));
                foreach (var playerPair in _playerDict)
                {
                    if (GUILayout.Button(playerPair.Key, _buttonStyle))
                    {
                        Character target = playerPair.Value;
                        switch (_pendingAction)
                        {
                            case PlayerAction.Revive: ModUtilities.RevivePlayer(target); break;
                            case PlayerAction.Kill: ModUtilities.KillPlayer(target); break;
                            case PlayerAction.Stick: ModUtilities.StickPlayer(target); break;
                            case PlayerAction.UnStick: ModUtilities.UnStickPlayer(target); break;
                            case PlayerAction.TeleportToMe: ModUtilities.TeleportPlayerToMe(target); break;
                            case PlayerAction.Bees: ModUtilities.AttackWithBees(target); break;
                        }
                        _pendingAction = PlayerAction.None; return;
                    }
                }
                GUILayout.EndScrollView();
                if (GUILayout.Button("Cancel", _buttonStyle)) _pendingAction = PlayerAction.None;
            });
        }

        private void DrawEspTab()
        {
            Section("ESP Settings", () =>
            {
                Plugin.EspEnabled = GUILayout.Toggle(Plugin.EspEnabled, " Enable Player Info (Text)", _toggleStyle);
                Plugin.TracersEnabled = GUILayout.Toggle(Plugin.TracersEnabled, " Enable Player Tracers (Lines)", _toggleStyle);
                Plugin.BoxEspEnabled = GUILayout.Toggle(Plugin.BoxEspEnabled, " Enable Player Boxes", _toggleStyle);
            });
        }

        private void DrawMiscTab()
        {
            Section("Utilities", () => { if (GUILayout.Button("Dump All RPCs to File", _buttonStyle)) ModUtilities.DumpRPCsToFile(); });
            Section("Host / World (DANGEROUS)", () =>
            {
                if (GUILayout.Button("End Game (Force Draw)", _buttonStyle)) ModUtilities.EndGame();
                if (GUILayout.Button("Force Win for You", _buttonStyle)) ModUtilities.ForceWinGame();
            });
        }

        private void Section(string title, Action content)
        {
            GUILayout.BeginVertical(_boxStyle);
            GUILayout.Label(title, _headerLabelStyle);
            GUILayout.Space(5);
            content();
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        public void CreatePlayersVerticalSelect()
        {
            Section("Players in Session", () =>
            {
                _playerSearchBuffer = GUILayout.TextField(_playerSearchBuffer, _textFieldStyle);
                GUILayout.Space(5);
                string[] filteredKeys = _cachedKeys.Where(name => string.IsNullOrEmpty(_playerSearchBuffer) || name.ToLower().Contains(_playerSearchBuffer.ToLower())).ToArray();
                _playerScrollPos = GUILayout.BeginScrollView(_playerScrollPos, GUILayout.ExpandHeight(true));
                if (filteredKeys.Length > 0)
                {
                    for (int i = 0; i < filteredKeys.Length; i++)
                    {
                        string playerName = filteredKeys[i];
                        if (GUILayout.Button(playerName, _buttonStyle)) _selectedPlayerIndex = i;
                    }
                }
                else GUILayout.Label("No matching players found.");
                GUILayout.EndScrollView();
            });
        }
        private void RefreshPlayerDict()
        {
            _playerDict.Clear();
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) return;
            foreach (var player in allPlayers)
            {
                if (player != null && player.photonView?.Owner?.NickName != null)
                {
                    string nickName = player.photonView.Owner.NickName;
                    if (_playerDict.ContainsKey(nickName))
                    {
                        for (int i = 2; ; i++)
                        {
                            string newName = $"{nickName} ({i})";
                            if (!_playerDict.ContainsKey(newName)) { nickName = newName; break; }
                        }
                    }
                    _playerDict.Add(nickName, player);
                }
            }
            _cachedKeys = _playerDict.Keys.ToArray();
            _selectedPlayerIndex = -1;
        }
        public static Dictionary<string, Character> GetPlayerDict() => _playerDict;

        private void DrawPlayerBox(Character character)
        {
            Bounds bounds = character.refs.mainRenderer.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            Vector3[] corners = {
                center + new Vector3( extents.x,  extents.y,  extents.z), center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z), center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z), center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z), center + new Vector3(-extents.x, -extents.y, -extents.z)
            };
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool isVisible = false;
            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(corner);
                if (screenPoint.z > 0)
                {
                    isVisible = true;
                    Vector2 screenPos = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                    min = Vector2.Min(min, screenPos);
                    max = Vector2.Max(max, screenPos);
                }
            }
            if (isVisible)
            {
                float width = max.x - min.x;
                float height = max.y - min.y;
                Color color = character.refs.customization.PlayerColor;
                DrawBox(min.x, min.y, width, height, color);
            }
        }
        private void DrawBox(float x, float y, float w, float h, Color color)
        {
            DrawLine(new Vector2(x, y), new Vector2(x + w, y), color);
            DrawLine(new Vector2(x, y), new Vector2(x, y + h), color);
            DrawLine(new Vector2(x + w, y), new Vector2(x + w, y + h), color);
            DrawLine(new Vector2(x, y + h), new Vector2(x + w, y + h), color);
        }
        private void DrawSeparator(float space)
        {
            GUILayout.Space(space / 2);
            var rect = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true));
            GUI.color = Theme.Primary;
            GUI.DrawTexture(rect, _whiteTexture, ScaleMode.StretchToFill);
            GUI.color = Color.white;
            GUILayout.Space(space / 2);
        }
        private void DrawLine(Vector2 start, Vector2 end, Color color)
        {
            if (_whiteTexture == null) return;
            Color savedColor = GUI.color;
            Matrix4x4 savedMatrix = GUI.matrix;
            GUI.color = color;
            Vector2 delta = end - start;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            float length = delta.magnitude;
            GUIUtility.ScaleAroundPivot(new Vector2(length, 1), start);
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start, Vector2.one), _whiteTexture);
            GUI.matrix = savedMatrix;
            GUI.color = savedColor;
        }
        private Texture2D MakeTex(int w, int h, Color c)
        {
            var pix = new Color[w * h]; for (int i = 0; i < pix.Length; ++i) pix[i] = c;
            var result = new Texture2D(w, h) { hideFlags = HideFlags.HideAndDontSave };
            result.SetPixels(pix); result.Apply(); return result;
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            _whiteTexture = MakeTex(1, 1, Color.white);
            var backgroundTex = MakeTex(1, 1, Theme.Background);
            var primaryTex = MakeTex(1, 1, Theme.Primary);
            var accentTex = MakeTex(1, 1, Theme.Accent);
            var accentActiveTex = MakeTex(1, 1, Theme.AccentActive);

            _windowStyle = new GUIStyle { normal = { background = backgroundTex, textColor = Theme.Text }, padding = new RectOffset(10, 10, 10, 10), border = new RectOffset(0, 0, 0, 0) };
            _headerLabelStyle = new GUIStyle { normal = { textColor = Theme.Text }, fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 5, 10) };
            _labelStyle = new GUIStyle { normal = { textColor = Theme.Text }, fontSize = 14, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleLeft };
            _espLabelStyle = new GUIStyle(_labelStyle) { normal = { background = MakeTex(1, 1, Theme.ESP_TextBG), textColor = Theme.Text }, padding = new RectOffset(4, 4, 4, 4) };
            _buttonStyle = new GUIStyle { normal = { background = primaryTex, textColor = Theme.Text }, hover = { background = accentTex, textColor = Theme.Text }, active = { background = accentActiveTex, textColor = Theme.Text }, onNormal = { background = accentTex, textColor = Theme.Text }, padding = new RectOffset(10, 10, 10, 10), fontSize = 14, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, margin = new RectOffset(0, 0, 2, 2), border = new RectOffset(0, 0, 0, 0) };
            _toggleStyle = new GUIStyle("toggle") { normal = { textColor = Theme.Text }, hover = { textColor = Theme.Accent }, active = { textColor = Theme.AccentActive }, onNormal = { textColor = Theme.Accent }, fontSize = 14, padding = new RectOffset(20, 0, 3, 3) };
            _textFieldStyle = new GUIStyle("textfield") { normal = { background = primaryTex, textColor = Theme.Text }, hover = { background = primaryTex, textColor = Theme.Text }, active = { background = primaryTex, textColor = Theme.Text }, focused = { background = primaryTex, textColor = Theme.Text }, padding = new RectOffset(8, 8, 8, 8), fontSize = 14 };
            _boxStyle = new GUIStyle("box") { normal = { background = primaryTex }, padding = new RectOffset(10, 10, 10, 10) };
            _stylesInitialized = true;
        }
    }
}