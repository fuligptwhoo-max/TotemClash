# Totem Clash - AI Agent Documentation

## Project Overview

**Totem Clash** is a single-player 3D arena combat game built in Unity. Player controls a magician character, casts fireball spells at AI bots, and competes to capture and hold a totem for points.

### Key Information
- **Project Name**: Totem Clash
- **Company**: BredInc
- **Unity Version**: 6000.2.6f2 (Unity 6)
- **Product Version**: 0.1.0
- **Bundle ID (Android)**: com.bredinc.totemclash
- **Template**: URP (Universal Render Pipeline) Blank Template
- **Game Mode**: Single Player (with AI bots)

---

## Technology Stack

### Core Technologies
| Component | Version | Description |
|-----------|---------|-------------|
| Unity Editor | 6000.2.6f2 | Unity 6 LTS |
| Render Pipeline | URP 17.2.0 | Universal Render Pipeline |
| Input System | 1.14.2 | New Unity Input System |
| Text Rendering | TextMeshPro | UI text rendering |
| glTF Support | 2.18.5 | UnityGLTF from Needle Tools |

### Third-Party Packages
- **UnityGLTF**: Khronos Group glTF implementation for 3D models
- **Newtonsoft JSON**: JSON serialization

### Scripting & Compilation
- **Language**: C# 
- **Scripting Backend**: IL2CPP (Android)
- **API Compatibility**: .NET Framework

---

## Project Structure

```
d:\programs\unity\source/
├── .git/                      # Git version control
├── .plastic/                  # Plastic SCM configuration
├── .vscode/                   # VS Code settings
├── Assets/
│   ├── Animations/            # Character animation assets
│   ├── Audio/                 # Sound effects and music
│   ├── Materials/             # Material assets
│   ├── Models/                # 3D models (Magician character, fireball)
│   ├── Prefabs/
│   │   ├── Players/Magician/  # Player character prefab
│   │   ├── Projectiles/       # Fireball projectile prefab
│   │   ├── Totem/             # Game objective prefab
│   │   └── UI/                # UI element prefabs
│   ├── Resources/             # Runtime-loaded resources
│   ├── Scenes/
│   │   ├── MainMenu.unity     # Main menu scene
│   │   └── SampleScene.unity  # Main game scene
│   ├── SceneSwitcherPro/      # Scene management tool
│   ├── Scripts/               # Game logic
│   │   ├── Classes/           # Character classes
│   │   ├── Combat/            # Combat systems
│   │   ├── Network/           # Game settings (was: networking)
│   │   └── UI/                # User interface
│   ├── Settings/              # URP render settings
│   ├── Spags Assets/          # Third-party assets
│   ├── TextMesh Pro/          # Text rendering
│   ├── Textures/              # Texture assets
│   └── Tree_Textures/         # Environment textures
├── Packages/
│   ├── manifest.json          # Package dependencies
│   └── packages-lock.json     # Locked package versions
└── ProjectSettings/           # Unity project configuration
```

---

## Code Organization

### Script Directory Structure

```
Assets/Scripts/
├── Classes/
│   └── MagicianClass.cs       # Magician character abilities, fireball casting, auto-aim
├── Combat/
│   ├── AiBotController.cs     # AI opponent behavior
│   ├── AimingSystem.cs        # Player targeting system
│   ├── AttackRangeDetector.cs # Combat range detection
│   ├── BotSpawner.cs          # AI bot spawning system
│   ├── CameraController.cs    # Third-person camera
│   ├── GameInitializer.cs     # Automatic scene setup
│   ├── GameManager.cs         # Game state, timer, scoring
│   ├── HealthSystem.cs        # Player health, damage, respawn
│   ├── LocalGameSpawner.cs    # Player and bot spawning
│   ├── PlayerCombat.cs        # Combat input handling, ability usage
│   ├── PlayerController.cs    # Local player movement and input
│   ├── PlayerScore.cs         # Score tracking
│   ├── PlayerTotemInteraction.cs  # Totem pickup/drop mechanics
│   ├── SpawnPointManager.cs   # Player spawn management
│   ├── TotemController.cs     # Totem game objective logic
│   └── Projectiles/
│       ├── FireBallProjectile.cs   # Fireball behavior
│       ├── IceSpikeProjectile.cs   # Ice ability
│       ├── LightningProjectile.cs  # Lightning ability
│       └── MeteorProjectile.cs     # Meteor ability
├── Network/
│   ├── GameSettings.cs        # Game configuration
│   └── GameStateManager.cs    # Game state management
└── UI/
    ├── CountdownDisplay.cs    # Pre-game countdown UI
    ├── GameOverMenu.cs        # End game screen
    ├── LeaderboardManager.cs  # Scoreboard with Tab toggle
    ├── LobbyManager.cs        # Pre-game settings UI
    ├── LocalScoreDisplay.cs   # Local player score UI
    ├── MainMenu.cs            # Main menu
    ├── PauseMenu.cs           # In-game pause menu
    └── TotemPickUpUI.cs       # Totem interaction progress UI
```

---

## Game Mechanics

### Core Gameplay Loop
1. Player starts game from MainMenu
2. Configure game settings in Lobby (optional)
3. Countdown before match start (3, 2, 1, GO!)
4. Player spawns at designated spawn point
5. AI bots spawn at other spawn points
6. Player uses fireball spells to attack bots
7. Totem spawns in the arena
8. Player and bots compete to pick up and hold the totem
9. Holding the totem earns points over time
10. Game ends when timer reaches zero
11. Final scores displayed

### Combat System
- **Primary Attack**: Fireball projectile with auto-aim
- **Auto-Aim**: Targets nearest enemy within range and angle (45° cone, 15m range)
- **Projectile Physics**: Fireballs use Rigidbody physics
- **Damage**: Direct damage application (20 default)
- **Death**: Physics-based death animation, respawn after 3 seconds

### Character Class: Magician
Located in `MagicianClass.cs`:
- Fireball casting with cooldown (1 second default)
- Animation integration with attack delay
- Auto-aim targeting system with obstacle checking
- Static player tracker for all magician instances
- Future ability placeholders (Ability1, Ability2, Ultimate)

### AI Bots
Located in `AiBotController.cs`:
- Automatically find and chase nearest player
- Attack when within range
- Can pick up and carry totem
- Respawn when killed

### Totem System
Located in `TotemController.cs`:
- Pickup by players/bots within range (2m default)
- Carried state with visual feedback (smooth follow)
- Score accumulation while carried
- Drop on death or key press

---

## Input System

### Player Controls (Keyboard & Mouse)
| Action | Key |
|--------|-----|
| Move | WASD / Arrow Keys |
| Look | Mouse |
| Attack | Left Mouse Button |
| Ability 1 | Q |
| Ability 2 | R |
| Ultimate | F |
| Pickup/Drop Totem | E |
| Drop Totem | G |
| Jump | Space |
| Sprint | Left Shift |
| Show Leaderboard | Tab |
| Pause | Escape |

---

## Key Components

### GameManager
- Singleton pattern
- Manages game timer
- Tracks total score
- Handles game start/end
- Applies game settings

### LocalGameSpawner
- Spawns player at game start
- Coordinates with BotSpawner
- Shows countdown display
- Handles game restart

### BotSpawner
- Spawns AI bots
- Configurable bot count
- Assigns random names to bots
- Tracks spawned bots

### PlayerController
- Local player input handling
- CharacterController movement
- Animation state management
- Camera setup
- Totem interaction input

### HealthSystem
- Health tracking (not networked)
- TakeDamage method
- Death animation
- Respawn after delay

### PlayerScore
- Score tracking with events
- Bot flag for differentiation
- Leaderboard integration

---

## Build Configuration

### Target Platforms
- **Standalone** (Windows, macOS, Linux) - Primary
- **Android** - Mobile support configured
- **iOS** - Configured but not primary

### Platform Settings
| Setting | Value |
|---------|-------|
| Default Screen | 1920x1080 |
| Fullscreen Mode | Fullscreen Window |
| Color Space | Linear |
| Rendering Path | Forward |

### Android Settings
| Setting | Value |
|---------|-------|
| Min SDK | 26 (Android 8.0) |
| Target SDK | Automatic |
| Architecture | ARM64 |
| Scripting Backend | IL2CPP |

---

## Development Conventions

### Code Style
- **Comments**: Primarily in Russian language
- **Naming**: PascalCase for public members, camelCase for private
- **Access Modifiers**: Explicit `private`/`public` modifiers used

### Important Tags
Defined in `TagManager.asset`:
- `Ground` - Walkable surfaces
- `Projectile` - Projectile objects
- `SpawnPoint` - Player spawn locations
- `LocalScore` - Local player score UI
- `Player` - Player character
- `Enemy` - AI bots

### Physics Layers
- Layer 0: Default
- Layer 3: Ground
- Layer 4: Water
- Layer 5: UI
- Layer 6: Player
- Layer 7: Enemy
- Layer 8: Projectile
- Layer 9: LocalScore

---

## Testing & Debugging

### Local Testing Setup
1. Open project in Unity Editor
2. Open MainMenu scene
3. Press Play to start
4. Click "Play" to start game
5. Configure settings if needed
6. Game will spawn player and bots

### Key Debug Features
- Extensive `Debug.Log()` statements
- Player/bot spawn logging
- Fireball trajectory logging

---

## Key Configuration Files

| Purpose | Path |
|---------|------|
| Package Dependencies | `Packages/manifest.json` |
| Project Settings | `ProjectSettings/ProjectSettings.asset` |
| Input Actions | `Assets/InputSystem_Actions.inputactions` |
| Tag Manager | `ProjectSettings/TagManager.asset` |
| URP Settings | `Assets/Settings/` |

---

## File Locations Quick Reference

| Purpose | Path |
|---------|------|
| Main Menu Scene | `Assets/Scenes/MainMenu.unity` |
| Game Scene | `Assets/Scenes/SampleScene.unity` |
| Game Logic | `Assets/Scripts/Combat/GameManager.cs` |
| Player Prefab | `Assets/Prefabs/Players/Magician/Magician.prefab` |
| Totem Prefab | `Assets/Prefabs/Totem/Totem.prefab` |
| Fireball Prefab | `Assets/Prefabs/Projectiles/Fireball.prefab` |
| Setup Guide | `SINGLE_PLAYER_SETUP.md` |

---

## Migration Notes

### From Multiplayer to Single Player
The project was converted from FishNet multiplayer to single-player with AI bots:

| Original | New |
|----------|-----|
| NetworkBehaviour | MonoBehaviour |
| SyncVar | Regular fields with events |
| [ServerRpc] | Direct method calls |
| NetworkObject | Regular GameObject |
| MyNetworkManager | LocalGameSpawner + BotSpawner |
| Player connections | AI bots |

### Removed Components
- FishNet package
- NetworkManager
- NetworkObject components
- All RPC methods
- Server/client authority checks

---

## License & Attribution

- **UnityGLTF**: Khronos Group glTF implementation
- **TextMeshPro**: Unity Technologies
- **Magician Model**: Third-party asset (check specific license in Models folder)

---

*This documentation was generated for AI coding agents working on the Totem Clash project. For human-readable documentation, see project README files or contact the development team.*
