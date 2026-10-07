using System;
using System.Collections.Generic;

namespace Tossup
{
    // The enemy of a fight: a fixed pouch of coins (Content/EnemyCatalog.cs). After every round of yours it draws
    // EnemyDraw coins from it, flips them and scores their printed points; an empty pouch is refilled from its
    // used coins. No hooks, no gold, no buffs: the coin's Heads chance and the Score effects of the side it lands on.
    public static partial class Game
    {
        public static EnemyDef EnemyOf(GameState game) =>
            game.Encounter?.EnemyId != null && EnemyCatalog.ById.TryGetValue(game.Encounter.EnemyId, out var d) ? d : null;

        // The enemy of a fight, seeded by (run, level, salt) only, so the map can show it before you walk there.
        // An elite fight with no elite enemy for it falls back to a regular one.
        public static string PickEnemy(GameState g, int level, bool elite, bool boss, int salt)
        {
            var pool = new List<EnemyDef>();
            for (int pass = 0; pass < 2 && pool.Count == 0; pass++, elite = false)
                foreach (var d in EnemyCatalog.Ordered)
                    if (d.Boss == boss && (d.Elite == elite || boss) && level >= d.MinLevel && level <= d.MaxLevel) pool.Add(d);
            if (pool.Count == 0) return null;
            unchecked
            {
                ulong h = (ulong)g.Seed * 0x9E3779B97F4A7C15UL ^ (ulong)(level * 7919 + salt * 104729 + 91);
                h ^= h >> 31; h *= 0xBF58476D1CE4E5B9UL; h ^= h >> 29;
                return pool[(int)(h % (ulong)pool.Count)].Id;
            }
        }

        static void StartEnemy(GameState game, StageDef stage)
        {
            var e = game.Encounter;
            if (game.Sandbox != null && game.PendingEnemyId == null) return; // coin sandbox: no enemy unless asked for
            string id = game.PendingEnemyId; // set by the map; otherwise seeded by level
            game.PendingEnemyId = null;
            if (id == null || !EnemyCatalog.ById.ContainsKey(id)) id = PickEnemy(game, game.EncounterIndex, false, stage.Boss || stage.Endless != null, 0);
            if (id == null) return;
            var d = EnemyCatalog.ById[id];
            e.EnemyId = id;
            e.EnemyDraw = d.Draw + (int)Rule(game, "enemy_draw", 0);
            e.EnemyRoundPoints = d.RoundPoints + Rule(game, "enemy_round", 0) + 2 * (stage.Endless ?? 0);
            e.EnemyHeadsBonus = d.HeadsBonus;
            e.EnemyPouch = new List<string>(d.Pouch);
            Shuffle(game, e.EnemyPouch);
            Log(game, d.Name + " flips " + e.EnemyDraw + " coins a round.");
        }

        // The enemy's half of a round.
        static void EnemyTurn(GameState game)
        {
            var e = game.Encounter;
            e.EnemyFlips = new List<EnemyFlip>();
            if (EnemyOf(game) == null) return;
            for (int i = 0; i < e.EnemyDraw; i++)
            {
                if (e.EnemyPouch.Count == 0)
                {
                    if (e.EnemyDiscard.Count == 0) break;
                    e.EnemyPouch = e.EnemyDiscard;
                    e.EnemyDiscard = new List<string>();
                    Shuffle(game, e.EnemyPouch);
                }
                string id = e.EnemyPouch[0];
                e.EnemyPouch.RemoveAt(0);
                e.EnemyDiscard.Add(id);
                var coin = Content.Coins[id];
                double tie = coin.TieProbability, heads = Math.Max(0, Math.Min(1 - tie, coin.Probability + e.EnemyHeadsBonus));
                double roll = Rng.Random(game);
                string side = roll < heads ? Side.Heads : roll < heads + tie ? Side.Tie : Side.Tails;
                double points = 0;
                foreach (var effect in coin.EffectsFor(side == Side.Heads ? OutcomeSide.Heads : side == Side.Tie ? OutcomeSide.Edge : OutcomeSide.Tails))
                    if (effect.Type == EffectType.Score) points += effect.Amount;
                e.EnemyScore += points;
                e.EnemyFlips.Add(new EnemyFlip { CoinId = id, Result = side, Points = points });
            }
            if (e.EnemyRoundPoints > 0)
            {
                e.EnemyScore += e.EnemyRoundPoints;
                Log(game, EnemyOf(game).Name + " gains " + N(e.EnemyRoundPoints) + " points.");
            }
        }
    }
}
