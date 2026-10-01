-- Mutable event bus. An emitted event is a plain table passed through every listener in
-- registration order; listeners may mutate it. Gameplay reads the final result after emit.
local Signal = {}

local listeners = {}

function Signal.on(name, callback)
  local handle = {event = name, callback = callback, active = true}
  listeners[name] = listeners[name] or {}
  table.insert(listeners[name], handle)
  return handle
end

function Signal.off(handle)
  if not handle.active then return end
  handle.active = false
  local list = listeners[handle.event]
  for i = #list, 1, -1 do
    if list[i] == handle then table.remove(list, i) break end
  end
end

function Signal.emit(name, event)
  local list = listeners[name]
  if not list then return event end
  local snapshot = {} -- listeners added/removed mid-emit only affect later emits
  for i, handle in ipairs(list) do snapshot[i] = handle end
  for _, handle in ipairs(snapshot) do
    if handle.active then handle.callback(event) end
  end
  return event
end

function Signal.clear_all() listeners = {} end

return Signal
