using RoR2;
using R2API;
using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;

namespace MegalomaniaPlugin.Utilities
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
        private static double initHealth;
        private static double health;
        private static double initRegen;
        private static double regen;
        private static double initArmor;
        private static double armor;
        private static double initDamage;
        private static double damage;
        private static double initCrit;
        private static double crit;
        private static double initATK;
        private static double attackSpeed;
        private static double initMove;
        private static double moveSpeed;
        private static bool doReplacePrimary;
        private static string primaryReplacementID;
        private static bool doReplaceSecondary;
        private static string secondaryReplacementID;
        private static bool doReplaceUtility;
        private static string utilityReplacementID;
        private static bool doReplaceSpecial;
        private static string specialReplacementID;
        private static int skillReplacementCount;
        private static bool benefitStats;
        private static bool harmStats;

        public static void start()
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

            initHealth = MegalomaniaPlugin.ConfigMaxHealthInitialStack.Value;
            health = MegalomaniaPlugin.ConfigMaxHealthPerStack.Value;

            initRegen = MegalomaniaPlugin.ConfigRegenInitialStack.Value;
            regen = MegalomaniaPlugin.ConfigRegenPerStack.Value;

            initArmor = MegalomaniaPlugin.ConfigArmorInitialStack.Value;
            armor = MegalomaniaPlugin.ConfigArmorPerStack.Value;

            initDamage = MegalomaniaPlugin.ConfigDamageInitialStack.Value * 100;
            damage = MegalomaniaPlugin.ConfigDamagePerStack.Value * 100;

            initCrit = MegalomaniaPlugin.ConfigCritChanceInitialStack.Value * 100;
            crit = MegalomaniaPlugin.ConfigCritChancePerStack.Value * 100;

            initATK = MegalomaniaPlugin.ConfigAttackSpeedInitialStack.Value * 100;
            attackSpeed = MegalomaniaPlugin.ConfigAttackSpeedPerStack.Value * 100;

            initMove = MegalomaniaPlugin.ConfigMovementSpeedInitialStack.Value * 100;
            moveSpeed = MegalomaniaPlugin.ConfigMovementSpeedPerStack.Value * 100;

            primaryReplacementID = Utils.lookupSkill(MegalomaniaPlugin.ConfigPrimarySkill.Value.Trim().ToLower()).skillNameToken;
            doReplacePrimary = MegalomaniaPlugin.ConfigPrimaryReplacement.Value && !primaryReplacementID.IsNullOrWhiteSpace();
            secondaryReplacementID = Utils.lookupSkill(MegalomaniaPlugin.ConfigSecondarySkill.Value.Trim().ToLower()).skillNameToken;
            doReplaceSecondary = MegalomaniaPlugin.ConfigSecondaryReplacement.Value && !secondaryReplacementID.IsNullOrWhiteSpace();
            utilityReplacementID = Utils.lookupSkill(MegalomaniaPlugin.ConfigUtilitySkill.Value.Trim().ToLower()).skillNameToken;
            doReplaceUtility = MegalomaniaPlugin.ConfigUtilityReplacement.Value && !utilityReplacementID.IsNullOrWhiteSpace();
            specialReplacementID = Utils.lookupSkill(MegalomaniaPlugin.ConfigSpecialSkill.Value.Trim().ToLower()).skillNameToken;
            doReplaceSpecial = MegalomaniaPlugin.ConfigSpecialReplacement.Value && !specialReplacementID.IsNullOrWhiteSpace();

            skillReplacementCount = 0;
            if (doReplacePrimary) skillReplacementCount++;
            if (doReplaceSecondary) skillReplacementCount++;
            if (doReplaceUtility) skillReplacementCount++;
            if (doReplaceSpecial) skillReplacementCount++;

            benefitStats = health > 0.0 || initHealth > 0.0
                || regen > 0.0 || initRegen > 0.0
                || armor > 0.0 || initArmor > 0.0
                || damage > 0.0 || initDamage > 0.0
                || crit > 0.0 || initCrit > 0.0
                || attackSpeed > 0.0 || initATK > 0.0
                || moveSpeed > 0.0 || initMove > 0.0;

            harmStats = health < 0.0 || initHealth < 0.0
                || regen < 0.0 || initRegen < 0.0
                || armor < 0.0 || initArmor < 0.0
                || damage < 0.0 || initDamage < 0.0
                || crit < 0.0 || initCrit < 0.0
                || attackSpeed < 0.0 || initATK < 0.0
                || moveSpeed < 0.0 || initMove < 0.0;

            setTokens();
        }

        private static void setTokens()
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
                    $"gain an <style=cIsDamage>orbiting bomb</style> that detonates on impact for <style=cIsDamage>{bombInitDamage * 100}%</style>{bombStackDamageString} damage, " +
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
                switch (Utils.parsedConversionSelectionType)
                {
                    case Utils.ConversionSelectionType.weighted:
                        selection = "weighted";
                        break;
                    case Utils.ConversionSelectionType.priority:
                        selection = "prioritized";
                        break;
                }

                string convertTo = "this";
                if (Utils.parsedItemConvertToList.Count > 1)
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
            if (health != 0.0 || initHealth != 0.0)
            {
                string stackingstr = "";
                if (health != 0.0)
                {
                    stackingstr = $"<style=cStack>(+{health} per stack)</style>";
                }
                statsString += $"Gain <style=cIsHealing>{initHealth}{stackingstr} max health</style>. ";
            }
            if (regen != 0.0 || initRegen != 0.0)
            {
                string stackingstr = "";
                if (regen != 0.0)
                {
                    stackingstr = $"<style=cStack>(+{regen} per stack)</style>";
                }
                statsString += $"Gain <style=cIsHealing>{initRegen}{stackingstr} health per second</style>. ";
            }
            if (armor != 0.0 || initArmor != 0.0)
            {
                string stackingstr = "";
                if (armor != 0.0)
                {
                    stackingstr = $"<style=cStack>(+{armor} per stack)</style>";
                }
                statsString += $"Gain <style=cIsUtility>{initArmor}{stackingstr} armor</style>. ";
            }
            if (damage != 0.0 || initDamage != 0.0)
            {
                string stackingstr = "";
                if (damage != 0.0)
                {
                    stackingstr = $"<style=cStack>(+{damage}% per stack)</style>";
                }
                statsString += $"Gain <style=cIsDamage>{initDamage}%{stackingstr} damage</style>. ";
            }
            if (crit != 0.0 || initCrit != 0.0)
            {
                string stackingstr = "";
                if (crit != 0.0)
                {
                    stackingstr = $"<style=cStack>(+{crit}% per stack)</style>";
                }
                statsString += $"Gain <style=cIsDamage>{initCrit}%{stackingstr} critical strike chance.</style> ";
            }
            if (attackSpeed != 0.0 || initATK != 0.0)
            {
                string stackingstr = "";
                if (attackSpeed != 0.0)
                {
                    stackingstr = $"<style=cStack>(+{attackSpeed}% per stack)</style>";
                }
                statsString += $"Gain <style=cIsDamage>{initATK}%{stackingstr} attack speed</style>. ";
            }
            if (moveSpeed != 0.0 || initMove != 0.0)
            {
                string stackingstr = "";
                if (moveSpeed != 0.0)
                {
                    stackingstr = $"<style=cStack>(+{moveSpeed}% per stack)</style>";
                }
                statsString += $"Gain <style=cIsUtility>{initMove}%{stackingstr} movement speed</style>. ";
            }

            string skillsReplacementString = "";
            if (doReplacePrimary)
            {
                string s = skillReplacementLookupEN(primaryReplacementID);
                skillsReplacementString += $"Replace primary skill with <style=cIsUtility>{s}</style>. ";
            }
            if (doReplaceSecondary)
            {
                string s = skillReplacementLookupEN(secondaryReplacementID);
                skillsReplacementString += $"Replace secondary skill with <style=cIsUtility>{s}</style>. ";
            }
            if (doReplaceUtility)
            {
                string s = skillReplacementLookupEN(utilityReplacementID);
                skillsReplacementString += $"Replace utility skill with <style=cIsUtility>{s}</style>. ";
            }
            if (doReplaceSpecial)
            {
                string s = skillReplacementLookupEN(specialReplacementID);
                skillsReplacementString += $"Replace special skill with <style=cIsUtility>{s}</style>. ";
            }

            //"ITEM_LUNARSUN_PICKUP": "Gain multiple orbiting bombs. <color=#FF7F7F>Every minute, assimilate another item into Egocentrism.</color>",
            //"ITEM_LUNARSUN_DESC": "Every <style=cIsUtility>3</style><style=cStack>(-50% per stack)</style> seconds, gain an <style=cIsDamage>orbiting bomb</style> that detonates on impact for <style=cIsDamage>360%</style> damage, up to a maximum of <style=cIsUtility>3<style=cStack>(+1 per stack)</style> bombs</style>. Every <style=cIsUtility>60</style> seconds, a random item is <style=cIsUtility>converted</style> into this item.",
            LanguageAPI.Add("ITEM_LUNARSUN_DESC",
                bombGenString + transformTimeString + transformStageString + statsString + skillsReplacementString,
            "en");

            string pickup_bombGenString = "";
            if (bombsAreEnabled)
            {
                pickup_bombGenString = "Gain multiple orbiting bombs. ";
            }

            string pickup_skillsString = "";

            if (skillReplacementCount > 0)
            {
                if (skillReplacementCount == 1)
                {
                    if (doReplacePrimary)
                    {
                        pickup_skillsString = $"Replace your primary skill with '{skillReplacementLookupEN(primaryReplacementID)}'. ";
                    }
                    else if (doReplaceSecondary)
                    {
                        pickup_skillsString = $"Replace your secondary with '{skillReplacementLookupEN(secondaryReplacementID)}'. ";
                    }
                    else if (doReplaceUtility)
                    {
                        pickup_skillsString = $"Replace your utility with '{skillReplacementLookupEN(utilityReplacementID)}'. ";
                    }
                    else if (doReplaceSpecial)
                    {
                        pickup_skillsString = $"Replace your special with '{skillReplacementLookupEN(specialReplacementID)}'. ";
                    }

                }
                else if (skillReplacementCount == 4)
                {
                    pickup_skillsString = "Replaces every skill. ";
                }
                else
                {
                    pickup_skillsString = "Replaces some skills. ";
                }
            }

            string pickup_statsString = "";

            if (benefitStats && harmStats)
            {
                pickup_statsString = "Increase some of your stats, <color=#FF7F7F>decrease some of your stats.</color> ";
            }
            else if (benefitStats)
            {
                pickup_statsString = "Increase some of your stats. ";
            }
            else if (harmStats)
            {
                pickup_statsString = "<color=#FF7F7F>Decrease some of your stats.</color> ";
            }

            string pickup_transformTimeString = "";
            if (transformsHappenOverTime)
            {
                if (Math.Abs(transformTime - 60.0f) < 1.0f)
                {
                    pickup_transformTimeString = "<color=#FF7F7F>Every minute, assimilate another item into Egocentrism.</color>";
                }
                else
                {
                    pickup_transformTimeString = $"<color=#FF7F7F>Every {transformTime} seconds, assimilate another item into Egocentrism.</color>";
                }
            }

            string pickup_transformStageString = "";
            if (transformOnStageStartHappens)
            {
                pickup_transformStageString = "<color=#FF7F7F>On the start of each stage, assimilate more items into Egocentrism.</color>";
            }

            LanguageAPI.Add("ITEM_LUNARSUN_PICKUP",
                pickup_bombGenString + pickup_skillsString + pickup_statsString + pickup_transformTimeString + pickup_transformStageString,
            "en");
        }

        //this function exists because i couldnt get languageapi to grab the translation
        private static string skillReplacementLookupEN(string lookupID)
        {
            string s = lookupID;
            switch (lookupID)
            {
                case "MEGALOMANIA_PRIMARY_NAME":
                    s = "Conceit";
                    break;
                case "MEGALOMANIA_MONOPOLIZE_NAME":
                    s = "Monopolize";
                    break;
                case "MEGALOMANIA_BOMB_NAME":
                    s = "Chimera Bomb";
                    break;
                case "MEGALOMANIA_TWINSHOT_NAME":
                    s = "Twin Shot";
                    break;
                case "MEGALOMANIA_SHELL_NAME":
                    s = "Chimera Shell";
                    break;
                case "MEGALOMANIA_MINIGUN_NAME":
                    s = "Chimera Minigun";
                    break;
            }

            return s;
        }
    }
}
