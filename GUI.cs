using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MyCoolMod
{
    public class ModGUI : MonoBehaviour
    {
        private Rect _windowRect = new Rect(20, 20, 800, 700);
        private bool _stylesInitialized = false;
        private enum Tab { Player, Self, Troll, ESP, Misc }
        private Tab _currentTab = Tab.Player;
        private readonly string[] _tabNames = { "Player", "Self", "Troll", "ESP", "Misc" };

        private Vector2 _playerTabScrollPos;
        private Vector2 _selfTabScrollPos;
        private Vector2 _trollTabScrollPos;
        private Vector2 _espTabScrollPos;
        private Vector2 _miscTabScrollPos;
        private Vector2 _playerListScrollPos;

        private enum PlayerAction
        {
            None, Revive, Kill, RenderDead, Bees, Crash, Fling, StunLock, Tumble, Disarm,
            MeteorStrike, SpamPoof, GiveItem,
            Jellyfish, JellyBomb, JellyRain,       // Jellyfish mods
            ForceCarryMe, CarryAndFling           // Carry mods
        }

        private PlayerAction _pendingAction = PlayerAction.None;
        private string _itemToGive = "Flashlight";

        private static Dictionary<string, Character> _playerDict = new Dictionary<string, Character>();
        private string[] _cachedKeys = new string[0];
        private int _selectedPlayerIndex = -1;
        private string _playerSearchBuffer = "";
        private GUIStyle _windowStyle, _labelStyle, _headerLabelStyle, _buttonStyle, _toggleStyle, _textFieldStyle, _boxStyle, _espLabelStyle;
        private static Texture2D _whiteTexture;

        #region Unchanged Code (Setup, Theming, Bone Pairs)
        public static bool NoS;
        public static bool NoP;
        public static bool NoR;
        public static bool NoD;
        public static bool NoSL;
        public static bool NoDstry;

        private struct BonePair
        {
            public readonly HumanBodyBones Start;
            public readonly HumanBodyBones End;
            public BonePair(HumanBodyBones start, HumanBodyBones end) { Start = start; End = end; }
        }

        private static readonly BonePair[] bonePairs = new BonePair[]
        {
            new BonePair(HumanBodyBones.Head, HumanBodyBones.Neck), new BonePair(HumanBodyBones.Neck, HumanBodyBones.UpperChest), new BonePair(HumanBodyBones.UpperChest, HumanBodyBones.Chest), new BonePair(HumanBodyBones.Chest, HumanBodyBones.Spine), new BonePair(HumanBodyBones.Spine, HumanBodyBones.Hips), new BonePair(HumanBodyBones.UpperChest, HumanBodyBones.LeftShoulder), new BonePair(HumanBodyBones.UpperChest, HumanBodyBones.RightShoulder), new BonePair(HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm), new BonePair(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm), new BonePair(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand), new BonePair(HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm), new BonePair(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm), new BonePair(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand), new BonePair(HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg), new BonePair(HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg), new BonePair(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg), new BonePair(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot), new BonePair(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes), new BonePair(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg), new BonePair(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot), new BonePair(HumanBodyBones.RightFoot, HumanBodyBones.RightToes)
        };

        private static class Theme
        {
            public static readonly Color Background = new Color(0.1f, 0.1f, 0.13f, 1f); public static readonly Color Primary = new Color(0.17f, 0.17f, 0.21f, 1f); public static readonly Color Accent = new Color(0.32f, 0.3f, 0.9f, 1f); public static readonly Color AccentActive = new Color(0.4f, 0.38f, 1.0f, 1f); public static readonly Color Text = new Color(0.9f, 0.9f, 0.9f, 1f); public static readonly Color ESP_TextBG = new Color(0, 0, 0, 0.5f);
        }
        #endregion

        void OnGUI()
        {
            if (!_stylesInitialized) InitializeStyles();
            if (Plugin.EspEnabled || Plugin.TracersEnabled || Plugin.BoxEspEnabled || Plugin.HealthBarEspEnabled || Plugin.StaminaBarEspEnabled || Plugin.SkeletonEspEnabled) DrawESP();
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
                case Tab.Player:
                    _playerTabScrollPos = GUILayout.BeginScrollView(_playerTabScrollPos, false, false);
                    DrawPlayerTab();
                    GUILayout.EndScrollView();
                    break;
                case Tab.Self:
                    _selfTabScrollPos = GUILayout.BeginScrollView(_selfTabScrollPos, false, false);
                    DrawSelfTab();
                    GUILayout.EndScrollView();
                    break;
                case Tab.Troll:
                    _trollTabScrollPos = GUILayout.BeginScrollView(_trollTabScrollPos, false, false);
                    DrawTrollTab();
                    GUILayout.EndScrollView();
                    break;
                case Tab.ESP:
                    _espTabScrollPos = GUILayout.BeginScrollView(_espTabScrollPos, false, false);
                    DrawEspTab();
                    GUILayout.EndScrollView();
                    break;
                case Tab.Misc:
                    _miscTabScrollPos = GUILayout.BeginScrollView(_miscTabScrollPos, false, false);
                    DrawMiscTab();
                    GUILayout.EndScrollView();
                    break;
            }

            GUILayout.FlexibleSpace();
            DrawSeparator(10);
            GUILayout.Label("Press F12 to hide menu", _labelStyle);
            GUILayout.EndVertical();
        }

        private void DrawPlayerTab()
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(350));
            Section("Player Settings", () =>
            {
                Plugin.GodModeEnabled = GUILayout.Toggle(Plugin.GodModeEnabled, " God Mode", _toggleStyle);
                GUILayout.Space(10);
                GUILayout.Label($"Damage Multiplier: {Plugin.DamageMultiplier:F2}", _labelStyle);
                Plugin.DamageMultiplier = GUILayout.HorizontalSlider(Plugin.DamageMultiplier, 0.1f, 10.0f);
            });
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            CreatePlayersVerticalSelect();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawSelfTab()
        {
            Section("Self Toggles", () =>
            {
                bool c1 = Character.localCharacter != null && Character.localCharacter.infiniteStam; if (GUILayout.Toggle(c1, " Infinite Stamina (Legacy)", _toggleStyle) != c1) ModUtilities.ToggleInfiniteStamina();
                bool c2 = Character.localCharacter != null && Character.localCharacter.statusesLocked; if (GUILayout.Toggle(c2, " Status Immunity", _toggleStyle) != c2) ModUtilities.ToggleStatusImmunity();
                if (GUILayout.Toggle(Plugin.AlwaysSprintEnabled, " Always Sprint", _toggleStyle) != Plugin.AlwaysSprintEnabled) ModUtilities.ToggleAlwaysSprint();
                if (GUILayout.Toggle(Plugin.NoFallDamageEnabled, " No Fall Damage", _toggleStyle) != Plugin.NoFallDamageEnabled) ModUtilities.ToggleNoFallDamage();
                if (GUILayout.Toggle(Plugin.KeepItemsEnabled, " Keep Items on Death", _toggleStyle) != Plugin.KeepItemsEnabled) ModUtilities.ToggleKeepItemsOnDeath();
            });
            Section("Movement Modifiers", () =>
            {
                GUILayout.Label($"Speed Multiplier: {Plugin.SpeedMultiplier:F2}x", _labelStyle);
                Plugin.SpeedMultiplier = GUILayout.HorizontalSlider(Plugin.SpeedMultiplier, 1f, 100f);
                ModUtilities.SetSpeedMultiplier(Plugin.SpeedMultiplier);
                GUILayout.Space(10);
                GUILayout.Label($"Jump Multiplier: {Plugin.JumpMultiplier:F2}x", _labelStyle);
                Plugin.JumpMultiplier = GUILayout.HorizontalSlider(Plugin.JumpMultiplier, 1f, 50f);
                ModUtilities.SetJumpMultiplier(Plugin.JumpMultiplier);
            });
            Section("Self Actions", () =>
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Revive Self", _buttonStyle)) ModUtilities.ReviveSelf();
                if (GUILayout.Button("Kill Self", _buttonStyle)) ModUtilities.KillSelf();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Jellyfish Self", _buttonStyle)) ModUtilities.JellyfishSelf();
                if (GUILayout.Button("Place Jellyfish Trap", _buttonStyle)) ModUtilities.PlaceJellyfishTrap();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Attack with Bees", _buttonStyle)) ModUtilities.BeesSelf();
                if (GUILayout.Button("<b><color=red>Crash Self</color></b>", _buttonStyle)) ModUtilities.CrashSelf();
                GUILayout.EndHorizontal();
            });
        }

        private void DrawTrollTab()
        {
            if (_pendingAction == PlayerAction.None)
            {
                DrawTrollActionButtons();
            }
            else
            {
                DrawPlayerSelectionForPendingAction();
            }
        }

        private void DrawTrollActionButtons()
        {
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            Section("Single Player Actions", () =>
            {
                GUILayout.Label("Jellyfish:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Jellyfish...", _buttonStyle)) _pendingAction = PlayerAction.Jellyfish;
                if (GUILayout.Button("Jelly Bomb...", _buttonStyle)) _pendingAction = PlayerAction.JellyBomb;
                if (GUILayout.Button("Jelly Rain...", _buttonStyle)) _pendingAction = PlayerAction.JellyRain;
                GUILayout.EndHorizontal();
                DrawSeparator(5);

                GUILayout.Label("Carry:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Force Carry Me...", _buttonStyle)) _pendingAction = PlayerAction.ForceCarryMe;
                if (GUILayout.Button("Carry & Fling...", _buttonStyle)) _pendingAction = PlayerAction.CarryAndFling;
                GUILayout.EndHorizontal();
                DrawSeparator(5);

                GUILayout.Label("Combat / Movement:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Meteor Strike...", _buttonStyle)) _pendingAction = PlayerAction.MeteorStrike;
                if (GUILayout.Button("Fling...", _buttonStyle)) _pendingAction = PlayerAction.Fling;
                if (GUILayout.Button("Bees...", _buttonStyle)) _pendingAction = PlayerAction.Bees;
                GUILayout.EndHorizontal();
                DrawSeparator(5);

                GUILayout.Label("Core Actions:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Kill...", _buttonStyle)) _pendingAction = PlayerAction.Kill;
                if (GUILayout.Button("Revive...", _buttonStyle)) _pendingAction = PlayerAction.Revive;
                if (GUILayout.Button("Disarm...", _buttonStyle)) _pendingAction = PlayerAction.Disarm;
                GUILayout.EndHorizontal();
                DrawSeparator(5);

                GUILayout.Label("Give Item:", _labelStyle);
                _itemToGive = GUILayout.TextField(_itemToGive, _textFieldStyle);
                if (GUILayout.Button("Give Item to Player...", _buttonStyle)) _pendingAction = PlayerAction.GiveItem;
                DrawSeparator(10);

                if (GUILayout.Button("<b><color=red>Crash Player...</color></b>", _buttonStyle)) _pendingAction = PlayerAction.Crash;

            }, GUILayout.Width(370));

            Section("All Players Actions (Excludes You)", () =>
            {
                GUILayout.Label("Core Actions:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Kill All", _buttonStyle)) ModUtilities.KillAllPlayers();
                if (GUILayout.Button("Revive All", _buttonStyle)) ModUtilities.ReviveAllPlayers();
                if (GUILayout.Button("Disarm All", _buttonStyle)) ModUtilities.ActionOnAllPlayers(ModUtilities.DisarmPlayer);
                GUILayout.EndHorizontal();
                DrawSeparator(5);

                GUILayout.Label("Trolling:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Jellyfish All", _buttonStyle)) ModUtilities.JellyfishAll();
                if (GUILayout.Button("Fling All", _buttonStyle)) ModUtilities.FlingAll();
                if (GUILayout.Button("Bees All", _buttonStyle)) ModUtilities.BeesAll();
                GUILayout.EndHorizontal();
                DrawSeparator(5);

                GUILayout.Label("Movement/Grouping:", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Player Chain", _buttonStyle)) StartCoroutine(ModUtilities.CreatePlayerChainRoutine());
                if (GUILayout.Button("Teleport All To Me", _buttonStyle)) ModUtilities.TeleportAllPlayersToMe();
                GUILayout.EndHorizontal();
                DrawSeparator(10);

                if (GUILayout.Button("<b><color=red>Crash All</color></b>", _buttonStyle)) ModUtilities.CrashAll();

            }, GUILayout.ExpandWidth(true));

            GUILayout.EndHorizontal();
        }

        private void DrawPlayerSelectionForPendingAction()
        {
            Section($"Select Player to {_pendingAction}...", () =>
            {
                if (_playerDict.Count == 0) RefreshPlayerDict();
                if (_playerDict.Count == 0)
                {
                    GUILayout.Label("No players found.");
                }

                _playerListScrollPos = GUILayout.BeginScrollView(_playerListScrollPos, GUILayout.Height(400));
                foreach (var playerPair in _playerDict)
                {
                    if (GUILayout.Button(playerPair.Key, _buttonStyle))
                    {
                        Character target = playerPair.Value;
                        switch (_pendingAction)
                        {
                            case PlayerAction.Revive: ModUtilities.RevivePlayer(target); break;
                            case PlayerAction.Kill: ModUtilities.KillPlayer(target); break;
                            case PlayerAction.Bees: ModUtilities.AttackWithBees(target); break;
                            case PlayerAction.Crash: ModUtilities.CrashPlayer(target); break;
                            case PlayerAction.RenderDead: ModUtilities.RenderPlayerDead(target); break;
                            case PlayerAction.Fling: ModUtilities.FlingPlayer(target); break;
                            case PlayerAction.StunLock: ModUtilities.StunLockPlayer(target); break;
                            case PlayerAction.Tumble: ModUtilities.TumblePlayer(target); break;
                            case PlayerAction.Disarm: ModUtilities.DisarmPlayer(target); break;
                            case PlayerAction.MeteorStrike: ModUtilities.MeteorStrikePlayer(target); break;
                            case PlayerAction.SpamPoof: ModUtilities.SpamPoofEffect(target); break;
                            case PlayerAction.GiveItem: ModUtilities.GiveItemToPlayer(target, _itemToGive); break;
                            case PlayerAction.Jellyfish: ModUtilities.SpawnNetworkedJellyfish(target); break;
                            case PlayerAction.JellyBomb: ModUtilities.JellyfishBomb(target); break;
                            case PlayerAction.JellyRain: ModUtilities.JellyfishRain(target); break;
                            case PlayerAction.ForceCarryMe: ModUtilities.ForcePlayerToCarryMe(target); break;
                            case PlayerAction.CarryAndFling: ModUtilities.CarryAndFling(target); break;
                        }
                        _pendingAction = PlayerAction.None;
                        return;
                    }
                }
                GUILayout.EndScrollView();

                if (GUILayout.Button("Cancel", _buttonStyle))
                {
                    _pendingAction = PlayerAction.None;
                }
            });
        }

        #region Remainder of Code (ESP, Misc Tab, Helpers, Styles)
        private void DrawEspTab()
        {
            Section("ESP Settings", () =>
            {
                Plugin.EspEnabled = GUILayout.Toggle(Plugin.EspEnabled, " Player Info (Text)", _toggleStyle);
                Plugin.HeldItemEspEnabled = GUILayout.Toggle(Plugin.HeldItemEspEnabled, " Held Item Name", _toggleStyle);
                Plugin.BoxEspEnabled = GUILayout.Toggle(Plugin.BoxEspEnabled, " 2D Boxes", _toggleStyle);
                Plugin.TracersEnabled = GUILayout.Toggle(Plugin.TracersEnabled, " Tracers (Lines)", _toggleStyle);
                Plugin.SkeletonEspEnabled = GUILayout.Toggle(Plugin.SkeletonEspEnabled, " Skeletons", _toggleStyle);
                Plugin.HealthBarEspEnabled = GUILayout.Toggle(Plugin.HealthBarEspEnabled, " Health Bars", _toggleStyle);
                Plugin.StaminaBarEspEnabled = GUILayout.Toggle(Plugin.StaminaBarEspEnabled, " Stamina Bars", _toggleStyle);
            });
        }

        private void DrawMiscTab()
        {
            Section("Camera", () =>
            {
                Plugin.ThirdPersonEnabled = GUILayout.Toggle(Plugin.ThirdPersonEnabled, " Enable Third-Person Camera", _toggleStyle);
                GUI.enabled = Plugin.ThirdPersonEnabled;
                GUILayout.Space(10);
                GUILayout.Label($"Distance: {Plugin.ThirdPersonDistance:F1}m (Use Scroll Wheel)", _labelStyle);
                Plugin.ThirdPersonDistance = GUILayout.HorizontalSlider(Plugin.ThirdPersonDistance, 1.5f, 10.0f);
                GUILayout.Label($"Height Offset: {Plugin.ThirdPersonHeight:F1}m", _labelStyle);
                Plugin.ThirdPersonHeight = GUILayout.HorizontalSlider(Plugin.ThirdPersonHeight, -1.0f, 2.0f);
                GUILayout.Label($"Camera Smoothing: {Plugin.ThirdPersonSmoothing:F1}", _labelStyle);
                Plugin.ThirdPersonSmoothing = GUILayout.HorizontalSlider(Plugin.ThirdPersonSmoothing, 5f, 30f);
                GUI.enabled = true;
            });
            Section("Utilities", () => { if (GUILayout.Button("Dump All RPCs to File", _buttonStyle)) ModUtilities.DumpRPCsToFile(); });
            Section("Host / World (DANGEROUS)", () =>
            {
                if (GUILayout.Button("End Game (Force Draw)", _buttonStyle)) ModUtilities.EndGame();
                if (GUILayout.Button("Force Win for You", _buttonStyle)) ModUtilities.ForceWinGame();
            });
        }

        private void Section(string title, Action content, params GUILayoutOption[] options)
        {
            GUILayout.BeginVertical(_boxStyle, options);
            GUILayout.Label(title, _headerLabelStyle);
            GUILayout.Space(5);
            content();
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void Section(string title, Action content)
        {
            Section(title, content, Array.Empty<GUILayoutOption>());
        }

        public void CreatePlayersVerticalSelect()
        {
            Section("Players in Session", () =>
            {
                _playerSearchBuffer = GUILayout.TextField(_playerSearchBuffer, _textFieldStyle);
                GUILayout.Space(5);
                string[] filteredKeys = _cachedKeys.Where(name => string.IsNullOrEmpty(_playerSearchBuffer) || name.ToLower().Contains(_playerSearchBuffer.ToLower())).ToArray();

                _playerListScrollPos = GUILayout.BeginScrollView(_playerListScrollPos, GUILayout.ExpandHeight(true));
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

        private void DrawESP()
        {
            if (Character.AllCharacters == null || Camera.main == null) return;
            foreach (var character in Character.AllCharacters)
            {
                if (character == null || character.IsLocal || character.data.dead) continue;

                if (Plugin.SkeletonEspEnabled) DrawSkeleton(character);
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
                        if (Plugin.HeldItemEspEnabled && character.data.currentItem != null) espText += $"\n<color=#00f5d4>{character.data.currentItem.UIData.itemName}</color>";
                        GUI.Label(new Rect(screenPos.x + 8, screenPos.y, 200f, 150f), espText, _espLabelStyle);
                    }
                    if (Plugin.TracersEnabled)
                    {
                        DrawLine(new Vector2(Screen.width / 2, Screen.height), screenPos, Theme.Accent);
                    }
                }
            }
        }

        private void DrawPlayerBox(Character character)
        {
            Bounds bounds = character.refs.mainRenderer.bounds; Vector3 center = bounds.center; Vector3 extents = bounds.extents;
            Vector3[] corners = { center + new Vector3(extents.x, extents.y, extents.z), center + new Vector3(extents.x, extents.y, -extents.z), center + new Vector3(extents.x, -extents.y, extents.z), center + new Vector3(extents.x, -extents.y, -extents.z), center + new Vector3(-extents.x, extents.y, extents.z), center + new Vector3(-extents.x, extents.y, -extents.z), center + new Vector3(-extents.x, -extents.y, extents.z), center + new Vector3(-extents.x, -extents.y, -extents.z) };
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue); Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool isVisible = false;
            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(corner);
                if (screenPoint.z > 0)
                {
                    isVisible = true;
                    Vector2 screenPos = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
                    min = Vector2.Min(min, screenPos); max = Vector2.Max(max, screenPos);
                }
            }
            if (isVisible)
            {
                float width = max.x - min.x; float height = max.y - min.y;
                DrawBox(min.x, min.y, width, height, character.refs.customization.PlayerColor);
                if (Plugin.HealthBarEspEnabled)
                {
                    float healthPercent = 1f - character.refs.afflictions.statusSum;
                    DrawFilledRect(min.x - 7, min.y, 4, height, new Color(1, 0, 0, 0.3f));
                    DrawFilledRect(min.x - 7, min.y + (height * (1 - healthPercent)), 4, height * healthPercent, Color.green);
                }
                if (Plugin.StaminaBarEspEnabled)
                {
                    float staminaPercent = character.GetTotalStamina();
                    DrawFilledRect(max.x + 3, min.y, 4, height, new Color(0.5f, 0.5f, 0.5f, 0.3f));
                    DrawFilledRect(max.x + 3, min.y + (height * (1 - staminaPercent)), 4, height * staminaPercent, Color.blue);
                }
            }
        }
        private void DrawSkeleton(Character character) { Animator animator = character.refs.animator; if (animator == null) return; foreach (var bonePair in bonePairs) { Transform startBone = animator.GetBoneTransform(bonePair.Start), endBone = animator.GetBoneTransform(bonePair.End); if (startBone == null || endBone == null) continue; Vector3 startPos3D = startBone.position, endPos3D = endBone.position; Vector3 startScreenPos = Camera.main.WorldToScreenPoint(startPos3D), endScreenPos = Camera.main.WorldToScreenPoint(endPos3D); if (startScreenPos.z > 0 && endScreenPos.z > 0) { startScreenPos.y = Screen.height - startScreenPos.y; endScreenPos.y = Screen.height - endScreenPos.y; DrawLine(startScreenPos, endScreenPos, character.refs.customization.PlayerColor); } } }
        private void DrawBox(float x, float y, float w, float h, Color color) { DrawLine(new Vector2(x, y), new Vector2(x + w, y), color); DrawLine(new Vector2(x, y), new Vector2(x, y + h), color); DrawLine(new Vector2(x + w, y), new Vector2(x + w, y + h), color); DrawLine(new Vector2(x, y + h), new Vector2(x + w, y + h), color); }
        private void DrawSeparator(float space) { GUILayout.Space(space / 2); var rect = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true)); GUI.color = Theme.Primary; GUI.DrawTexture(rect, _whiteTexture, ScaleMode.StretchToFill); GUI.color = Color.white; GUILayout.Space(space / 2); }
        private void DrawLine(Vector2 start, Vector2 end, Color color) { if (_whiteTexture == null) return; Color savedColor = GUI.color; Matrix4x4 savedMatrix = GUI.matrix; GUI.color = color; Vector2 delta = end - start; float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg; float length = delta.magnitude; GUIUtility.ScaleAroundPivot(new Vector2(length, 1), start); GUIUtility.RotateAroundPivot(angle, start); GUI.DrawTexture(new Rect(start, Vector2.one), _whiteTexture); GUI.matrix = savedMatrix; GUI.color = savedColor; }
        private void DrawFilledRect(float x, float y, float w, float h, Color color) { if (_whiteTexture == null) return; Color savedColor = GUI.color; GUI.color = color; GUI.DrawTexture(new Rect(x, y, w, h), _whiteTexture, ScaleMode.StretchToFill); GUI.color = savedColor; }
        private Texture2D MakeTex(int w, int h, Color c) { var pix = new Color[w * h]; for (int i = 0; i < pix.Length; ++i) pix[i] = c; var result = new Texture2D(w, h) { hideFlags = HideFlags.HideAndDontSave }; result.SetPixels(pix); result.Apply(); return result; }
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            _whiteTexture = MakeTex(1, 1, Color.white);
            var backgroundTex = MakeTex(1, 1, Theme.Background); var primaryTex = MakeTex(1, 1, Theme.Primary); var accentTex = MakeTex(1, 1, Theme.Accent); var accentActiveTex = MakeTex(1, 1, Theme.AccentActive);
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
        #endregion
    }
}

