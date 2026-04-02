-- example_audio_trigger.lua
-- Shows how to trigger AudioManager playback from a Lua script
-- (used when AUDIOMANAGER_MLF + AUDIOMANAGER_CSM are both defined and
--  MoonSharp is available via MapLoaderFramework).

-- Play background music by track id
playMusic("town_theme")

-- Play a SFX
playSfx("btn_click")

-- Set music volume to 70%
setVolume("music", 0.7)

-- Stop ambient layers
stopAmbient()
