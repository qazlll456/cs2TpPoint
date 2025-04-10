# cs2TpPoint

A CounterStrikeSharp plugin for Counter-Strike 2 (CS2) that allows admins to save teleport points, teleport players or teams to those points, and toggle noclip for easier server management. Designed for servers like Jailbreak and Zombie Escape to streamline teleportation tasks.

## Overview
This plugin provides a simple way for CS2 server admins to manage teleportation and movement. It supports saving teleport points on a per-map basis, teleporting players or teams to those points, and toggling noclip for admins. It’s particularly useful for custom game modes like Jailbreak and Zombie Escape, where precise player positioning is often needed.

- Module Name: cs2TpPoint
- Version: 1.0.0
- Author: qazlll456 from HK with xAI assistance
- Description: A plugin for teleport point management and noclip in CS2

## Features
- Save up to 10 teleport points per map with !settppoint.
- Teleport yourself or a team to a saved point with !tpp.
- Teleport a team to your location with !tpphere.
- List all saved teleport points with !listtpp.
- Clear specific or all teleport points with !cleartpp.
- Toggle noclip for admins with !tpnoclip.

## Installation

### Prerequisites
To use this plugin, you need:
- Counter-Strike 2 Dedicated Server: A running CS2 server.
- CounterStrikeSharp: The C# plugin framework for CS2. Download the latest version from [GitHub releases](https://github.com/roflmuffin/CounterStrikeSharp/releases) (choose the "with runtime" version if it’s your first install).

### Steps
1. Download the latest release from [Releases](https://github.com/qazlll456/cs2TpPoint/releases) or clone the repository:
   > git clone https://github.com/qazlll456/cs2TpPoint.git
2. Copy the cs2TpPoint folder to csgo/addons/counterstrikesharp/plugins/.
3. Start or restart your server, or load the plugin manually:
   > css_plugins load cs2TpPoint

## Usage

### Commands
| Command            | Arguments                     | Description                                                                 |
|--------------------|-------------------------------|-----------------------------------------------------------------------------|
| !settppoint        | <id\|name>                    | Saves a teleport point at your current position (e.g., !settppoint 1).     |
| !tpp               | tpp <id\|name>                | Teleports yourself to a saved point (e.g., !tpp tpp 1).                    |
| !tpp               | @t\|@ct tpp <id\|name>        | Teleports a team to a point (e.g., !tpp @ct tpp 1).                        |
| !tpphere           | @t\|@ct                       | Teleports a team to your location (e.g., !tpphere @t).                     |
| !listtpp           |                               | Lists all saved teleport points for the current map.                       |
| !cleartpp          | <id\|name>                    | Clears a specific teleport point (e.g., !cleartpp 1).                      |
| !cleartpp          | @all                          | Clears all teleport points for the current map.                            |
| !tpnoclip          | [player]                      | Toggles noclip for yourself or another player (e.g., !tpnoclip qazlll456). |

### Examples
#### Zombie Escape Map - Boss Fight Teleport
In a Zombie Escape map, you can set a teleport point for a boss fight area to quickly move players during the event:
1. Stand at the boss fight location (e.g., a safe platform or arena) and save the point:
   > !settppoint boss
   - This saves your current position as "boss" in the map’s teleport point list.
2. When the boss fight starts, teleport all Counter-Terrorists (humans) to the boss area:
   > !tpp @ct tpp boss
   - All Counter-Terrorists will be teleported to the "boss" point.
3. If you need to inspect the area first (e.g., to check for map issues), toggle noclip to fly around:
   > !tpnoclip
   - You can now fly through walls to ensure the area is ready.

#### Jailbreak Map - Gun Room Teleport
In a Jailbreak map, you can set a teleport point for the gun room to move players during games:
1. Stand in the gun room and save the point:
   > !settppoint gunroom
   - This saves your current position as "gunroom" in the map’s teleport point list.
2. Teleport the Terrorists (prisoners) to the gun room for an event (e.g., a gun game):
   > !tpp @t tpp gunroom
   - All Terrorists will be teleported to the "gunroom" point.
3. Alternatively, if you’re already in the gun room, teleport the Terrorists to your location:
   > !tpphere @t
   - All Terrorists will be teleported to where you’re standing.

## Requirements
- Counter-Strike 2 server.
- CounterStrikeSharp (minimum API version 80).
- Admin permissions with @css/root or @css/slay to use commands.

## Building from Source
1. Clone the repository:
   > git clone https://github.com/qazlll456/cs2TpPoint.git
2. Open the project in Visual Studio or another C# IDE.
3. Restore the dependencies using the .csproj file.
4. Build the project to generate the plugin DLL.

## Donate
If you enjoy it and find it helpful, consider donating to me! Every bit helps me keep developing.
Money, Steam games, or any valuable contribution is welcome.
- Ko-fi: [Support on Ko-fi](https://ko-fi.com/qazlll456)
- Patreon: [Become a Patron](https://www.patreon.com/c/qazlll456)
- Streamlabs: [Tip via Streamlabs](https://streamlabs.com/BKCqazlll456/tip)

## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments
- Built with assistance from xAI.
- Thanks to the CounterStrikeSharp community for their support.