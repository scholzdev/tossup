local RNG = {}
local modulus = 2147483647

function RNG.seed(value)
  value = tonumber(value)
  if not value or value ~= value or value == math.huge or value == -math.huge then value = 1 end
  local seed = math.floor(value) % modulus
  if seed <= 0 then seed = 1 end
  return seed
end

function RNG.random(state)
  state.rng_state = (state.rng_state * 48271) % modulus
  local value = state.rng_state / modulus
  state.last_rng = value
  return value
end

function RNG.int(state, low, high)
  assert(low <= high)
  return low + math.floor(RNG.random(state) * (high - low + 1))
end

return RNG
