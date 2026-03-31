# StreamAvatar - Animated 2D Avatar for Streaming

## Description
StreamAvatar is a tool for creating animated 2D avatars for streaming via OBS Studio. The application supports multi-layer avatars, skeletal animation, audio effects, and OBS integration through Browser Source.

## Features

### Graphics Engine & Animation
- **Multi-layer avatars**: Support for body, eyes, mouth layers
- **Skeletal animation**: Basic bone system for smooth deformation and movement
- **Shake effect**: Programmatic shake when microphone volume reaches threshold
- **Eye animation**: Automatic slow idle movement within radius + random blinking
- **Mouth animation**: Dynamic sprite switching based on audio amplitude analysis

### Audio Module
- Microphone capture with real-time amplitude analysis
- Audio effects: Reverb, Pitch Shift
- Virtual microphone output (for sending processed audio to OBS)
- Automatic lip-sync synchronization with audio intensity

### User Interface (UI)
- **Technology**: WinForms with custom SkiaSharp rendering
- **Adaptive**: Resizable interface (minimum 800x600)
- **Design**: Modern dark "gamer" style, intuitive for beginners
- **Localization**: RU/EN support via external .ini files
- **Themes**: Custom color schemes via JSON files

### OBS Integration
- Built-in local HTTP server for Browser Source URL generation
- Transparent background PNG streaming
- Preview page with auto-refresh (~60fps)

### Save System
- Import/Export avatar presets in custom `.avatar` format
- Includes animation settings and image paths

## Requirements
- .NET 8.0 SDK
- Windows 10/11

## Build

Run `build.bat` for automatic compilation:

```batch
build.bat
```

Or manually via CLI:

```batch
dotnet publish -c Release -r win-x64 --self-contained false
```

## Project Structure

```
StreamAvatar/
├── Core/              # Core classes and utilities (Models, Localization, Theme)
├── Audio/             # Audio engine (capture, processing, virtual mic)
├── Rendering/         # Avatar rendering (SkiaSharp, AnimationController)
├── WebServer/         # HTTP server for OBS Browser Source
├── UI/                # User interface (WinForms, MainForm)
├── Assets/
│   ├── Loc/           # Localization files (RU/EN)
│   └── Themes/        # Color themes (JSON)
├── StreamAvatar.sln
├── StreamAvatar.csproj
├── app.manifest
├── build.bat
└── README.md
```

## Usage

1. Launch the application
2. Select or import an avatar preset
3. Configure audio input (microphone selection)
4. Adjust animation settings (mouth sensitivity, shake threshold)
5. Enable audio effects if desired (Reverb, Pitch Shift)
6. Copy the OBS Browser Source URL from the interface
7. Add to OBS as "Browser Source" with the URL

### OBS Setup
- Add new "Browser" source
- Paste URL: `http://localhost:8080/avatar`
- Set Width: 512, Height: 512 (or your preferred size)
- Check "Shutdown source when tab becomes inactive"
- Enable "Use custom FPS" if needed

## Localization

Localization files are in `Assets/Loc/`. Supported formats: `.ini`
- `ru.ini` — Russian
- `en.ini` — English

Add new languages by creating additional `.ini` files.

## Themes

Color schemes are stored in `Assets/Themes/` as JSON files.

Included themes:
- `dark_gamer.json` — Default dark theme
- `cyberpunk.json` — Neon cyberpunk style

Create custom themes by copying and modifying existing JSON files.

## Architecture Overview

### Core Classes
- **AvatarPreset**: Main container for avatar configuration (layers, bones, settings)
- **AvatarLayer**: Individual sprite layer with properties (image, opacity, bone attachment)
- **Bone**: Skeletal bone with position, rotation, parent-child hierarchy
- **AnimationSettings**: Configuration for blink intervals, shake threshold, mouth sensitivity
- **AudioSettings**: Audio processing configuration (reverb, pitch shift, volume)

### Audio Module
- **AudioEngine**: Handles microphone capture, amplitude analysis
- **AudioProcessor**: Applies effects (reverb, pitch shift) to audio stream

### Rendering Module
- **AvatarRenderer**: SkiaSharp-based renderer for layers and bones
- **AnimationController**: Manages idle animations, blinking, eye movement, shake effects

### Web Server
- **ObsWebServer**: Embedded HTTP server serving avatar as transparent PNG

### UI Module
- **MainForm**: Main application window with preview canvas and controls
- **LocalizationManager**: Runtime language switching
- **ThemeManager**: Dynamic theme application

## File Formats

### Avatar Preset (.avatar)
JSON-based format containing:
- Layer definitions with image paths
- Bone hierarchy
- Animation settings
- Audio settings

### Localization (.ini)
Simple key=value format:
```ini
# Comment
key=Value text
```

### Theme (.json)
JSON color scheme:
```json
{
  "Name": "Theme Name",
  "BackgroundPrimary": "#1E1E1E",
  "AccentColor": "#0078D4"
}
```

## License
MIT
