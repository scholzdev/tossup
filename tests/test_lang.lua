-- Every coin, chip, prize and character has a German name and description; lookups fall back to English.
package.path = "./?.lua;" .. package.path
local Lang = require("src.lang")
local de = require("locales.de")
local Game = require("src.game")

local function check(set, defs)
  for id in pairs(defs) do
    local entry = de[set] and de[set][id]
    assert(entry and entry.name, "missing German name: " .. set .. "." .. id)
    assert(set == "characters" or entry.description, "missing German description: " .. set .. "." .. id)
  end
end
check("coins", Game.catalog())
check("items", Game.item_catalog())
check("relics", Game.relics())
check("characters", Game.characters())
check("modifiers", Game.modifiers())

Lang.set("de")
assert(Lang.t("BACK") == "ZURÜCK")
assert(Lang.t("LEVEL %d / 4", 2) == "LEVEL 2 / 4")
assert(Lang.t("not in the table") == "not in the table")
assert(("Münze"):upper() == "MÜNZE")
assert(Lang.wrap("coins", Game.catalog()).sword.name == "Schwert")
assert(Lang.wrap("coins", Game.catalog()).sword.probability == Game.catalog().sword.probability, "other fields fall through")
Lang.set("en")
assert(Lang.t("BACK") == "BACK")
assert(Lang.wrap("coins", Game.catalog()).sword.name == Game.catalog().sword.name)
print("lang tests passed")
