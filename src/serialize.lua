-- Plain-data serializer: tables, strings, numbers and booleans become Lua-like data text.
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

-- Parse only literals and table constructors. Save files are input, never programs.
function Serialize.decode(text)
  if type(text) ~= "string" or #text > 2 * 1024 * 1024 then return nil end
  local pos, nodes = 1, 0
  local function skip()
    while pos <= #text and text:sub(pos, pos):match("%s") do pos = pos + 1 end
  end
  local function string_value()
    local quote = text:sub(pos, pos)
    pos = pos + 1
    local out = {}
    while pos <= #text do
      local c = text:sub(pos, pos)
      pos = pos + 1
      if c == quote then return table.concat(out) end
      if c == "\\" then
        local e = text:sub(pos, pos)
        pos = pos + 1
        local escapes = {a="\a", b="\b", f="\f", n="\n", r="\r", t="\t", v="\v", ["\\"]="\\", ["\""]="\"", ["'"]="'"}
        if e:match("%d") then
          local more = text:sub(pos):match("^%d%d?") or ""
          pos = pos + #more
          out[#out + 1] = string.char(tonumber(e .. more) % 256)
        elseif escapes[e] then out[#out + 1] = escapes[e]
        elseif e == "\n" then out[#out + 1] = "\n"
        else return nil end
      else
        if c == "\n" or c == "\r" then return nil end
        out[#out + 1] = c
      end
    end
    return nil
  end
  local parse_value
  local function table_value(depth)
    if depth > 64 then return nil, false end
    pos = pos + 1
    local out, index = {}, 1
    skip()
    while text:sub(pos, pos) ~= "}" do
      nodes = nodes + 1
      if nodes > 200000 then return nil, false end
      local key, value, explicit
      if text:sub(pos, pos) == "[" then
        pos = pos + 1; skip()
        key = parse_value(depth + 1); skip()
        if key == nil or text:sub(pos, pos) ~= "]" then return nil, false end
        pos = pos + 1; skip()
        if text:sub(pos, pos) ~= "=" then return nil, false end
        pos = pos + 1; explicit = true
      else
        local ident = text:sub(pos):match("^([%a_][%w_]*)")
        if ident then
          local after = pos + #ident
          local saved = pos
          pos = after; skip()
          if text:sub(pos, pos) == "=" then
            key = ident; pos = pos + 1; explicit = true
          else pos = saved end
        end
      end
      if not explicit then key = index; index = index + 1 end
      skip()
      value = parse_value(depth + 1)
      if value == nil then
        -- nil entries are valid Lua data syntax and simply do not exist in a table.
      elseif key ~= nil then out[key] = value end
      skip()
      local sep = text:sub(pos, pos)
      if sep == "," or sep == ";" then pos = pos + 1; skip()
      elseif sep ~= "}" then return nil, false end
    end
    pos = pos + 1
    return out, true
  end
  parse_value = function(depth)
    skip()
    local c = text:sub(pos, pos)
    if c == "{" then return table_value(depth) end
    if c == "\"" or c == "'" then return string_value() end
    local word = text:sub(pos):match("^([%a_][%w_]*)")
    if word then
      pos = pos + #word
      if word == "true" then return true end
      if word == "false" then return false end
      return nil -- legacy saves may contain unknown identifiers; treat them as nil
    end
    local number = text:sub(pos):match("^[+-]?%d+%.?%d*[eE]?[+-]?%d*")
    if number and number:match("%d") then
      local value = tonumber(number)
      if not value or value ~= value or math.abs(value) == math.huge then return nil end
      pos = pos + #number
      return value
    end
    return nil
  end
  skip()
  if text:sub(pos, pos + 5) == "return" then
    pos = pos + 6
    if text:sub(pos, pos):match("[%w_]") then return nil end
  end
  local value = parse_value(0)
  if value == nil then return nil end
  skip()
  if pos <= #text then return nil end
  return value
end

return Serialize
