-- Sound effects (assets/sfx/*.wav, made by tools/gen_sounds.py). The rules never play sound: watch() looks at the
-- game state every frame and plays what changed, so a new sound only needs a line here.
local Sound = {volume = {master = .8, sfx = .8, music = .4}, sources = {}, last = {}}

local NAMES = {"click", "flip", "land_heads", "land_tails", "score", "penalty", "combo", "discard", "buy", "shop",
  "levelup", "win", "lose"}

function Sound.load()
  for _, name in ipairs(NAMES) do
    local ok, source = pcall(love.audio.newSource, "assets/sfx/" .. name .. ".wav", "static")
    if ok then Sound.sources[name] = source end -- no audio device or file: the game just stays quiet
  end
  local ok, music = pcall(love.audio.newSource, "assets/music/theme.wav", "stream")
  if ok then
    Sound.music = music
    music:setLooping(true)
    music:play()
  end
end

-- Volumes (0-100 in the profile options): master scales everything, then sfx or music.
function Sound.apply(options)
  Sound.volume.master = (options.volume_master or 80) / 100
  Sound.volume.sfx = (options.volume_sfx or 80) / 100
  Sound.volume.music = (options.volume_music or 40) / 100
  if Sound.music then Sound.music:setVolume(Sound.volume.master * Sound.volume.music * .6) end
end

function Sound.play(name, pitch, volume)
  local source = Sound.sources[name]
  if not source then return end
  source = source:clone() -- clones overlap, so quick events do not cut each other off
  source:setPitch(pitch or 1)
  source:setVolume((volume or .7) * Sound.volume.master * Sound.volume.sfx)
  source:play()
end

-- Compare this frame with the last one and play the sounds for what changed.
function Sound.watch(ui)
  local last = Sound.last
  local game = ui.game
  local animation = ui.flip_animation
  if animation and not last.animation then Sound.play("flip") end
  if last.animation and not animation then
    Sound.play(last.animation.outcome == "Heads" and "land_heads" or "land_tails")
  end
  last.animation = animation
  if not game then last.phase, last.result = nil, nil return end

  local result = game.last_result
  if result and result ~= last.result then -- a coin just resolved
    if (result.penalty or 0) > 0 then Sound.play("penalty")
    elseif (result.gained or 0) > 0 then Sound.play("score", 1 + math.min(result.gained, 24) / 48) end
    if result.combo and result.combo.len >= 2 then Sound.play("combo", 1 + .07 * math.min(result.combo.len - 2, 8)) end
  end
  last.result = result

  local e = game.encounter
  if e and game.phase == "ENCOUNTER" then
    if e.cleared and last.cleared == false then Sound.play("levelup") end
    if last.discards and e.discards > last.discards and not game.mulligan then Sound.play("discard") end
    last.cleared, last.discards = e.cleared, e.discards
  else
    last.cleared, last.discards = nil, nil
  end

  if game.phase ~= last.phase then
    if game.phase == "SHOP" then Sound.play("shop")
    elseif game.phase == "VICTORY" then Sound.play("win")
    elseif game.phase == "GAME_OVER" then Sound.play("lose") end
  end
  if game.phase == "SHOP" and last.phase == "SHOP" and last.gold and game.player.gold < last.gold then Sound.play("buy") end
  last.phase, last.gold = game.phase, game.player.gold
end

return Sound
