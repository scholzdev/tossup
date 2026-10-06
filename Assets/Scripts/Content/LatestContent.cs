using System;
using System.Collections.Generic;

namespace Tossup
{
    // Content added after the first Unity port. Kept separate so the data-heavy catalogue stays reviewable.
    public static partial class Content
    {
        static void ApplyLatestItems(Dictionary<string, ItemDef> map)
        {
            map["energy_drink"] = new ItemDef { Id="energy_drink", Name="Energy Drink", Short="+2 NRG", Cost=8, Description="Gain 2 energy", Use=g=> { g.Player.Energy += 2; return true; } };
            map["shortcut"] = new ItemDef { Id="shortcut", Name="Shortcut", Short="+3 PTS", Cost=12, Description="Score 3 points at once", Use=g=> { Game.ApplyEffect(g,null,Effect.Score(3)); return true; } };
            map["safety_net"] = new ItemDef { Id="safety_net", Name="Safety Net", Short="SHIELD", Cost=9, Description="The next combo break is prevented", Use=g=> { Game.ApplyEffect(g,null,Effect.ComboShield(1)); return true; } };
            map["lucky_charm"] = new ItemDef { Id="lucky_charm", Name="Lucky Charm", Short="CHARM", Cost=10, Description="The next 2 coins +20% Heads", Use=g=> { Game.AddBuff(g,"odds",.2,2,true); if(g.Dealt!=null)g.Dealt.Probability=Math.Min(1,g.Dealt.Probability+.2); return true; } };
        }

        static void ApplyLatestCharacters(Dictionary<string, CharacterDef> c)
        {
            c["blade"].Deck=S(CoinCatalog.Normal,CoinCatalog.Sword,CoinCatalog.Dagger,CoinCatalog.Hammer,CoinCatalog.Vampire,CoinCatalog.Cursed);
            c["blade"].Pool.Add(CoinCatalog.Hammer);c["blade"].Pool.Add(CoinCatalog.Vampire);c["blade"].Pool.Add(CoinCatalog.Cursed);
            c["blade"].Pool.Add(CoinCatalog.Lunge);
            c["blade"].Locked=new List<LockedCoin>{K(CoinCatalog.Blood,4),K(CoinCatalog.Chain,5),K(CoinCatalog.Fuse,5),K(CoinCatalog.Focus,6),K(CoinCatalog.Martyr,6),K(CoinCatalog.Snowball,8),K(CoinCatalog.Spark,3),K(CoinCatalog.Jackpot,5),K(CoinCatalog.Lifeline,4),K(CoinCatalog.Megaphone,5),K(CoinCatalog.Pot,4),K(CoinCatalog.HotHand,4),K(CoinCatalog.CashOut,5),K(CoinCatalog.Doubler,6),K(CoinCatalog.Amplifier,6),K(CoinCatalog.Compost,4),K(CoinCatalog.SquareDance,4),K(CoinCatalog.GoodDog,6),K(CoinCatalog.Whetstone,4),K(CoinCatalog.BloodPact,5),K(CoinCatalog.Conductor,4),K(CoinCatalog.Parry,3),K(CoinCatalog.Executioner,6),K(CoinCatalog.BloodPrice,4),K(CoinCatalog.Feint,4),K(CoinCatalog.Sunder,6),K(CoinCatalog.Grit,3),K(CoinCatalog.Bloodletting,5)};
            c["seer"].Deck=S(CoinCatalog.Normal,CoinCatalog.Dagger,CoinCatalog.Lucky,CoinCatalog.Focus,CoinCatalog.Spark,CoinCatalog.Gambler); c["seer"].Pool=S(CoinCatalog.Normal,CoinCatalog.Dagger,CoinCatalog.Cursed,CoinCatalog.Gambler,CoinCatalog.Spark,CoinCatalog.Focus,CoinCatalog.Lucky,CoinCatalog.Compost,CoinCatalog.Constellation);
            c["seer"].Locked=new List<LockedCoin>{K(CoinCatalog.Horoscope,4),K(CoinCatalog.CrystalBall,5),K(CoinCatalog.Mimic,6),K(CoinCatalog.Contrarian,4),K(CoinCatalog.LuckySeven,4),K(CoinCatalog.Blood,4),K(CoinCatalog.Hourglass,5),K(CoinCatalog.Jester,5),K(CoinCatalog.Echo,6),K(CoinCatalog.Phoenix,6),K(CoinCatalog.Mirror,5),K(CoinCatalog.Domino,6),K(CoinCatalog.Twin,5),K(CoinCatalog.ColdStreak,4),K(CoinCatalog.Anchor,4),K(CoinCatalog.TrueEcho,6),K(CoinCatalog.Amplifier,6),K(CoinCatalog.SquareDance,4),K(CoinCatalog.GoodDog,6),K(CoinCatalog.BloodPact,5),K(CoinCatalog.Doppelganger,6),K(CoinCatalog.Omen,4),K(CoinCatalog.Moonwatch,4),K(CoinCatalog.Paradox,6),K(CoinCatalog.Premonition,4),K(CoinCatalog.Fateweaver,5),K(CoinCatalog.LookingGlass,6)};
            c["trader"].Deck=S(CoinCatalog.Dagger,CoinCatalog.Rebate,CoinCatalog.Harvest,CoinCatalog.Bank,CoinCatalog.Spark,CoinCatalog.Bettor); c["trader"].Pool=S(CoinCatalog.Normal,CoinCatalog.Copper,CoinCatalog.Loaded,CoinCatalog.Dagger,CoinCatalog.Sword,CoinCatalog.SquareDance,CoinCatalog.SafePort,CoinCatalog.Bank,CoinCatalog.Spark,CoinCatalog.Bettor,CoinCatalog.Harvest);
            c["trader"].Pool.Add(CoinCatalog.Rebate);
            c["trader"].Locked=new List<LockedCoin>{K(CoinCatalog.Miser,4),K(CoinCatalog.Hammer,4),K(CoinCatalog.Bounty,5),K(CoinCatalog.Flock,5),K(CoinCatalog.Momentum,5),K(CoinCatalog.Capacitor,6),K(CoinCatalog.Cheerleader,4),K(CoinCatalog.Megaphone,5),K(CoinCatalog.Orchestra,4),K(CoinCatalog.Lifeline,4),K(CoinCatalog.Jackpot,5),K(CoinCatalog.Anchor,4),K(CoinCatalog.Doubler,6),K(CoinCatalog.TrueEcho,6),K(CoinCatalog.Compost,4),K(CoinCatalog.GoodDog,6),K(CoinCatalog.Counterfeiter,4),K(CoinCatalog.Conductor,4),K(CoinCatalog.AllIn,6),K(CoinCatalog.Broker,5),K(CoinCatalog.Windfall,4),K(CoinCatalog.Dividend,4),K(CoinCatalog.LoanNote,3),K(CoinCatalog.Arbitrage,6),K(CoinCatalog.PotOfGreed,5)};

            c["tinkerer"] = new CharacterDef
            {
                Id="tinkerer", Name="The Tinkerer", Description="Tune and upgrade your coins", Starter=CoinCatalog.Normal,
                Perks=new List<CharacterPerkDef>
                {
                    new CharacterPerkDef { Type=CharacterPerkType.StartEnergy, Value=1, Name="Overclock", Description="Start each level with +1 Energy." },
                    new CharacterPerkDef { Type=CharacterPerkType.CoinDiscount, Value=2, Name="Bulk Parts", Description="Coin offers cost 2 less gold." }
                },
                Deck=S(CoinCatalog.Normal,CoinCatalog.Copper,CoinCatalog.Spark,CoinCatalog.Focus,CoinCatalog.Caliper,CoinCatalog.Doubler),
                Pool=S(CoinCatalog.Normal,CoinCatalog.Copper,CoinCatalog.Spark,CoinCatalog.Focus,CoinCatalog.Whetstone,CoinCatalog.Capacitor,CoinCatalog.Amplifier,CoinCatalog.Counterfeiter,CoinCatalog.SpareCoil,CoinCatalog.Salvage,CoinCatalog.Caliper,CoinCatalog.Doubler),
                Locked=new List<LockedCoin>{K(CoinCatalog.Hammer,3),K(CoinCatalog.Bank,4),K(CoinCatalog.Fuse,4),K(CoinCatalog.Hourglass,5),K(CoinCatalog.SafePort,5),K(CoinCatalog.Overclock,5),K(CoinCatalog.Prototype,5),K(CoinCatalog.Reactor,4)}
            };
            c["naturalist"] = new CharacterDef
            {
                Id="naturalist", Name="The Naturalist", Description="Grow a deck that keeps coming back", Starter=CoinCatalog.Normal,
                Perks=new List<CharacterPerkDef>
                {
                    new CharacterPerkDef { Type=CharacterPerkType.TailsGold, Value=1, Name="Bountiful Harvest", Description="Tails earns +1 gold." },
                    new CharacterPerkDef { Type=CharacterPerkType.ExtraExchange, Value=1, Name="Second Wind", Description="One extra exchange each level." }
                },
                Deck=S(CoinCatalog.Normal,CoinCatalog.Seedling,CoinCatalog.Flock,CoinCatalog.GoodDog,CoinCatalog.Symbiosis,CoinCatalog.Harvest),
                Pool=S(CoinCatalog.Normal,CoinCatalog.Compost,CoinCatalog.Flock,CoinCatalog.GoodDog,CoinCatalog.Snowball,CoinCatalog.Lifeline,CoinCatalog.Phoenix,CoinCatalog.Harvest,CoinCatalog.Seedling,CoinCatalog.Symbiosis,CoinCatalog.Rootstock,CoinCatalog.Thicket),
                Locked=new List<LockedCoin>{K(CoinCatalog.Vampire,4),K(CoinCatalog.Blood,4),K(CoinCatalog.BloodPact,5),K(CoinCatalog.SquareDance,4),K(CoinCatalog.Martyr,5),K(CoinCatalog.Doppelganger,6),K(CoinCatalog.Mycelium,4),K(CoinCatalog.Pollinator,5)}
            };
            c["conductor"] = new CharacterDef
            {
                Id="conductor", Name="The Conductor", Description="Build powerful streaks and combos", Starter=CoinCatalog.Normal,
                Perks=new List<CharacterPerkDef>
                {
                    new CharacterPerkDef { Type=CharacterPerkType.ComboStep, Value=.2, Name="Good Rhythm", Description="Combo multiplier grows +0.2 per step." },
                    new CharacterPerkDef { Type=CharacterPerkType.ComboShield, Value=1, Name="Encore", Description="Start each level with a combo shield." }
                },
                Deck=S(CoinCatalog.Normal,CoinCatalog.Normal,CoinCatalog.Momentum,CoinCatalog.Cheerleader,CoinCatalog.Echo,CoinCatalog.Orchestra),
                Pool=S(CoinCatalog.Normal,CoinCatalog.Momentum,CoinCatalog.Cheerleader,CoinCatalog.Echo,CoinCatalog.Orchestra,CoinCatalog.Megaphone,CoinCatalog.TrueEcho,CoinCatalog.Conductor,CoinCatalog.Drumroll),
                Locked=new List<LockedCoin>{K(CoinCatalog.Twin,4),K(CoinCatalog.Chain,4),K(CoinCatalog.Anchor,5),K(CoinCatalog.Doubler,6),K(CoinCatalog.Jackpot,5),K(CoinCatalog.AllIn,6),K(CoinCatalog.Reprise,6),K(CoinCatalog.Crescendo,4),K(CoinCatalog.Counterpoint,5),K(CoinCatalog.Encore,4),K(CoinCatalog.Syncopation,5),K(CoinCatalog.Finale,6)}
            };
        }
    }
}
