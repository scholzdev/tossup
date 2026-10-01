local function hex(value)
  return {tonumber(value:sub(1, 2), 16) / 255,
    tonumber(value:sub(3, 4), 16) / 255,
    tonumber(value:sub(5, 6), 16) / 255}
end
local C = {
  felt = hex("335c59"), felt_dark = hex("274b49"), ink = hex("202b32"),
  panel = hex("374244"), panel_light = hex("4f6367"), slot = hex("2c3537"),
  face = hex("eef2f5"), muted = hex("b3c4c4"), blue = hex("009dff"),
  red = hex("fe5f55"), gold = hex("f3b958"), orange = hex("fda200"),
  green = hex("4bc292"), white = {1, 1, 1}, black = {0, 0, 0},
  -- the teal "screen" look shared by the shop and the round view
  screen = hex("17454d"), panel_dk = hex("0f333b"), card = hex("1d4f58"), line = hex("2f6670"),
  slot_dk = hex("0b2a30"), marked = hex("2c6a74"),
}

return C
