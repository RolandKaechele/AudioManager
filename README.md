# AudioManager

A modular, data-driven audio framework for Unity.  
Handles background music with crossfading, layered ambient sounds, pooled SFX, and per-channel volume control — all configured from JSON.  
Optionally integrates with [MapLoaderFramework](https://github.com/RolandKaechele/MapLoaderFramework) for automatic map/chapter audio and with [CutsceneManager](https://github.com/RolandKaechele/CutsceneManager) for cutscene audio routing.


## Features

- **Crossfading music** — smooth transition between background tracks using two `AudioSource` components
- **Layered ambients** — play any number of looping ambient clips simultaneously, each with fade in/out
- **SFX pool** — reusable pool of `AudioSource` components for one-shot sounds; world-position playback supported
- **JSON-authored tracks and playlists** — define `AudioTrackData` and `AudioPlaylistData` in plain JSON; no code required
- **Per-channel volume** — independent volume and mute for Music, Ambient, SFX, and Voice channels
- **Persistent settings** — volume values survive sessions via PlayerPrefs
- **MapLoaderFramework integration** — `MapLoaderAudioBridge` auto-crossfades to the correct music/ambients when chapters change (activated via `AUDIOMANAGER_MLF`)
- **CutsceneManager integration** — `CutsceneAudioBridge` routes cutscene `PlayAudio` / `StopAudio` steps through AudioManager channels (activated via `AUDIOMANAGER_CSM`)
- **DialogueManager integration** — `DialogueAudioBridge` routes per-node voice lines and sound cues from DialogueManager through AudioManager's channel system (activated via `AUDIOMANAGER_DM`)
- **MiniGameManager integration** — `MiniGameAudioBridge` plays configured music/SFX tracks when a mini-game starts, completes, or is aborted (activated via `AUDIOMANAGER_MGM`)
- **Custom Inspector** — runtime volume sliders, test-play buttons, and a reload button directly in the Unity Inspector
- **Odin Inspector integration** — `SerializedMonoBehaviour` base for full Inspector serialization of complex types; runtime-display fields marked `[ReadOnly]` in Play Mode (activated via `ODIN_INSPECTOR`)


## Installation

### Option A — Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL…**
3. Enter:

   ```
   https://github.com/RolandKaechele/AudioManager.git
   ```

### Option B — Clone into Assets

```bash
git clone https://github.com/RolandKaechele/AudioManager.git Assets/AudioManager
```

### Option C — Manual copy

Copy the `AudioManager/` folder into your project's `Assets/` directory.


## Folder Structure

After installation the post-install script creates the following working directories:

```
Assets/
  Audio/
    Music/          ← background music .wav / .ogg clips
    Ambient/        ← ambient sound clips
    SFX/            ← sound effect clips
    Voice/          ← voiced dialogue clips
  Resources/
    Audio/
      Tracks/       ← track JSON files (loaded via Resources.Load)
      Playlists/    ← playlist JSON files
  Scripts/          ← Lua scripts (used with MapLoaderFramework integration)
```


## Quick Start

### 1. Add AudioManager to your scene

Create a persistent GameObject, then add:

| Component | Purpose |
| --------- | ------- |
| `AudioManager` | Main orchestrator (required) |
| `MusicController` | Crossfading BGM (auto-added if absent) |
| `AmbientController` | Ambient layers (auto-added if absent) |
| `SfxController` | SFX pool (auto-added if absent) |

All sub-controllers are auto-resolved on `Awake`; you only need to add them manually to override settings.

### 2. Create a track JSON

Place a `.json` file in `Resources/Audio/Tracks/`:

```json
{
  "id": "town_theme",
  "label": "Town Square Theme",
  "resource": "Audio/Music/town_theme",
  "volume": 0.85,
  "loop": true,
  "channel": 0,
  "fadeInDuration": 1.5,
  "fadeOutDuration": 1.0
}
```

### 3. Play from code

```csharp
AudioManager.Runtime.AudioManager audio = FindFirstObjectByType<AudioManager.Runtime.AudioManager>();

// Play music by registered track id
audio.PlayMusic("town_theme");

// Or directly by resource path
audio.PlayMusic("Audio/Music/town_theme");

// Play SFX
audio.PlaySfx("Audio/SFX/explosion");

// Play ambient layers
audio.PlayAmbient(new[] { "Audio/Ambient/birds", "Audio/Ambient/wind" });

// Volume control
audio.SetVolume(AudioChannelType.Music, 0.7f);
audio.SetMasterVolume(0.9f);
```


## Track JSON Format

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique track identifier |
| `resource` | string | `Resources`-relative path to `AudioClip` (without extension) |
| `volume` | float | Volume multiplier (0–1, default 1) |
| `pitch` | float | Pitch multiplier (1 = normal) |
| `loop` | bool | Whether to loop the clip |
| `channel` | int | Channel type: 0=Music, 1=Ambient, 2=SFX, 3=Voice |
| `fadeInDuration` | float | Fade-in seconds (0 = instant) |
| `fadeOutDuration` | float | Fade-out seconds (0 = instant) |
| `label` | string | Human-readable label (Inspector/Editor only) |

## Playlist JSON Format

```json
{
  "id": "chapter01_playlist",
  "label": "Chapter 1 Music",
  "trackIds": ["town_theme", "danger_theme", "victory_theme"],
  "shuffle": false,
  "loop": true,
  "crossfadeDuration": 1.5
}
```

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique playlist identifier |
| `trackIds` | array | Ordered list of track ids |
| `shuffle` | bool | Randomise order on start |
| `loop` | bool | Restart after last track |
| `crossfadeDuration` | float | Seconds between tracks |


## MapLoaderFramework Integration

AudioManager can automatically respond to map and chapter changes in **MapLoaderFramework** without a compile-time dependency.

### Enable

1. **Edit → Project Settings → Player → Scripting Define Symbols** → add `AUDIOMANAGER_MLF`
2. Attach `MapLoaderAudioBridge` to the same (or any) GameObject in your scene.

On every map or chapter load, `MapLoaderAudioBridge` reads the new map's `audio.backgroundMusic` and `audio.ambientSounds` fields and calls `AudioManager.PlayMusic()` / `AudioManager.PlayAmbient()` automatically. The bridge subscribes to `MapLoaderFramework.OnMapLoaded`, which fires on all load paths — `LoadChapter()`, direct `LoadMap()` calls, and warp-event navigation.

```csharp
// Manual call — useful when you load a map directly
var bridge = FindFirstObjectByType<MapLoaderAudioBridge>();
bridge.PlayAudioForMap(mapData);   // mapData: MapLoaderFramework.Runtime.MapData
```

The `audio` block in your map JSON:

```json
{
  "id": "forest_path",
  "audio": {
    "backgroundMusic": "Audio/Music/forest_theme",
    "ambientSounds": ["Audio/Ambient/wind", "Audio/Ambient/birds"]
  }
}
```


## CutsceneManager Integration

AudioManager can take over `PlayAudio` / `StopAudio` steps in **CutsceneManager** sequences, routing them through the proper mixer channel with volume scaling.

### Enable

1. Add `AUDIOMANAGER_CSM` to **Scripting Define Symbols**
2. Attach `CutsceneAudioBridge` to any GameObject in your scene.

`CutsceneAudioBridge.Awake()` hooks `CutsceneManager.PlayAudioCallback` and `CutsceneManager.StopAudioCallback`. Without the bridge, CutsceneManager falls back to its built-in raw `AudioSource`.

Configure which channel cutscene audio plays through in the `CutsceneAudioBridge` Inspector:

| Setting | Default | Description |
| ------- | ------- | ----------- |
| Cutscene Audio Channel | Music | Channel to route PlayAudio steps to |
| Use Crossfade For Music | true | Crossfade instead of instant switch |
| Crossfade Duration | 0.8 s | Crossfade length in seconds |


## DialogueManager Integration

AudioManager can route per-node audio from **DialogueManager** through its channel system, so voice lines and sound cues respect the same per-channel volume controls as music and ambients.

### Enable

1. Add `AUDIOMANAGER_DM` to **Scripting Define Symbols**
2. Attach `DialogueAudioBridge` to any GameObject in your scene.

`DialogueAudioBridge.Awake()` hooks `DialogueManager.PlayAudioCallback`. Each time a dialogue node with an `audioResource` displays, the clip is dispatched to the AudioManager channel configured in the Inspector (default: `Voice`).

| Setting | Default | Description |
| ------- | ------- | ----------- |
| Dialogue Audio Channel | `Voice` | `AudioChannelType` to route node audio through |


## Runtime API

### `AudioManager`

| Member | Description |
| ------ | ----------- |
| `PlayMusic(id, fadeDuration)` | Start music by track id or resource path |
| `StopMusic(fadeDuration)` | Fade out and stop music |
| `PlayAmbient(ids[], fadeDuration)` | Start ambient layers |
| `StopAmbient(fadeDuration)` | Stop all ambient layers |
| `PlaySfx(id, volume, pitch)` | Play a one-shot SFX |
| `PlaySfxAt(id, position, volume)` | Play a one-shot SFX at world position |
| `SetVolume(channel, volume)` | Set channel volume (0–1) and persist |
| `SetMasterVolume(volume)` | Set master volume and persist |
| `SetMute(channel, muted)` | Mute/unmute a channel |
| `GetVolume(channel)` | Current effective volume for a channel |
| `LoadAllTracks()` | Reload all track/playlist JSON |
| `GetTrackIds()` | List of all registered track ids |
| `GetTrack(id)` | Return `AudioTrackData` by id |
| `GetPlaylist(id)` | Return `AudioPlaylistData` by id |
| `OnMusicStarted` | `event Action<string>` — fires when music starts |
| `OnMusicStopped` | `event Action` — fires when music stops |
| `OnTrackChanged` | `event Action<string>` — fires on playlist track change |
| `PlayMusicOverride` | `Action<string, bool, float>` delegate — intercept music playback |
| `StopMusicOverride` | `Action<float>` delegate — intercept music stop |

### `MusicController`

| Member | Description |
| ------ | ----------- |
| `Play(clip, loop, volume)` | Instant play (no crossfade) |
| `CrossfadeTo(clip, loop, volume, duration)` | Smooth crossfade to new clip |
| `Stop(fadeDuration)` | Fade out and stop |
| `StopImmediate()` | Stop without fade |
| `IsPlaying` | True while a track is playing |
| `CurrentClip` | Currently playing `AudioClip` |

### `AmbientController`

| Member | Description |
| ------ | ----------- |
| `Play(clips[], fadeDuration)` | Start ambient layers with fade in |
| `Stop(fadeDuration)` | Fade out all layers |
| `StopImmediate()` | Stop all layers without fade |
| `IsPlaying` | True while layers are active |

### `SfxController`

| Member | Description |
| ------ | ----------- |
| `Play(clip, volume, pitch)` | Play one-shot SFX from pool |
| `PlayAt(clip, position, volume)` | Play one-shot SFX at world position |
| `StopAll()` | Stop all active pool sources |

### `MapLoaderAudioBridge` *(requires `AUDIOMANAGER_MLF`)*

| Member | Description |
| ------ | ----------- |
| `PlayAudioForMap(mapData)` | Start music/ambients from a `MapData` object |
| `StopAll()` | Stop music and ambients |

### `CutsceneAudioBridge` *(requires `AUDIOMANAGER_CSM`)*

Hooks `CutsceneManager.PlayAudioCallback` and `CutsceneManager.StopAudioCallback` automatically on `Awake`.

### `DialogueAudioBridge` *(requires `AUDIOMANAGER_DM`)*

| Member | Description |
| ------ | ----------- |
| `dialogueAudioChannel` | Inspector — `AudioChannelType` to route per-node audio through (default: `Voice`) |

Hooks `DialogueManager.PlayAudioCallback` automatically on `Awake`.

### `MiniGameAudioBridge` *(requires `AUDIOMANAGER_MGM`)*

| Inspector Field | Description |
| --------------- | ----------- |
| `startMusicId` | Music track id to play when a mini-game starts |
| `startSfxId` | SFX id to play when a mini-game starts |
| `completeMusicId` | Music track id to play on completion |
| `completeSfxId` | SFX id to play on completion |
| `abortSfxId` | SFX id to play when a mini-game is aborted |
| `musicFadeDuration` | Crossfade duration in seconds (default: 0.5) |


## Channel Type Reference

| Value | Name | Used for |
| ----- | ---- | ------ |
| 0 | `Music` | Background music tracks |
| 1 | `Ambient` | Looping environmental sounds |
| 2 | `Sfx` | One-shot sound effects |
| 3 | `Voice` | Voiced dialogue |


## Examples

The `Examples/` folder contains ready-to-use files:

| File | Description |
| ---- | ----------- |
| `Audio/Tracks/example_bgm_track.json` | Background music track definition |
| `Audio/Tracks/example_sfx_track.json` | SFX track definition |
| `Audio/Playlists/example_playlist.json` | Three-track looping playlist |
| `Scripts/example_audio_trigger.lua` | Lua script triggering audio calls |


## Dependencies

| Dependency | Required | Notes |
| ---------- | -------- | ----- |
| Unity 2022.3+ | ✓ | |
| MapLoaderFramework | optional | Required when `AUDIOMANAGER_MLF` is defined |
| CutsceneManager | optional | Required when `AUDIOMANAGER_CSM` is defined |
| DialogueManager | optional | Required when `AUDIOMANAGER_DM` is defined |
| MiniGameManager | optional | Required when `AUDIOMANAGER_MGM` is defined |
| MoonSharp | optional | Required for Lua-triggered audio (included via MapLoaderFramework) |
| Odin Inspector | optional | Required when `ODIN_INSPECTOR` is defined |


## License

MIT — see [LICENSE](LICENSE).
