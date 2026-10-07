using System;
using System.Collections.Generic;

namespace Tossup
{
    public enum OutcomeSide { Heads, Tails, Edge }
    public enum BuffTargetKind { NextCoins, NextCoinsOfType }

    // A targeted effect granted when one outcome resolves. Type targets count matching coins,
    // so a buff waits through unrelated coins and is spent by the next matching ones.
    public sealed class BuffTarget
    {
        public BuffTargetKind Kind;
        public CoinType? Type;
        public int Count;

        public BuffTarget() { } // RunSave restores public fields.

        BuffTarget(BuffTargetKind kind, CoinType? type, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            if (kind == BuffTargetKind.NextCoinsOfType && !type.HasValue) throw new ArgumentNullException(nameof(type));
            Kind = kind; Type = type; Count = count;
        }

        public static BuffTarget NextCoins(int count = 1) => new BuffTarget(BuffTargetKind.NextCoins, null, count);
        public static BuffTarget NextOfType(CoinType type, int count = 1) => new BuffTarget(BuffTargetKind.NextCoinsOfType, type, count);
    }

    // Describes both when the buff is created and which later outcome receives its effect.
    public sealed class BuffSpec
    {
        public string Id;
        public OutcomeSide Trigger;
        public BuffTarget Target;
        public OutcomeSide? AppliesOn;
        public Effect Effect;

        public BuffSpec() { } // RunSave restores public fields.

        public BuffSpec(string id, OutcomeSide trigger, BuffTarget target, Effect effect, OutcomeSide? appliesOn = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A buff needs a stable id.", nameof(id));
            Id = id; Trigger = trigger; Target = target ?? throw new ArgumentNullException(nameof(target));
            AppliesOn = appliesOn; Effect = effect?.Copy() ?? throw new ArgumentNullException(nameof(effect));
            if (Effect.Type == EffectType.TypeBuff && (!Effect.Kind.HasValue || Target.Type != Effect.Kind))
                throw new ArgumentException("A type buff target must match the effect's coin type.", nameof(target));
        }

        public BuffSpec Copy() => new BuffSpec(Id, Trigger, Target, Effect, AppliesOn);
    }

}
