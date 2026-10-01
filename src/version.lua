-- Game version. `build` is the git commit the build scripts stamp into src/build_id.lua; "dev" when run from the source folder.
local ok, id = pcall(require, "src.build_id")
return {number = "0.2.0", build = ok and id or "dev"}
