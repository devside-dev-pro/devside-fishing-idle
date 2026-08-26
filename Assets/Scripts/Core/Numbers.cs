using System;
using System.Globalization;

namespace Devside.FishingIdle.Core
{
    /// <summary>Formatage des grands nombres (1234 → « 1.23K »), invariant de culture.</summary>
    public static class Numbers
    {
        static readonly string[] Suffixes = { "", "K", "M", "B", "T", "aa", "ab", "ac", "ad", "ae" };

        public static string Format(double value)
        {
            if (double.IsNaN(value)) return "0";
            if (double.IsInfinity(value)) return "inf";

            string sign = value < 0 ? "-" : "";
            value = Math.Abs(value);

            // Sous 1000 : entier tronqué — les décimales qui défilent à chaque frame
            // font trembler toute l'UI (retour playtest).
            if (value < 1000)
                return sign + Math.Floor(value).ToString("0", CultureInfo.InvariantCulture);

            int tier = (int)Math.Floor(Math.Log10(value) / 3);
            if (tier >= Suffixes.Length)
                return sign + value.ToString("0.##e0", CultureInfo.InvariantCulture);

            double scaled = value / Math.Pow(1000, tier);
            return sign + scaled.ToString("0.##", CultureInfo.InvariantCulture) + Suffixes[tier];
        }
    }
}
