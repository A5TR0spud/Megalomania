using RoR2;
using R2API;
using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;

namespace MegalomaniaPlugin
{
    public class LanguageUtils
    {
        private static bool bombsAreEnabled;
        private static double bombInitialGenerationTime;
        private static double bombGenerationTimeRateMult;
        private static double bombGenerationTimeAdder;
        private static bool bombgenAffectedByMult;
        private static bool bombgenAffectedByAdder;
        private static double bombInitDamage;
        private static double bombDamageAdd;
        private static int bombCapInit;
        private static double bombCapAdd;
        private static double transformTime;
        private static double transformTimeMult;
        private static double transformTimeAdd;
        private static double transformTimeMin;
        private static double transformTimeMax;
        private static bool transformsHappenOverTime;
        private static bool transformAffectedByMult;
        private static bool transformAffectedByAdder;
        private static int transformStageStart;
        private static double transformStageStartStacking;
        private static bool transformOnStageStartHappens;
        private static double health;
        private static double regen;
        private static double armor;
        private static double damage;
        private static double crit;
        private static double attackSpeed;
        private static double moveSpeed;
        private static bool doReplacePrimary;
        private static string primaryReplacementID;
        private static bool doReplaceSpecial;
        private static string specialReplacementID;

        public static void init(Utils utils)
        {
            bombsAreEnabled = MegalomaniaPlugin.ConfigEnableBombs.Value;
            bombInitialGenerationTime = MegalomaniaPlugin.ConfigBombCreationRate.Value;
            bombGenerationTimeRateMult = 50.0 * MegalomaniaPlugin.ConfigBombCreationStackingMultiplier.Value;
            bombGenerationTimeAdder = MegalomaniaPlugin.ConfigBombCreationStackingAdder.Value;
            bombgenAffectedByMult = bombGenerationTimeRateMult != 1.0;
            bombgenAffectedByAdder = bombGenerationTimeAdder != 0.0;
            bombInitDamage = MegalomaniaPlugin.ConfigBombDamage.Value;
            bombDamageAdd = MegalomaniaPlugin.ConfigBombStackingDamage.Value;
            bombCapInit = MegalomaniaPlugin.ConfigBombCap.Value;
            bombCapAdd = MegalomaniaPlugin.ConfigBombStackingCap.Value;
            transformTime = MegalomaniaPlugin.ConfigTransformTime.Value;
            transformTimeMult = MegalomaniaPlugin.ConfigTransformTimeDiminishing.Value;
            transformTimeAdd = MegalomaniaPlugin.ConfigTransformTimePerStack.Value;
            transformTimeMin = MegalomaniaPlugin.ConfigTransformTimeMin.Value;
            transformTimeMax = MegalomaniaPlugin.ConfigTransformTimeMax.Value;
            transformsHappenOverTime = transformTime >= 0.0;
            transformAffectedByAdder = transformTime != 0.0;
            transformAffectedByMult = transformTimeMult != 1.0;
            transformStageStart = MegalomaniaPlugin.ConfigStageStartTransform.Value;
            transformStageStartStacking = MegalomaniaPlugin.ConfigStageStartTransformStack.Value;
            transformOnStageStartHappens = transformStageStart > 0;
            health = MegalomaniaPlugin.ConfigMaxHealthPerStack.Value;
            regen = MegalomaniaPlugin.ConfigRegenPerStack.Value;
            armor = MegalomaniaPlugin.ConfigArmorPerStack.Value;
            damage = MegalomaniaPlugin.ConfigDamagePerStack.Value * 100;
            crit = MegalomaniaPlugin.ConfigCritChancePerStack.Value * 100;
            attackSpeed = MegalomaniaPlugin.ConfigAttackSpeedPerStack.Value * 100;
            moveSpeed = MegalomaniaPlugin.ConfigMovementSpeedPerStack.Value * 100;
            primaryReplacementID = utils.lookupSkill(MegalomaniaPlugin.ConfigPrimarySkill.Value.Trim().ToLower()).skillNameToken;
            doReplacePrimary = MegalomaniaPlugin.ConfigPrimaryReplacement.Value && !primaryReplacementID.IsNullOrWhiteSpace();
            specialReplacementID = utils.lookupSkill(MegalomaniaPlugin.ConfigSpecialSkill.Value.Trim().ToLower()).skillNameToken;
            doReplaceSpecial = MegalomaniaPlugin.ConfigSpecialReplacement.Value && !specialReplacementID.IsNullOrWhiteSpace();

            initEN(utils);
        }

        private static void initEN(Utils utils)
        {
            string bombGenString = "";
            if (bombsAreEnabled)
            {
                string bombRateString = "";

                if (bombgenAffectedByMult)
                {
                    bombRateString += $"-{bombGenerationTimeRateMult}% per stack";
                }
                if (bombgenAffectedByMult && bombgenAffectedByAdder)
                {
                    bombRateString += ", ";
                }
                if (bombgenAffectedByAdder)
                {
                    string s = "";
                    if (bombGenerationTimeAdder > 0)
                    {
                        s = "+";
                    }
                    bombRateString += $"{s + bombGenerationTimeAdder}s per stack";
                }
                if (bombRateString != "")
                {
                    bombRateString = "<style=cStack>(" + bombRateString + ")</style>";
                }

                string bombStackDamageString = "";
                if (bombDamageAdd != 0.0)
                {
                    bombStackDamageString = $"<style=cStack>(+{bombDamageAdd * 100}% per stack)</style>";
                }

                string bombStackCapString = "";
                if (bombCapAdd != 0.0)
                {
                    bombStackCapString = $"<style=cStack>(+{bombCapAdd} per stack)</style>";
                }

                bombGenString = $"Every <style=cIsUtility>{bombInitialGenerationTime}</style>{bombRateString} seconds, " +
                    $"gain an <style=cIsDamage>orbiting bomb</style> that detonates on impact for <style=cIsDamage>{bombInitDamage*100}</style>{bombStackDamageString} damage, " +
                    $"up to a maximum of <style=cIsUtility>{bombCapInit + bombStackCapString} bombs</style>. ";
            }

            string transformTimeString = "";
            if (transformsHappenOverTime)
            {
                string transformStackString = "";
                if (transformAffectedByMult)
                {
                    double d = 1.0 - transformTimeMult;
                    string s = "";
                    if (d >= 0)
                    {
                        s = "+";
                    }
                    transformStackString += $"{s + d}% per stack";
                }
                if (transformAffectedByMult && transformAffectedByAdder)
                {
                    transformStackString += ", ";
                }
                if (transformAffectedByAdder)
                {
                    string s = "";
                    if (transformTimeMult >= 0)
                    {
                        s = "+";
                    }
                    transformStackString += $"{s + transformTimeMult}s per stack";
                }
                if (transformStackString != "")
                {
                    transformStackString = "<style=cStack>(" + transformStackString + ")</style>";
                }

                string selection = "random";
                switch (utils.parsedConversionSelectionType)
                {
                    case Utils.ConversionSelectionType.weighted:
                        selection = "weighted";
                        break;
                    case Utils.ConversionSelectionType.priority:
                        selection = "prioritized";
                        break;
                }

                string convertTo = "this";
                if (utils.parsedItemConvertToList.Count > 1)
                {
                    convertTo = "an";
                }

                transformTimeString = $"Every <style=cIsUtility>{transformTime + transformStackString}</style> seconds, a {selection} item is <style=cIsUtility>converted</style> into {convertTo} item. ";
            }

            string transformStageString = "";
            if (transformOnStageStartHappens)
            {
                string transformStackingString = "";
                if (transformStageStartStacking > 0.0)
                {
                    transformStackingString = "<style=cStack>(+" + transformStageStartStacking + " per stack)</style>";
                }
                transformStageString = $"On the start of each stage, <style=cIsUtility>transform</style> {transformStageStart + transformStackingString} items. ";
            }

            string statsString = "";
            if (health != 0.0)
            {
                statsString += $"Gain <style=cIsHealing>{health}<style=cStack>(+{health} per stack)</style> max health</style>. ";
            }
            if (regen != 0.0)
            {
                statsString += $"Gain <style=cIsHealing>{regen}<style=cStack>(+{regen} per stack)</style> health per second</style>. ";
            }
            if (armor != 0.0)
            {
                statsString += $"Gain <style=cIsUtility>{armor}<style=cStack>(+{armor} per stack)</style> armor</style>. ";
            }
            if (damage != 0.0)
            {
                statsString += $"Gain <style=cIsDamage>{damage}%<style=cStack>(+{damage}% per stack)</style> damage</style>. ";
            }
            if (crit != 0.0)
            {
                statsString += $"Gain <style=cIsDamage>{crit}%<style=cStack>(+{crit}% per stack)</style> critical strike chance.</style> ";
            }
            if (attackSpeed != 0.0)
            {
                statsString += $"Gain <style=cIsDamage>{attackSpeed}%<style=cStack>(+{attackSpeed}% per stack)</style> attack speed</style>. ";
            }
            if (moveSpeed != 0.0)
            {
                statsString += $"Gain <style=cIsUtility>{moveSpeed}%<style=cStack>(+{moveSpeed}% per stack)</style> movement speed</style>. ";
            }

            string skillsReplacementString = "";
            if (doReplacePrimary)
            {
                string s = primaryReplacementID;
                switch (primaryReplacementID)
                {
                    case "MEGALOMANIA_PRIMARY_NAME":
                        s = "Conceit";
                        break;
                    case "MEGALOMANIA_MONOPOLIZE_NAME":
                        s = "Monopolize";
                        break;
                }
                skillsReplacementString += $"Replace primary skill with <style=cIsUtility>{s}</style>. ";
            }
            if (doReplaceSpecial)
            {
                string s = specialReplacementID;
                switch (specialReplacementID)
                {
                    case "MEGALOMANIA_PRIMARY_NAME":
                        s = "Conceit";
                        break;
                    case "MEGALOMANIA_MONOPOLIZE_NAME":
                        s = "Monopolize";
                        break;
                }
                skillsReplacementString += $"Replace special skill with <style=cIsUtility>{s}</style>. ";
            }

            //"ITEM_LUNARSUN_PICKUP": "Gain multiple orbiting bombs. <color=#FF7F7F>Every minute, assimilate another item into Egocentrism.</color>",
            //"ITEM_LUNARSUN_DESC": "Every <style=cIsUtility>3</style><style=cStack>(-50% per stack)</style> seconds, gain an <style=cIsDamage>orbiting bomb</style> that detonates on impact for <style=cIsDamage>360%</style> damage, up to a maximum of <style=cIsUtility>3<style=cStack>(+1 per stack)</style> bombs</style>. Every <style=cIsUtility>60</style> seconds, a random item is <style=cIsUtility>converted</style> into this item.",
            LanguageAPI.AddOverlay("ITEM_LUNARSUN_DESC",
                bombGenString + transformTimeString + transformStageString + statsString + skillsReplacementString,
            "en");
        }
    }
}
