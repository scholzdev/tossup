local RNG = {}
local modulus = 2147483647

function RNG.seed(value)
  local seed = math.floor(tonumber(value) or 1) % modulus
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
