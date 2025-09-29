# Peak Mod GUI

A comprehensive BepInEx-based mod for the game **PEAK**. It provides a clean, modern, ImGui-inspired user interface to access a wide range of features, from client-sided enhancements and ESP to powerful trolling and host utilities.

---

## Features

The menu is organized into several tabs for ease of use:

#### 🎨 ESP
- **Player Info:** Display player names, distance, and current status (e.g., "Passed Out").
- **Tracers:** Draw lines from the bottom of your screen to each player.
- **2D Bounding Boxes:** Draw a box around each player's character model, colored to match their in-game color.

#### 🚀 Self
- **Movement Hacks:** Sliders to control your personal Speed and Jump multipliers.
- **Infinite Stamina:** Toggle on/off infinite stamina.
- **Status Immunity:** Toggle on/off immunity to negative status effects.
- **Self-Actions:** Buttons to instantly Revive, Kill, Trip, or attack yourself with bees.

#### 😈 Troll
- **Targeted Player Actions:** A "click-to-select" system to perform actions on a specific player.
  - Revive, Kill, Knock Out, Stick/Unstick Player, Teleport to You, Attack with Bees, and Crash Player.
- **All Player Actions:** Buttons to apply an effect to every other player in the lobby simultaneously.
  - Kill All, Bees All, Teleport All to Me, Crash All, etc.
- **Multiple Crash Methods:**
  - **Destroy Objects (Authoritative):** The standard crasher that requires Master Client.
  - **RPC Spam (Targeted Lag):** Lags out a single player without affecting others.
  - **Instantiation Spam (Server Lag):** Lags out the entire server.

#### ⚙️ Misc & World
- **Host Utilities:** End the game or force a win for your team (requires Master Client).
- **RPC Dumper:** A developer tool to dump all game RPCs to a text file in your Downloads folder.

---

## 💾 Installation (For Users)

1.  **Install BepInEx:** Make sure you have [BepInEx 5](https://github.com/BepInEx/BepInEx/releases) installed for PEAK.
2.  **Download:** Download the latest `Peak.Mod.dll` from the [Releases](https://github.com/Longno12/Peak-Cheat/releases) page of this repository.
3.  **Install Mod:** Place the `Peak.Mod.dll` file inside your game's `BepInEx/plugins` folder.
4.  **Run Game:** Launch the game. The mod will load automatically. Press **INSERT** to open and close the menu.

---

## 🛠️ Building from Source (For Developers)

#### Prerequisites
-   Visual Studio 2022 or another modern IDE.
-   .NET 6 SDK (or newer).
-   A copy of the game PEAK.

#### 1. Clone the Repository
```bash
git clone https://github.com/Longno12/Peak-Cheat.git
cd Peak-Cheat
