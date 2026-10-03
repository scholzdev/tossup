-- Game version. Packaged builds embed src/build_id.lua; source runs use "dev".
local ok, id = pcall(require, "src.build_id")
return {number = "0.1.0", build = ok and id or "dev"}
