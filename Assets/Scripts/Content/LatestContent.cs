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
            c["blade"].Deck=S(CoinCatalog.Normal,CoinCatalog.Sword,CoinCatalog.Dagger);
            c["blade"].Locked=new List<LockedCoin>{K(CoinCatalog.Hammer,3),K(CoinCatalog.Blood,4),K(CoinCatalog.Vampire,4),K(CoinCatalog.Chain,5),K(CoinCatalog.Cursed,5),K(CoinCatalog.Fuse,5),K(CoinCatalog.Focus,6),K(CoinCatalog.Martyr,6),K(CoinCatalog.Snowball,8),K(CoinCatalog.Spark,3),K(CoinCatalog.Jackpot,5),K(CoinCatalog.Lifeline,4),K(CoinCatalog.Megaphone,5),K(CoinCatalog.Pot,4),K(CoinCatalog.HotHand,4),K(CoinCatalog.CashOut,5),K(CoinCatalog.Doubler,6),K(CoinCatalog.Amplifier,6),K(CoinCatalog.Compost,4),K(CoinCatalog.SquareDance,4),K(CoinCatalog.GoodDog,6),K(CoinCatalog.Whetstone,4),K(CoinCatalog.BloodPact,5),K(CoinCatalog.Conductor,4)};
            c["seer"].Deck=S(CoinCatalog.Normal,CoinCatalog.Normal,CoinCatalog.Dagger,CoinCatalog.Focus,CoinCatalog.Spark); c["seer"].Pool=S(CoinCatalog.Normal,CoinCatalog.Dagger,CoinCatalog.Cursed,CoinCatalog.Gambler,CoinCatalog.Spark,CoinCatalog.Focus,CoinCatalog.Lucky,CoinCatalog.Compost);
            c["seer"].Locked=new List<LockedCoin>{K(CoinCatalog.Horoscope,4),K(CoinCatalog.CrystalBall,5),K(CoinCatalog.Mimic,6),K(CoinCatalog.Contrarian,4),K(CoinCatalog.LuckySeven,4),K(CoinCatalog.Blood,4),K(CoinCatalog.Hourglass,5),K(CoinCatalog.Jester,5),K(CoinCatalog.Echo,6),K(CoinCatalog.Phoenix,6),K(CoinCatalog.Mirror,5),K(CoinCatalog.Domino,6),K(CoinCatalog.Twin,5),K(CoinCatalog.ColdStreak,4),K(CoinCatalog.Anchor,4),K(CoinCatalog.TrueEcho,6),K(CoinCatalog.Amplifier,6),K(CoinCatalog.SquareDance,4),K(CoinCatalog.GoodDog,6),K(CoinCatalog.BloodPact,5),K(CoinCatalog.Doppelganger,6)};
            c["trader"].Deck=S(CoinCatalog.Normal,CoinCatalog.Loaded,CoinCatalog.Dagger); c["trader"].Pool=S(CoinCatalog.Normal,CoinCatalog.Copper,CoinCatalog.Loaded,CoinCatalog.Dagger,CoinCatalog.Sword,CoinCatalog.SquareDance,CoinCatalog.SafePort);
            c["trader"].Locked=new List<LockedCoin>{K(CoinCatalog.Spark,3),K(CoinCatalog.Bank,4),K(CoinCatalog.Miser,4),K(CoinCatalog.Hammer,4),K(CoinCatalog.Bounty,5),K(CoinCatalog.Flock,5),K(CoinCatalog.Momentum,5),K(CoinCatalog.Capacitor,6),K(CoinCatalog.Cheerleader,4),K(CoinCatalog.Megaphone,5),K(CoinCatalog.Orchestra,4),K(CoinCatalog.Lifeline,4),K(CoinCatalog.Jackpot,5),K(CoinCatalog.Bettor,5),K(CoinCatalog.Anchor,4),K(CoinCatalog.Doubler,6),K(CoinCatalog.TrueEcho,6),K(CoinCatalog.Compost,4),K(CoinCatalog.GoodDog,6),K(CoinCatalog.Counterfeiter,4),K(CoinCatalog.Conductor,4),K(CoinCatalog.AllIn,6)};
        }
    }
}
