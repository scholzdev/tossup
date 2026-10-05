using System;

namespace Tossup.UI
{
    // Sound effects (Resources/sfx/*.wav). The rules never play sound: Watch() looks at the game state every
    // frame and plays what changed, so a new sound only needs a line here.
    public static class Sound
    {
        public static readonly string[] Names =
            { "click", "flip", "land_heads", "land_tails", "score", "penalty", "combo", "discard", "buy", "shop", "levelup", "win", "lose" };

        public static float Master = .8f, Sfx = .8f, Music = .4f;

        static FlipAnimation lastAnimation;
        static FlipState lastResult;
        static bool? lastCleared;
        static int? lastDiscards;
        static Phase? lastPhase;
        static double? lastGold;

        // Volumes (0-100 in the profile options): master scales everything, then sfx or music.
        public static void Apply(Options options)
        {
            Master = (float)options.VolumeMaster / 100;
            Sfx = (float)options.VolumeSfx / 100;
            Music = (float)options.VolumeMusic / 100;
            Ui.Platform.SetMusicVolume(Master * Music * .6f);
        }

        public static void Play(string name, double pitch = 1, double volume = .7)
        {
            Ui.Platform.PlaySound(name, (float)pitch, (float)volume * Master * Sfx);
        }

        // Compare this frame with the last one and play the sounds for what changed.
        public static void Watch()
        {
            var game = Ui.Game;
            var animation = Ui.FlipAnimation;
            if (animation != null && lastAnimation == null) Play("flip");
            if (lastAnimation != null && animation == null) Play(lastAnimation.Outcome == Side.Heads ? "land_heads" : "land_tails");
            lastAnimation = animation;
            if (game == null)
            {
                lastPhase = null;
                lastResult = null;
                return;
            }

            var result = game.LastResult;
            if (result != null && result != lastResult) // a coin just resolved
            {
                if ((result.Penalty ?? 0) > 0) Play("penalty");
                else if ((result.Gained ?? 0) > 0) Play("score", 1 + Math.Min(result.Gained.Value, 24) / 48);
                if (result.Combo != null && result.Combo.Len >= 2) Play("combo", 1 + .07 * Math.Min(result.Combo.Len - 2, 8));
            }
            lastResult = result;

            var e = game.Encounter;
            if (e != null && game.Phase == Phase.Encounter)
            {
                if (e.Cleared && lastCleared == false) Play("levelup");
                if (lastDiscards.HasValue && e.Discards > lastDiscards.Value && game.Mulligan == null) Play("discard");
                lastCleared = e.Cleared;
                lastDiscards = e.Discards;
            }
            else
            {
                lastCleared = null;
                lastDiscards = null;
            }

            if (game.Phase != lastPhase)
            {
                if (game.Phase == Phase.Shop) Play("shop");
                else if (game.Phase == Phase.Victory) Play("win");
                else if (game.Phase == Phase.GameOver) Play("lose");
            }
            if (game.Phase == Phase.Shop && lastPhase == Phase.Shop && lastGold.HasValue && game.Player.Gold < lastGold.Value) Play("buy");
            lastPhase = game.Phase;
            lastGold = game.Player.Gold;
        }
    }
}
