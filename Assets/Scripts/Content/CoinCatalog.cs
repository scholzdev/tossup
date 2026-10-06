using System.Collections.Generic;
using Tossup.Coins;

namespace Tossup
{
    public static class CoinCatalog
    {
        public static readonly NormalCoin Normal = new NormalCoin();
        public static readonly CopperCoin Copper = new CopperCoin();
        public static readonly SwordCoin Sword = new SwordCoin();
        public static readonly LuckyCoin Lucky = new LuckyCoin();
        public static readonly CursedCoin Cursed = new CursedCoin();
        public static readonly LoadedCoin Loaded = new LoadedCoin();
        public static readonly DaggerCoin Dagger = new DaggerCoin();
        public static readonly CompostCoin Compost = new CompostCoin();
        public static readonly SquareDanceCoin SquareDance = new SquareDanceCoin();
        public static readonly HammerCoin Hammer = new HammerCoin();
        public static readonly WhetstoneCoin Whetstone = new WhetstoneCoin();
        public static readonly BloodCoin Blood = new BloodCoin();
        public static readonly SparkCoin Spark = new SparkCoin();
        public static readonly FocusCoin Focus = new FocusCoin();
        public static readonly SnowballCoin Snowball = new SnowballCoin();
        public static readonly GamblerCoin Gambler = new GamblerCoin();
        public static readonly MomentumCoin Momentum = new MomentumCoin();
        public static readonly EchoCoin Echo = new EchoCoin();
        public static readonly VampireCoin Vampire = new VampireCoin();
        public static readonly MiserCoin Miser = new MiserCoin();
        public static readonly CounterfeiterCoin Counterfeiter = new CounterfeiterCoin();
        public static readonly FuseCoin Fuse = new FuseCoin();
        public static readonly PhoenixCoin Phoenix = new PhoenixCoin();
        public static readonly ContrarianCoin Contrarian = new ContrarianCoin();
        public static readonly ChainCoin Chain = new ChainCoin();
        public static readonly BankCoin Bank = new BankCoin();
        public static readonly LuckySevenCoin LuckySeven = new LuckySevenCoin();
        public static readonly HourglassCoin Hourglass = new HourglassCoin();
        public static readonly CapacitorCoin Capacitor = new CapacitorCoin();
        public static readonly MartyrCoin Martyr = new MartyrCoin();
        public static readonly BloodPactCoin BloodPact = new BloodPactCoin();
        public static readonly BountyCoin Bounty = new BountyCoin();
        public static readonly JesterCoin Jester = new JesterCoin();
        public static readonly DoppelgangerCoin Doppelganger = new DoppelgangerCoin();
        public static readonly FlockCoin Flock = new FlockCoin();
        public static readonly MegaphoneCoin Megaphone = new MegaphoneCoin();
        public static readonly CheerleaderCoin Cheerleader = new CheerleaderCoin();
        public static readonly MirrorCoin Mirror = new MirrorCoin();
        public static readonly TwinCoin Twin = new TwinCoin();
        public static readonly PotCoin Pot = new PotCoin();
        public static readonly DominoCoin Domino = new DominoCoin();
        public static readonly HotHandCoin HotHand = new HotHandCoin();
        public static readonly AnchorCoin Anchor = new AnchorCoin();
        public static readonly BettorCoin Bettor = new BettorCoin();
        public static readonly CashOutCoin CashOut = new CashOutCoin();
        public static readonly ColdStreakCoin ColdStreak = new ColdStreakCoin();
        public static readonly AmplifierCoin Amplifier = new AmplifierCoin();
        public static readonly TrueEchoCoin TrueEcho = new TrueEchoCoin();
        public static readonly DoublerCoin Doubler = new DoublerCoin();
        public static readonly JackpotCoin Jackpot = new JackpotCoin();
        public static readonly MimicCoin Mimic = new MimicCoin();
        public static readonly GoodDogCoin GoodDog = new GoodDogCoin();
        public static readonly OrchestraCoin Orchestra = new OrchestraCoin();
        public static readonly ConductorCoin Conductor = new ConductorCoin();
        public static readonly LifelineCoin Lifeline = new LifelineCoin();
        public static readonly HoroscopeCoin Horoscope = new HoroscopeCoin();
        public static readonly CrystalBallCoin CrystalBall = new CrystalBallCoin();
        public static readonly AllInCoin AllIn = new AllInCoin();
        public static readonly SafePort SafePort = new SafePort();

        public static readonly List<CoinDef> Ordered = new List<CoinDef>
        {
            Normal,
            Copper,
            Sword,
            Lucky,
            Cursed,
            Loaded,
            Dagger,
            Compost,
            SquareDance,
            Hammer,
            Whetstone,
            Blood,
            Spark,
            Focus,
            Snowball,
            Gambler,
            Momentum,
            Echo,
            Vampire,
            Miser,
            Counterfeiter,
            Fuse,
            Phoenix,
            Contrarian,
            Chain,
            Bank,
            LuckySeven,
            Hourglass,
            Capacitor,
            Martyr,
            BloodPact,
            Bounty,
            Jester,
            Doppelganger,
            Flock,
            Megaphone,
            Cheerleader,
            Mirror,
            Twin,
            Pot,
            Domino,
            HotHand,
            Anchor,
            Bettor,
            CashOut,
            ColdStreak,
            Amplifier,
            TrueEcho,
            Doubler,
            Jackpot,
            Mimic,
            GoodDog,
            Orchestra,
            Conductor,
            Lifeline,
            Horoscope,
            CrystalBall,
            AllIn,
            SafePort
        };
    }
}
