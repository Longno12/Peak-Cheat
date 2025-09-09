using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zorro.Core;

namespace MyCoolMod
{
    public class ModGUI : MonoBehaviour
    {
        public static ModGUI Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #region --- NEW: Notification System ---

        public enum NotificationType { Info, Success, Warning, Error }

        private class Notification
        {
            public string Title;
            public string Message;
            public NotificationType Type;
            public float StartTime;
            public float Duration;
            public float CurrentAlpha;
        }

        private static List<Notification> _activeNotifications = new List<Notification>();
        private GUIStyle _notificationBoxStyle, _notificationTitleStyle, _notificationMessageStyle;
        public static void ShowNotification(string title, string message, NotificationType type = NotificationType.Info, float duration = 4f)
        {
            _activeNotifications.Add(new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                StartTime = Time.time,
                Duration = duration,
                CurrentAlpha = 0f
            });
        }

        private void DrawNotifications()
        {
            if (_activeNotifications.Count == 0) return;

            float startY = 20f;
            float notificationHeight = 60f;
            float spacing = 10f;
            float notificationWidth = 300f;
            float fadeInTime = 0.3f;
            float fadeOutTime = 0.5f;

            for (int i = _activeNotifications.Count - 1; i >= 0; i--)
            {
                var notif = _activeNotifications[i];
                float age = Time.time - notif.StartTime;
                if (age < fadeInTime) notif.CurrentAlpha = Mathf.Lerp(0, 1, age / fadeInTime);
                else if (age > notif.Duration) notif.CurrentAlpha = Mathf.Lerp(1, 0, (age - notif.Duration) / fadeOutTime);
                else notif.CurrentAlpha = 1f;

                if (age > notif.Duration + fadeOutTime)
                {
                    _activeNotifications.RemoveAt(i);
                    continue;
                }
                Color baseColor = GetNotificationColor(notif.Type);
                Color textColor = Theme.Text;
                textColor.a = notif.CurrentAlpha;
                Color boxColor = Theme.Primary;
                boxColor.a = notif.CurrentAlpha * 0.9f;
                GUI.backgroundColor = boxColor;
                _notificationTitleStyle.normal.textColor = textColor;
                _notificationMessageStyle.normal.textColor = textColor;
                Rect rect = new Rect(Screen.width - notificationWidth - 20f, startY + (i * (notificationHeight + spacing)), notificationWidth, notificationHeight);
                GUI.Box(rect, GUIContent.none, _notificationBoxStyle);
                Rect titleRect = new Rect(rect.x + 10, rect.y + 5, rect.width - 20, 25);
                GUI.Label(titleRect, notif.Title, _notificationTitleStyle);
                Rect messageRect = new Rect(rect.x + 10, rect.y + 25, rect.width - 20, 30);
                GUI.Label(messageRect, notif.Message, _notificationMessageStyle);
            }
        }

        private Color GetNotificationColor(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return Color.green;
                case NotificationType.Warning: return Color.yellow;
                case NotificationType.Error: return Color.red;
                default: return Theme.Accent;
            }
        }
        #endregion

        // --- GUI State & Layout ---
        private Rect _windowRect = new Rect(20, 20, 950, 650);
        private bool _stylesInitialized = false;
        private enum Category { Main, LocalPlayer, PlayerTargeting, GlobalChaos, Visuals, Server, Settings }
        private Category _currentCategory = Category.Main;

        // --- Player Management ---
        private static Dictionary<string, Character> _playerDict = new Dictionary<string, Character>();
        private Character _selectedCharacter;
        private string _selectedPlayerName;
        private string _playerSearchBuffer = "";
        private Vector2 _playerListScrollPos, _playerActionsScrollPos;
        private bool _confirmBadgeUnlock = false;

        // --- Mod & UI State ---
        private string _itemToGive = "Flashlight";
        private Dictionary<string, bool> _sectionStates = new Dictionary<string, bool>();
        private Vector2 _scrollPos;

        private Character _sourcePlayer;
        private Character _destinationPlayer;
        private Customization.Type _selectedCosmeticType = Customization.Type.Hat;
        private string _cosmeticIndexStr = "0";

        // --- GUI Styles ---
        private GUIStyle _windowStyle, _labelStyle, _headerLabelStyle, _subHeaderStyle, _buttonStyle, _toggleStyle, _textFieldStyle;
        private GUIStyle _navButtonStyle, _navButtonActiveStyle, _sectionHeaderStyle, _playerButtonSelectedStyle;
        private static Texture2D _whiteTexture;

        #region Unchanged Code (Bone Pairs, Theming)
        private struct BonePair { public readonly HumanBodyBones Start, End; public BonePair(HumanBodyBones s, HumanBodyBones e) { Start = s; End = e; } }
        private static readonly BonePair[] bonePairs = new BonePair[] { new BonePair(HumanBodyBones.Head, HumanBodyBones.Neck), new BonePair(HumanBodyBones.Neck, HumanBodyBones.UpperChest), new BonePair(HumanBodyBones.UpperChest, HumanBodyBones.Chest), new BonePair(HumanBodyBones.Chest, HumanBodyBones.Spine), new BonePair(HumanBodyBones.Spine, HumanBodyBones.Hips), new BonePair(HumanBodyBones.UpperChest, HumanBodyBones.LeftShoulder), new BonePair(HumanBodyBones.UpperChest, HumanBodyBones.RightShoulder), new BonePair(HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm), new BonePair(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm), new BonePair(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand), new BonePair(HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm), new BonePair(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm), new BonePair(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand), new BonePair(HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg), new BonePair(HumanBodyBones.Hips, HumanBodyBones.RightUpperLeg), new BonePair(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg), new BonePair(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot), new BonePair(HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes), new BonePair(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg), new BonePair(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot), new BonePair(HumanBodyBones.RightFoot, HumanBodyBones.RightToes) };
        private static class Theme { public static readonly Color Background = new Color(0.1f, 0.1f, 0.13f, 1f); public static readonly Color Primary = new Color(0.17f, 0.17f, 0.21f, 1f); public static readonly Color Accent = new Color(0.32f, 0.3f, 0.9f, 1f); public static readonly Color AccentActive = new Color(0.4f, 0.38f, 1.0f, 1f); public static readonly Color Text = new Color(0.9f, 0.9f, 0.9f, 1f); public static readonly Color HeaderBG = new Color(0.12f, 0.12f, 0.15f, 1f); }
        #endregion

        void OnGUI()
        {
            if (!_stylesInitialized) InitializeStyles();

            if (Plugin.EspEnabled || Plugin.TracersEnabled || Plugin.BoxEspEnabled || Plugin.SkeletonEspEnabled) DrawESP();
            if (Plugin.IsGuiVisible)
            {
                _windowRect = GUI.Window(12345, _windowRect, DrawWindow, "", _windowStyle);
            }

            DrawNotifications();
        }

        void DrawWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 30));
            GUILayout.BeginHorizontal();

            DrawNavigation();

            GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) }, GUILayout.ExpandHeight(true));
            switch (_currentCategory)
            {
                case Category.Main: DrawMainCategory(); break;
                case Category.LocalPlayer: DrawLocalPlayerCategory(); break;
                case Category.PlayerTargeting: DrawPlayerTargetingCategory(); break;
                case Category.GlobalChaos: DrawGlobalChaosCategory(); break;
                case Category.Visuals: DrawVisualsCategory(); break;
                case Category.Server: DrawServerCategory(); break;
                case Category.Settings: DrawSettingsCategory(); break;
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawNavigation()
        {
            GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10), normal = { background = MakeTex(1, 1, Theme.HeaderBG) } }, GUILayout.Width(180), GUILayout.ExpandHeight(true));

            GUILayout.Label("PEAK Mod Menu", _headerLabelStyle);
            GUILayout.Label($"v{PluginInfo.PLUGIN_VERSION}", _subHeaderStyle);
            DrawSeparator(10);

            if (NavButton("★ Main", Category.Main)) _currentCategory = Category.Main;
            if (NavButton("☺ Local Player", Category.LocalPlayer)) _currentCategory = Category.LocalPlayer;
            if (NavButton("🎯 Player Targeting", Category.PlayerTargeting)) _currentCategory = Category.PlayerTargeting;
            if (NavButton("⚡ Global Chaos", Category.GlobalChaos)) _currentCategory = Category.GlobalChaos;
            if (NavButton("👁 Visuals", Category.Visuals)) _currentCategory = Category.Visuals;
            if (NavButton("👑 Server", Category.Server)) _currentCategory = Category.Server;
            if (NavButton("⚙ Settings", Category.Settings)) _currentCategory = Category.Settings;

            GUILayout.FlexibleSpace();
            GUILayout.Label("F12 to Hide Menu", _subHeaderStyle);
            GUILayout.EndVertical();
        }

        private void DrawMainCategory()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            GUILayout.Label("Welcome!", _headerLabelStyle);
            GUILayout.Label("Select a category from the left to get started.", _labelStyle);
            GUILayout.Label("The 'Player Targeting' tab is the main hub for interacting with other players in your session.", _labelStyle);
            GUILayout.EndScrollView();
        }

        private void DrawLocalPlayerCategory()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            CollapsibleSection("Toggles", () =>
            {
                if (GUILayout.Toggle(Plugin.AlwaysSprintEnabled, " Always Sprint", _toggleStyle) != Plugin.AlwaysSprintEnabled) ModUtilities.ToggleAlwaysSprint();
                if (GUILayout.Toggle(Plugin.NoFallDamageEnabled, " No Fall Damage", _toggleStyle) != Plugin.NoFallDamageEnabled) ModUtilities.ToggleNoFallDamage();
                if (GUILayout.Toggle(Plugin.KeepItemsEnabled, " Keep Items on Death", _toggleStyle) != Plugin.KeepItemsEnabled) ModUtilities.ToggleKeepItemsOnDeath();
                bool c1 = Character.localCharacter != null && Character.localCharacter.infiniteStam; if (GUILayout.Toggle(c1, " Infinite Stamina", _toggleStyle) != c1) ModUtilities.ToggleInfiniteStamina();
                bool c2 = Character.localCharacter != null && Character.localCharacter.statusesLocked; if (GUILayout.Toggle(c2, " Status Immunity", _toggleStyle) != c2) ModUtilities.ToggleStatusImmunity();
            });

            CollapsibleSection("Flight Controls", () =>
            {
                if (GUILayout.Button(FlyMod.IsFlying ? $"<b><color=lime>Flight System [ON - {FlyMod.CurrentMode}]</color></b>" : "Flight System [OFF]", _buttonStyle))
                {
                    FlyMod.ToggleFly();
                    ShowNotification("Flight System", FlyMod.IsFlying ? "Enabled" : "Disabled");
                }
                GUI.enabled = FlyMod.IsFlying;
                GUILayout.Label("Flight Mode:", _labelStyle);
                FlyMod.CurrentMode = (FlyMode)GUILayout.SelectionGrid((int)FlyMod.CurrentMode, System.Enum.GetNames(typeof(FlyMode)), 3, _buttonStyle);
                FlyMod.IsHovering = GUILayout.Toggle(FlyMod.IsHovering, " Hover (Freeze in place)", _toggleStyle);
                FlyMod.NoClipEnabled = GUILayout.Toggle(FlyMod.NoClipEnabled, " No-Clip (Pass through walls)", _toggleStyle);
                DrawSeparator(5);
                GUILayout.Label($"Fly Speed: {FlyMod.FlySpeed:F0}", _labelStyle);
                FlyMod.FlySpeed = GUILayout.HorizontalSlider(FlyMod.FlySpeed, 10f, 200f);
                GUILayout.Label($"Boost Speed (L-Ctrl): {FlyMod.BoostSpeed:F0}", _labelStyle);
                FlyMod.BoostSpeed = GUILayout.HorizontalSlider(FlyMod.BoostSpeed, 100f, 500f);
                GUI.enabled = true;
            });

            CollapsibleSection("Ground Movement", () =>
            {
                GUILayout.Label($"Speed Multiplier: {Plugin.SpeedMultiplier:F2}x", _labelStyle);
                Plugin.SpeedMultiplier = GUILayout.HorizontalSlider(Plugin.SpeedMultiplier, 1f, 100f);
                ModUtilities.SetSpeedMultiplier(Plugin.SpeedMultiplier);
                GUILayout.Label($"Jump Multiplier: {Plugin.JumpMultiplier:F2}x", _labelStyle);
                Plugin.JumpMultiplier = GUILayout.HorizontalSlider(Plugin.JumpMultiplier, 1f, 50f);
                ModUtilities.SetJumpMultiplier(Plugin.JumpMultiplier);
            });

            CollapsibleSection("Actions", () =>
            {
                if (GUILayout.Button("Revive Self", _buttonStyle)) { ModUtilities.ReviveSelf(); }
                if (GUILayout.Button("Kill Self", _buttonStyle)) { ModUtilities.KillSelf(); }
                if (GUILayout.Button("Trip Self", _buttonStyle)) { ModUtilities.TripSelf(); }
                if (GUILayout.Button("Pass Out Self", _buttonStyle)) { ModUtilities.PassOutSelf(); }
                if (GUILayout.Button("<b><color=red>Crash Self</color></b>", _buttonStyle)) ModUtilities.CrashSelf();
            });

            CollapsibleSection("Self Trolling / Effects", () =>
            {
                if (GUILayout.Button("Bees!", _buttonStyle)) { ModUtilities.BeesSelf(); }
                if (GUILayout.Button("Jellyfish!", _buttonStyle)) { ModUtilities.JellyfishSelf(); }
                if (GUILayout.Button("Cactus!", _buttonStyle)) { ModUtilities.CactusSelf(); }
                if (GUILayout.Button("Explode!", _buttonStyle)) { ModUtilities.ExplodeSelf(); }
                if (GUILayout.Button("Magic Bean!", _buttonStyle)) { ModUtilities.MagicBeanSelf(); }
            });

            GUILayout.EndScrollView();
        }

        private void DrawPlayerTargetingCategory()
        {
            if (_playerDict.Count == 0 && Character.AllCharacters != null && Character.AllCharacters.Count > 1) RefreshPlayerDict();
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(250));
            if (GUILayout.Button("Refresh Players", _buttonStyle)) RefreshPlayerDict();
            _playerSearchBuffer = GUILayout.TextField(_playerSearchBuffer, _textFieldStyle);
            _playerListScrollPos = GUILayout.BeginScrollView(_playerListScrollPos);
            var filteredKeys = _playerDict.Keys.Where(name => string.IsNullOrEmpty(_playerSearchBuffer) || name.ToLower().Contains(_playerSearchBuffer.ToLower())).ToArray();
            foreach (var playerName in filteredKeys)
            {
                bool isSelected = playerName == _selectedPlayerName;
                if (GUILayout.Button(playerName, isSelected ? _playerButtonSelectedStyle : _buttonStyle))
                {
                    _selectedPlayerName = playerName;
                    _selectedCharacter = _playerDict[playerName];
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            if (_selectedCharacter == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Select a Player", _headerLabelStyle);
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.Label($"Actions for: {_selectedPlayerName}", _headerLabelStyle);
                _playerActionsScrollPos = GUILayout.BeginScrollView(_playerActionsScrollPos);

                CollapsibleSection("Core Actions", () => {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Kill", _buttonStyle)) ModUtilities.KillPlayer(_selectedCharacter);
                    if (GUILayout.Button("Revive", _buttonStyle)) ModUtilities.RevivePlayer(_selectedCharacter);
                    if (GUILayout.Button("TP To Me", _buttonStyle)) ModUtilities.TeleportPlayerToMe(_selectedCharacter);
                    if (GUILayout.Button("<b><color=red>Crash</color></b>", _buttonStyle)) ModUtilities.CrashPlayer(_selectedCharacter);
                    GUILayout.EndHorizontal();
                });

                CollapsibleSection("Movement & Control", () => {
                    if (GUILayout.Button("Fling", _buttonStyle)) ModUtilities.FlingPlayer(_selectedCharacter);
                    if (GUILayout.Button("Tumble", _buttonStyle)) ModUtilities.TumblePlayer(_selectedCharacter);
                    if (GUILayout.Button("Trip", _buttonStyle)) ModUtilities.TripPlayer(_selectedCharacter);
                    if (GUILayout.Button("Pass Out", _buttonStyle)) ModUtilities.PassOutPlayer(_selectedCharacter);
                    if (GUILayout.Button("Wake Up", _buttonStyle)) ModUtilities.WakeUpPlayer(_selectedCharacter);
                    if (GUILayout.Button("Stun Lock", _buttonStyle)) ModUtilities.StunLockPlayer(_selectedCharacter);
                    if (GUILayout.Button("Freeze", _buttonStyle)) ModUtilities.FreezePlayer(_selectedCharacter);
                    if (GUILayout.Button("Unfreeze", _buttonStyle)) ModUtilities.UnfreezePlayer(_selectedCharacter);
                    if (GUILayout.Button("Force Push", _buttonStyle)) ModUtilities.ForcePushPlayer(_selectedCharacter);
                    if (GUILayout.Button("Meteor Strike", _buttonStyle)) ModUtilities.MeteorStrikePlayer(_selectedCharacter);
                });

                CollapsibleSection("Spawnables & Traps", () => {
                    if (GUILayout.Button("Explode", _buttonStyle)) ModUtilities.ExplodePlayer(_selectedCharacter);
                    if (GUILayout.Button("Bees!", _buttonStyle)) ModUtilities.AttackWithBees(_selectedCharacter);
                    if (GUILayout.Button("Cactus!", _buttonStyle)) ModUtilities.CactusPlayer(_selectedCharacter);
                    if (GUILayout.Button("Magic Bean!", _buttonStyle)) ModUtilities.MagicBeanPlayer(_selectedCharacter);
                    if (GUILayout.Button("Jellyfish Bomb (5x)", _buttonStyle)) ModUtilities.JellyfishBomb(_selectedCharacter, 5);
                    if (GUILayout.Button("Jellyfish Rain (15x)", _buttonStyle)) ModUtilities.JellyfishRain(_selectedCharacter, 10f, 15);
                    if (GUILayout.Button("Mark with Flare", _buttonStyle)) ModUtilities.MarkPlayerWithFlare(_selectedCharacter);
                });

                CollapsibleSection("Carry & Items", () => {
                    _itemToGive = GUILayout.TextField(_itemToGive, _textFieldStyle);
                    if (GUILayout.Button("Give Item", _buttonStyle)) ModUtilities.GiveItemToPlayer(_selectedCharacter, _itemToGive);
                    if (GUILayout.Button("Disarm", _buttonStyle)) ModUtilities.DisarmPlayer(_selectedCharacter);
                    if (GUILayout.Button("Force To Carry Me", _buttonStyle)) ModUtilities.ForcePlayerToCarryMe(_selectedCharacter);
                    if (GUILayout.Button("Carry & Fling", _buttonStyle)) ModUtilities.CarryAndFling(_selectedCharacter);
                    if (GUILayout.Button("Morale Boost", _buttonStyle)) ModUtilities.MoraleBoostPlayer(_selectedCharacter);
                });

                CollapsibleSection("Appearance", () => {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Make Giant", _buttonStyle)) ModUtilities.MakePlayerGiant(_selectedCharacter);
                    if (GUILayout.Button("Make Tiny", _buttonStyle)) _selectedCharacter.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    if (GUILayout.Button("Reset Size", _buttonStyle)) ModUtilities.ResetPlayerSize(_selectedCharacter);
                    GUILayout.EndHorizontal();
                });

                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawGlobalChaosCategory()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            CollapsibleSection("Mass Player Actions (Excludes You)", () => {
                if (GUILayout.Button("Kill All", _buttonStyle)) ModUtilities.KillAllPlayers();
                if (GUILayout.Button("Revive All", _buttonStyle)) ModUtilities.ReviveAllPlayers();
                if (GUILayout.Button("Pass Out All", _buttonStyle)) ModUtilities.PassOutAll();
                if (GUILayout.Button("Fling All", _buttonStyle)) ModUtilities.FlingAll();
                if (GUILayout.Button("Stun Lock All", _buttonStyle)) ModUtilities.StunLockAll();
                if (GUILayout.Button("Bees on All", _buttonStyle)) ModUtilities.BeesAll();
                if (GUILayout.Button("Jellyfish All", _buttonStyle)) ModUtilities.JellyfishAll();
                if (GUILayout.Button("Cactus All", _buttonStyle)) ModUtilities.CactusAllPlayers();
                if (GUILayout.Button("Explode All", _buttonStyle)) ModUtilities.ExplodeAllPlayers();
                if (GUILayout.Button("Magic Bean All", _buttonStyle)) ModUtilities.MagicBeanAllPlayers();
                if (GUILayout.Button("Make All Tiny", _buttonStyle)) ModUtilities.MakeAllPlayersTiny();
                if (GUILayout.Button("Reset All Sizes", _buttonStyle)) ModUtilities.ResetAllPlayerSizes();
                if (GUILayout.Button("<b><color=red>Crash All</color></b>", _buttonStyle)) ModUtilities.CrashAll();
            });

            CollapsibleSection("Carry Chains & Session", () => {
                if (GUILayout.Button("Create Carry Chain", _buttonStyle)) ModUtilities.ForceCarryChain();
                if (GUILayout.Button("Create Carry Circle", _buttonStyle)) ModUtilities.ForceCarryCircle();
                if (GUILayout.Button("Break All Carries", _buttonStyle)) ModUtilities.BreakAllCarries();
                DrawSeparator(10);
                if (GUILayout.Button("Teleport All To Me", _buttonStyle)) ModUtilities.TeleportAllPlayersToMe();
                if (GUILayout.Button("End Game (Force Draw)", _buttonStyle)) ModUtilities.EndGame();
                if (GUILayout.Button("Force Win For Me", _buttonStyle)) ModUtilities.ForceWinGame();
            });

            GUILayout.EndScrollView();
        }

        private void DrawVisualsCategory()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            CollapsibleSection("ESP", () => {
                Plugin.EspEnabled = GUILayout.Toggle(Plugin.EspEnabled, " Player Info (Name, Distance)", _toggleStyle);
                Plugin.BoxEspEnabled = GUILayout.Toggle(Plugin.BoxEspEnabled, " 2D Boxes", _toggleStyle);
                Plugin.TracersEnabled = GUILayout.Toggle(Plugin.TracersEnabled, " Tracers (Lines to players)", _toggleStyle);
                Plugin.SkeletonEspEnabled = GUILayout.Toggle(Plugin.SkeletonEspEnabled, " Skeletons", _toggleStyle);
            });
            CollapsibleSection("Camera", () => {
                Plugin.ThirdPersonEnabled = GUILayout.Toggle(Plugin.ThirdPersonEnabled, " Third-Person Camera", _toggleStyle);
                GUI.enabled = Plugin.ThirdPersonEnabled;
                GUILayout.Label($"Distance: {Plugin.ThirdPersonDistance:F1}m (Scroll Wheel)", _labelStyle);
                Plugin.ThirdPersonDistance = GUILayout.HorizontalSlider(Plugin.ThirdPersonDistance, 1.5f, 10.0f);
                GUI.enabled = true;
            });
            GUILayout.EndScrollView();
        }

        private void DrawServerCategory()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            GUILayout.Label("Server-Side Cosmetics", _headerLabelStyle);
            GUILayout.Label("These mods affect player appearances for everyone in the lobby.", _labelStyle);
            GUI.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            GUILayout.BeginVertical("box");
            GUILayout.Label("Appearance Features Unavailable", _subHeaderStyle);
            GUILayout.Label("The RPCs required to change player cosmetics were not found in the game dump.", _labelStyle);
            GUILayout.Label("As a result, Appearance Scrambler, Copy Appearance, and Force Change Appearance have been disabled.", _labelStyle);

            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;

            GUILayout.EndScrollView();
        }

        private void DrawSettingsCategory()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            CollapsibleSection("Utilities", () => {
                if (GUILayout.Button("Dump All RPCs to File", _buttonStyle))
                {
                    ModUtilities.DumpRPCsToFile();
                    ShowNotification("Success", "RPCs dumped to Downloads", NotificationType.Success);
                }
                GUILayout.Label("Dumps a list of game functions to your Downloads folder. For advanced users.", _labelStyle);
                GUILayout.Space(10);
                if (_confirmBadgeUnlock)
                {
                    GUILayout.Label("Are you sure you want to unlock all badges?", _labelStyle);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Yes", _buttonStyle))
                    {
                        try
                        {
                            Singleton<AchievementManager>.Instance.DebugGetAllAchievements();
                            ShowNotification("Success", "All badges unlocked!", NotificationType.Success);
                        }
                        catch (Exception ex)
                        {
                            ShowNotification("Error", "Failed to unlock badges.", NotificationType.Error);
                        }
                        _confirmBadgeUnlock = false;
                    }
                    if (GUILayout.Button("No", _buttonStyle)) _confirmBadgeUnlock = false;
                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button("Unlock All Badges", _buttonStyle)) _confirmBadgeUnlock = true;
                    GUILayout.Label("Instantly unlocks all badges/achievements on your account.", _labelStyle);
                }
            });
            GUILayout.EndScrollView();
        }

        #region Helpers, Styles, and Drawing
        private bool NavButton(string text, Category category)
        {
            bool isActive = _currentCategory == category;
            return GUILayout.Button(text, isActive ? _navButtonActiveStyle : _navButtonStyle);
        }
        private void CollapsibleSection(string title, Action content)
        {
            if (!_sectionStates.ContainsKey(title)) _sectionStates[title] = true;
            if (GUILayout.Button($"{(_sectionStates[title] ? "▼" : "►")} {title}", _sectionHeaderStyle)) _sectionStates[title] = !_sectionStates[title];
            if (_sectionStates[title])
            {
                GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(15, 5, 5, 5) });
                content();
                GUILayout.EndVertical();
            }
        }
        private void RefreshPlayerDict()
        {
            _playerDict.Clear();
            var allPlayers = PlayerManager.GetAllCharacters();
            if (allPlayers == null) return;
            foreach (var player in allPlayers)
            {
                if (player != null && !player.IsLocal && !string.IsNullOrEmpty(player.characterName)) _playerDict[player.characterName] = player;
            }
            if (_selectedPlayerName != null && !_playerDict.ContainsKey(_selectedPlayerName))
            {
                _selectedCharacter = null;
                _selectedPlayerName = null;
            }
            ShowNotification("Players", $"Found {_playerDict.Count} other players.");
        }
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            _whiteTexture = MakeTex(1, 1, Color.white);
            var bg = MakeTex(1, 1, Theme.Background);
            var primary = MakeTex(1, 1, Theme.Primary);
            var accent = MakeTex(1, 1, Theme.Accent);
            var headerBg = MakeTex(1, 1, Theme.HeaderBG);

            _windowStyle = new GUIStyle { normal = { background = primary } };
            _headerLabelStyle = new GUIStyle { normal = { textColor = Theme.Text }, fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _subHeaderStyle = new GUIStyle(_headerLabelStyle) { fontSize = 12, fontStyle = FontStyle.Normal };
            _labelStyle = new GUIStyle { normal = { textColor = Theme.Text }, fontSize = 14, wordWrap = true };
            _buttonStyle = new GUIStyle("button") { normal = { background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.25f)), textColor = Theme.Text }, hover = { background = accent }, active = { background = accent }, fontSize = 14, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 8, 8) };
            _playerButtonSelectedStyle = new GUIStyle(_buttonStyle) { normal = { background = accent } };
            _toggleStyle = new GUIStyle("toggle") { normal = { textColor = Theme.Text }, onNormal = { textColor = Theme.Accent }, hover = { textColor = Theme.Accent }, fontSize = 14, padding = new RectOffset(20, 0, 3, 3) };
            _textFieldStyle = new GUIStyle("textfield") { normal = { background = bg, textColor = Theme.Text }, padding = new RectOffset(8, 8, 8, 8), fontSize = 14 };
            _navButtonStyle = new GUIStyle(_buttonStyle) { alignment = TextAnchor.MiddleLeft, normal = { background = headerBg } };
            _navButtonActiveStyle = new GUIStyle(_navButtonStyle) { normal = { background = accent, textColor = Color.white } };
            _sectionHeaderStyle = new GUIStyle(_buttonStyle) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _notificationBoxStyle = new GUIStyle("box") { normal = { background = MakeTex(1, 1, Theme.Primary) } };
            _notificationTitleStyle = new GUIStyle("label") { fontStyle = FontStyle.Bold, fontSize = 14, normal = { textColor = Theme.Text } };
            _notificationMessageStyle = new GUIStyle("label") { fontSize = 12, normal = { textColor = Theme.Text } };
            _stylesInitialized = true;
        }
        private void DrawESP()
        {
            if (Camera.main == null) return;
            foreach (var character in PlayerManager.GetAllCharacters())
            {
                if (character == null || character.IsLocal || character.data.dead) continue;
                if (Plugin.SkeletonEspEnabled) DrawSkeleton(character);
                if (Plugin.BoxEspEnabled) ModUtilities.BoxESP();
                Vector3 headPos = character.Head;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(headPos);
                if (screenPos.z > 0)
                {
                    screenPos.y = Screen.height - screenPos.y;
                    if (Plugin.EspEnabled) GUI.Label(new Rect(screenPos.x + 8, screenPos.y, 200f, 150f), $"<b>{character.characterName}</b>\n[{Vector3.Distance(Camera.main.transform.position, headPos):F1}m]");
                    if (Plugin.TracersEnabled) DrawLine(new Vector2(Screen.width / 2, Screen.height), screenPos, Theme.Accent);
                }
            }
        }
        private void DrawSkeleton(Character character) { Animator animator = character.refs.animator; if (animator == null) return; foreach (var bonePair in bonePairs) { Transform startBone = animator.GetBoneTransform(bonePair.Start), endBone = animator.GetBoneTransform(bonePair.End); if (startBone == null || endBone == null) continue; Vector3 startPos3D = startBone.position, endPos3D = endBone.position; Vector3 startScreenPos = Camera.main.WorldToScreenPoint(startPos3D), endScreenPos = Camera.main.WorldToScreenPoint(endPos3D); if (startScreenPos.z > 0 && endScreenPos.z > 0) { startScreenPos.y = Screen.height - startScreenPos.y; endScreenPos.y = Screen.height - endScreenPos.y; DrawLine(startScreenPos, endScreenPos, character.refs.customization.PlayerColor); } } }
        private void DrawSeparator(float space) { GUILayout.Space(space / 2); var rect = GUILayoutUtility.GetRect(10, 1, GUILayout.ExpandWidth(true)); GUI.color = Theme.Background; GUI.DrawTexture(rect, _whiteTexture); GUI.color = Color.white; GUILayout.Space(space / 2); }
        private void DrawLine(Vector2 start, Vector2 end, Color color) { if (_whiteTexture == null) return; Color savedColor = GUI.color; Matrix4x4 savedMatrix = GUI.matrix; GUI.color = color; Vector2 delta = end - start; float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg; float length = delta.magnitude; GUIUtility.ScaleAroundPivot(new Vector2(length, 1), start); GUIUtility.RotateAroundPivot(angle, start); GUI.DrawTexture(new Rect(start, Vector2.one), _whiteTexture); GUI.matrix = savedMatrix; GUI.color = savedColor; }
        private Texture2D MakeTex(int w, int h, Color c) { var pix = new Color[w * h]; for (int i = 0; i < pix.Length; ++i) pix[i] = c; var result = new Texture2D(w, h) { hideFlags = HideFlags.HideAndDontSave }; result.SetPixels(pix); result.Apply(); return result; }
        #endregion
    }
}