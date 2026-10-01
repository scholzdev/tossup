-- Plain-data serializer: tables, strings, numbers and booleans become Lua source that `load` reads back (used for the saved run).
-- Functions and userdata are skipped; keys are sorted so the output is deterministic.
local Serialize = {}

local function is_array(t)
  local n = 0
  for _ in pairs(t) do n = n + 1 end
  for i = 1, n do if t[i] == nil then return false end end
  return true, n
end

local function key_text(k) return type(k) == "string" and string.format("[%q]", k) or "[" .. tostring(k) .. "]" end

local function write(value, out)
  local kind = type(value)
  if kind == "number" then
    out[#out + 1] = (value == math.floor(value) and math.abs(value) < 1e15) and string.format("%d", value) or string.format("%.17g", value)
  elseif kind == "string" then out[#out + 1] = string.format("%q", value)
  elseif kind == "boolean" then out[#out + 1] = tostring(value)
  elseif kind == "table" then
    local array, n = is_array(value)
    out[#out + 1] = "{"
    if array then
      for i = 1, n do
        local item = value[i]
        if type(item) == "function" or type(item) == "userdata" then item = false end
        write(item, out)
        out[#out + 1] = ","
      end
    else
      local keys = {}
      for k, v in pairs(value) do
        if (type(k) == "string" or type(k) == "number") and type(v) ~= "function" and type(v) ~= "userdata" then keys[#keys + 1] = k end
      end
      table.sort(keys, function(a, b)
        if type(a) == type(b) then return a < b end
        return type(a) == "number"
      end)
      for _, k in ipairs(keys) do
        out[#out + 1] = key_text(k) .. "="
        write(value[k], out)
        out[#out + 1] = ","
      end
    end
    out[#out + 1] = "}"
  else
    out[#out + 1] = "false"
  end
end

function Serialize.encode(value)
  local out = {"return "}
  write(value, out)
  return table.concat(out)
end

-- Returns the value, or nil when the text is not valid data.
function Serialize.decode(text)
  local chunk = text and load(text, "data", "t", {})
  if not chunk then return nil end
  local ok, value = pcall(chunk)
  if ok then return value end
end

return Serialize
