using System;

namespace Tossup
{
    // Park-Miller "minimal standard" generator (multiplier 48271). The state lives on the game, so a
    // seed replays a whole run exactly.
    public static class Rng
    {
        const long Modulus = 2147483647;

        public static long Seed(double value)
        {
            double floored = Math.Floor(value);
            double seed = floored - Math.Floor(floored / Modulus) * Modulus; // Lua's % on numbers
            if (seed <= 0) seed = 1;
            return (long)seed;
        }

        public static double Random(GameState state)
        {
            state.RngState = state.RngState * 48271 % Modulus;
            double value = (double)state.RngState / Modulus;
            state.LastRng = value;
            return value;
        }

        public static int Int(GameState state, int low, int high)
        {
            if (low > high) throw new ArgumentException("Rng.Int: low > high");
            return low + (int)Math.Floor(Random(state) * (high - low + 1));
        }
    }
}
