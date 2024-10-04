using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MegalomaniaPlugin
{
    public static class Utils
    {
        //Thank you ConfigEgocentrism by Judgy53 for code reference:
        //https://github.com/Judgy53/ConfigEgocentrism/blob/main/ConfigEgocentrism/ConfigEgocentrismPlugin.cs

        public enum ItemTierLookup
        {
            white = ItemTier.Tier1,
            common = ItemTier.Tier1,

            green = ItemTier.Tier2,
            uncommon = ItemTier.Tier2,

            red = ItemTier.Tier3,
            legendary = ItemTier.Tier3,

            blue = ItemTier.Lunar,
            lunar = ItemTier.Lunar,

            yellow = ItemTier.Boss,
            boss = ItemTier.Boss,

            voidwhite = ItemTier.VoidTier1,
            voidcommon = ItemTier.VoidTier1,

            voidgreen = ItemTier.VoidTier2,
            voiduncommon = ItemTier.VoidTier2,

            voidred = ItemTier.VoidTier3,
            voidlegendary = ItemTier.VoidTier3,

            voidyellow = ItemTier.VoidBoss,
            voidboss = ItemTier.VoidBoss
        }

        public enum ConversionSelectionType
        {
            weighted = 0,
            priority = 1
        }
    }
}
