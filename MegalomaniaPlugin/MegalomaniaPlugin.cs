using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//MIT License

//This is my first RoR2 mod (that I've written code for) and basically my first C# project

//Thank you ConfigEgocentrism by Judgy53 for code reference:
//https://github.com/Judgy53/ConfigEgocentrism/blob/main/ConfigEgocentrism/ConfigEgocentrismPlugin.cs

//The github for this one is
//https://github.com/A5TR0spud/Megalomania/tree/master

namespace MegalomaniaPlugin
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]

    //https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
    [BepInDependency(LanguageAPI.PluginGUID)]

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class MegalomaniaPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "A5TR0spud";
        public const string PluginName = "Megalomania";
        public const string PluginVersion = "1.1.0";

        public static AssetBundle megalomaniaAssetBundle;
        public static Sprite EgoPrimarySprite;
        
        #region Constants and Configs

        public static ConfigEntry<bool> ConfigCompatibilityMode { get; set; }

        #region defensive
        public static ConfigEntry<double> ConfigMaxHealthPerStack { get; set; }
        public static ConfigEntry<double> ConfigRegenPerStack { get; set; }
        public static ConfigEntry<double> ConfigArmorPerStack { get; set; }
        public static ConfigEntry<double> ConfigArmorMax { get; set; }
        #endregion

        #region offensive
        public static ConfigEntry<double> ConfigDamagePerStack { get; set; }
        public static ConfigEntry<double> ConfigCritChancePerStack { get; set; }
        public static ConfigEntry<bool> ConfigAttackSpeedType { get; set; }
        public static ConfigEntry<double> ConfigAttackSpeedPerStack { get; set; }
        public static ConfigEntry<double> ConfigAttackSpeedBonusCap { get; set; }
        #endregion

        #region movement
        public static ConfigEntry<bool> ConfigMovementSpeedType { get; set; }
        public static ConfigEntry<double> ConfigMovementSpeedPerStack { get; set; }
        public static ConfigEntry<double> ConfigMovementSpeedBonusCap { get; set; }
        #endregion

        #region bomb toggles
        public static ConfigEntry<bool> ConfigEnableBombs { get; set; }
        public static ConfigEntry<bool> ConfigBombStacking { get; set; }
        public static ConfigEntry<bool> ConfigPrimaryReplacement { get; set; }
        public static ConfigEntry<bool> ConfigPrimaryEnhancement { get; set; }
        public static ConfigEntry<bool> ConfigPassiveBombAttack { get; set; }
        //public static ConfigEntry<bool> ConfigOnHitBombAttack { get; set; }
        #endregion

        #region bomb stats
        public static ConfigEntry<double> ConfigBombCreationRate { get; set; }
        public static ConfigEntry<double> ConfigBombCreationStackingMultiplier { get; set; }
        public static ConfigEntry<double> ConfigBombCreationStackingAdder { get; set; }
        public static ConfigEntry<double> ConfigBombDamage { get; set; }
        public static ConfigEntry<double> ConfigBombStackingDamage { get; set; }
        public static ConfigEntry<int> ConfigBombCap { get; set; }
        public static ConfigEntry<double> ConfigBombStackingCap { get; set; }
        public static ConfigEntry<double> ConfigBombRange { get; set; }
        public static ConfigEntry<double> ConfigBombStackingRange { get; set; }

        #endregion

        #region transform time
        public static ConfigEntry<int> ConfigStageStartTransform { get; set; }
        public static ConfigEntry<double> ConfigStageStartTransformStack { get; set; }
        public static ConfigEntry<double> ConfigTransformTime { get; set; }
        public static ConfigEntry<double> ConfigTransformTimePerStack { get; set; }
        public static ConfigEntry<double> ConfigTransformTimeDiminishing { get; set; }
        public static ConfigEntry<double> ConfigTransformTimeMin { get; set; }
        public static ConfigEntry<double> ConfigTransformTimeMax { get; set; }
        public static ConfigEntry<int> ConfigMaxTransformationsPerStage { get; set; }
        public static ConfigEntry<int> ConfigMaxTransformationsPerStageStacking { get; set; }
        #endregion

        #region transform rules
        public static ConfigEntry<bool> ConfigStackSizeMatters {  get; set; }
        public static ConfigEntry<double> ConfigStackSizeMultiplier { get; set; }
        public static ConfigEntry<double> ConfigStackSizeAdder { get; set; }
        public static ConfigEntry<string> ConfigConversionSelectionType { get; set; }
        public static ConfigEntry<string> ConfigItemsToConvertTo { get; set; }
        public static ConfigEntry<string> ConfigRarityPriorityList { get; set; }
        public static ConfigEntry<string> ConfigItemPriorityList { get; set; }
        #endregion

        #endregion

        #region Items
        public static ItemDef transformToken;
        #endregion

        public MegalomaniaEgoBehavior megalomaniaEgoBehavior { get; set; }
        public Utils utils { get; set; }

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);
            utils = new Utils();
            megalomaniaEgoBehavior = new MegalomaniaEgoBehavior();

            LoadAssets();
            CreateConfig();

            //Add pearl-like stats
            HookLunarSunStats();

            InitItems();
            InitSkills();

            utils.ParseRarityPriorityList();
            //parse items after items have loaded
            On.RoR2.ItemCatalog.SetItemDefs += ItemCatalog_SetItemDefs;
            utils.ParseConversionSelectionType(); 

            //Helper for transform time modality (benthic and timed max)
            //Clears counter for timed max, and does the conversion for benthic
            On.RoR2.CharacterMaster.OnServerStageBegin += CharacterMaster_OnServerStageBegin;

            On.RoR2.CharacterBody.OnInventoryChanged += CharacterBody_OnInventoryChanged;

            //everything from here on out is disabled in compatibility mode
            if (ConfigCompatibilityMode.Value)
                return;

            //Override Egocentrism code, haha. Sorry mate.
            megalomaniaEgoBehavior.init(utils);
        }

        private void CharacterBody_OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            SkillLocator skillLocator = self.skillLocator;

            if ((bool)skillLocator && ConfigPrimaryReplacement.Value)
            {
                //prioritize visions of heresy
                if ((bool)self.inventory && self.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement) == 0)
                    self.ReplaceSkillIfItemPresent(skillLocator.primary, DLC1Content.Items.LunarSun.itemIndex, ConceitAbility.ConceitSkill);
            }

            orig(self);
        }

        private void LoadAssets() {
            megalomaniaAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "AssetBundles", "megalomaniaassets"));

            EgoPrimarySprite = megalomaniaAssetBundle.LoadAsset<Sprite>("texConceitIcon");
        }

        private void ItemCatalog_SetItemDefs(On.RoR2.ItemCatalog.orig_SetItemDefs orig, ItemDef[] newItemDefs)
        {
            orig(newItemDefs);
            utils.ParseItemPriorityList();
            utils.ParseItemConvertToList();
        }



        private void InitItems()
        {
            transformToken = ScriptableObject.CreateInstance<ItemDef>();

            // Language Tokens, explained there https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/
            transformToken.name = "MEGALOMANIA_TOKEN_NAME";
            transformToken.nameToken = "MEGALOMANIA_TOKEN_NAME";
            transformToken.pickupToken = "MEGALOMANIA_TOKEN_PICKUP";
            transformToken.descriptionToken = "MEGALOMANIA_TOKEN_DESC";
            transformToken.loreToken = "MEGALOMANIA_TOKEN_LORE";

#pragma warning disable CS0618 // Type or member is obsolete
            transformToken.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618 // Type or member is obsolete

            transformToken.canRemove = false;
            transformToken.hidden = true;

            var displayRules = new ItemDisplayRuleDict(null);

            // Then finally add it to R2API
            ItemAPI.Add(new CustomItem(transformToken, displayRules));
        }

        private void InitSkills()
        {
            ConceitAbility.initEgoPrimary(EgoPrimarySprite);
        }

        private void CreateConfig()
        {
            ConfigCompatibilityMode = Config.Bind("0. Main", "Compatibility Mode", false,
               "If true, skips the hook to override Egocentrism's behavior:\n" +
               "Disables all bomb stat and transformation over time changes.\n" +
               "Other features, including stats (eg. max health), will still work.\n" +
               "Transformation on stage start will still work.");

            #region Stats
            // STATS
            //Defense
            ConfigMaxHealthPerStack = Config.Bind("1. Stats - Defensive", "Stacking Max Health", 5.0,
               "A flat amount added to max health per stack.");
            ConfigRegenPerStack = Config.Bind("1. Stats - Defensive", "Stacking Regeneration", 0.3,
               "A flat amount added to base regeneration per stack. Measured in health per second.");
            ConfigArmorPerStack = Config.Bind("1. Stats - Defensive", "Stacking Armor", 2.0,
               "A flat amount added to armor per stack.");
            ConfigArmorMax = Config.Bind("1. Stats - Defensive", "Stacking Armor Cap", 200.0,
               "Used to determine maximum armor benefit from stacking.\n" +
               "Set cap to a negative value to disable the cap.");
            //Offense
            ConfigDamagePerStack = Config.Bind("2. Stats - Offensive", "Stacking Damage", 0.02,
                "A percentage increase to damage per stack.");
            ConfigCritChancePerStack = Config.Bind("2. Stats - Offensive", "Stacking Crit Chance", 0.01,
                "A percentage increase to critical hit chance per stack.");
            ConfigAttackSpeedType = Config.Bind("2. Stats - Offensive", "Attack Speed Diminishing Returns", false,
                "If true, attack speed will have dimishing returns, with the limit towards infinity approaching the bonus cap.\n" +
                "If false, attack speed will stack linearly and cap at the bonus cap.");
            ConfigAttackSpeedPerStack = Config.Bind("2. Stats - Offensive", "Stacking Attack Speed", 0.028,
                "A percentage used to determine how much attack speed is given per item stack.");
            ConfigAttackSpeedBonusCap = Config.Bind("2. Stats - Offensive", "Bonus Attack Speed Cap", -1.0,
                "A percentage used to determine the maximum attack speed boost from Egocentrism stacking.\n" +
                "In linear mode, set cap to a negative value to disable the cap.\n" +
                "In any mode, set cap to 0 to disable attack speed bonus entirely.");
            //Movement Speed
            ConfigMovementSpeedType = Config.Bind("3. Stats - Movement Speed", "Movement Speed Diminishing Returns", true,
                "If true, movement speed will have dimishing returns, with the limit towards infinity approaching the bonus cap.\n" +
                "If false, movement speed will stack linearly and cap at the bonus cap.");
            ConfigMovementSpeedPerStack = Config.Bind("3. Stats - Movement Speed", "Stacking Movement Speed", 0.028,
                "A percentage used to determine how much speed is given per item stack.");
            ConfigMovementSpeedBonusCap = Config.Bind("3. Stats - Movement Speed", "Bonus Movement Speed Cap", 9.0,
                "A percentage used to determine the maximum speed boost from Egocentrism stacking.\n" +
                "In linear mode, set cap to a negative value to disable the cap.\n" +
                "In any mode, set cap to 0 to disable speed bonus entirely.");
            #endregion

            #region Bombs
            //BOMBS
            //Toggles
            ConfigEnableBombs = Config.Bind("4. Bombs - Toggles", "Enable Bomb Generation", true,
                "Should bombs be generated over time at all?");
            ConfigBombStacking = Config.Bind("4. Bombs - Toggles", "Bomb Stacking", false,
               "If true, the amount of bombs currently orbiting the player is used instead of the amount of Egocentrism, for stacking calculations of player stats.");
            ConfigPrimaryReplacement = Config.Bind("4. Bombs - Toggles", "Egocentrism Primary REPLACEMENT", false,
                "If true, holding Egocentrism replaces the primary skill with Conceit unless you have Visions of Heresy.");
            ConfigPrimaryEnhancement = Config.Bind("4. Bombs - Toggles", "Egocentrism Primary Enhancement", false,
                "If true, Egocentrism enhances your primary skill by firing Egocentrism bombs at enemies within 30 degrees of view.\n" +
                "Comparable to Shuriken.");
            ConfigPassiveBombAttack = Config.Bind("4. Bombs - Toggles", "Passive Bomb Attack", true,
                "Whether the vanilla seeking behavior should apply. If a bomb collides with an enemy, it might still explode.");
            /*ConfigOnHitBombAttack = Config.Bind("4. Bombs - Toggles", "On Hit: Bombs Attack", false,
                "If true, then any damage done against an enemy will also target an Egocentrism bomb at that enemy.\n" +
                "It doesn't care about proc coefficient (unless it's zero), but can't proc itself.");*/
            //Stats
            ConfigBombCreationRate = Config.Bind("5. Bombs - Stats", "Initial Bomb Creation Rate", 3.0,
                "How many seconds it takes to generate a bomb at stack size 1.");
            ConfigBombCreationStackingMultiplier = Config.Bind("5. Bombs - Stats", "Bomb Creation Stacking Multiplier", 0.5,
                "Scales the rate at which additional stacks decrease cooldown.\n" +
                "Lower values require more Egocentrism to reduce the cooldown by the same amount.\n" +
                "For example, 0.5 requires 2x as many stacks as 1 would to reduce the time by the same amount.");
            ConfigBombCreationStackingAdder = Config.Bind("5. Bombs - Stats", "Bomb Creation Stacking Adder", 0.0,
                "Time to add to bomb creation rate per stack. Can be negative.");
            ConfigBombDamage = Config.Bind("5. Bombs - Stats", "Initial Bomb Damage", 3.0,
                "A percentage of damage the bombs should do at stack size 1. Vanilla is 3.6 (360%).");
            ConfigBombStackingDamage = Config.Bind("5. Bombs - Stats", "Stacking Bomb Damage", 0.05,
                "How much damage to add to each bomb per stack.");
            ConfigBombCap = Config.Bind("5. Bombs - Stats", "Initial Bomb Cap", 3,
                "How many bombs can be generated at stack size 1.");
            ConfigBombStackingCap = Config.Bind("5. Bombs - Stats", "Stacking Bomb Cap", 1.0,
                "How many bombs to add to the bomb cap per stack.");
            ConfigBombRange = Config.Bind("5. Bombs - Stats", "Bomb Range", 17.5,
                "The distance at which bombs can target enemies.");
            ConfigBombStackingRange = Config.Bind("5. Bombs - Stats", "Stacking Bomb Range", 0.5,
                "The distance to add to bomb range per stack.");
            #endregion

            #region Transform
            //TRANSFORMING
            //Time
            ConfigStageStartTransform = Config.Bind("5. Transform - When to Transform", "Stage Start Transformations", 5,
                "How many items to convert on stage start, similar to Benthic Bloom.\n" +
                "If this is set to 0 or a negative number, conversion on stage start is disabled.");

            ConfigStageStartTransformStack = Config.Bind("5. Transform - When to Transform", "Stage Start Transformations Per Stack", 0.0,
                "How many items to convert on stage start per additional stack.\n" +
                "Rounded down after calculating.");

            ConfigTransformTime = Config.Bind("5. Transform - When to Transform", "Default Transform Timer", -60.0,
                "The time it takes for Egocentrism to transform another item.\n" +
                "If this is set a negative number, conversion over time is disabled.\n" +
                "Minimum allowed value is 1/60th of a second.");

            ConfigTransformTimePerStack = Config.Bind("5. Transform - When to Transform", "Flat Time Per Stack", 0.0,
                "Time to add to transform timer per stack. Can be negative.\n" +
                "Ignored if Default Transform Timer is 0");

            ConfigTransformTimeDiminishing = Config.Bind("5. Transform - When to Transform", "Multiplier Per Stack", 1.0,
                "Every stack multiplies the transform timer by this value.");

            ConfigTransformTimeMin = Config.Bind("5. Transform - When to Transform", "Min Time", 30.0,
                "The minimum time Egocentrism can take before transforming an item.\n" +
                "Anything less than 1/60th of a second is forced back up to 1/60th of a second.");

            ConfigTransformTimeMax = Config.Bind("5. Transform - When to Transform", "Max Time", 120.0,
                "The maximum time Egocentrism can take before transforming an item.\n" +
                "Anything less than 1/60th of a second is forced back up to 1/60th of a second.");

            ConfigMaxTransformationsPerStage = Config.Bind("5. Transform - When to Transform", "Max Transforms Per Stage", 10,
                "Caps how many transformations can happen per stage.\n" +
                "Set negative to disable cap.");

            ConfigMaxTransformationsPerStageStacking = Config.Bind("5. Transform - When to Transform", "Max Transforms Per Stage Per Stack", 0,
                "How many transformations to add to the cap per stack.\n" +
                "The system is intelligent and won't count stacks added by conversion from the current stage.");

            //Rules
            ConfigStackSizeMatters = Config.Bind("6. Transform - Rules", "Stack Size Matters", false,
                "If true, the weight of the item is multiplied by how many of that item you have.\n" +
                "Weight Calculation: TierWeight + ItemWeight + Floor(SSMultiplier * ItemWeight * (StackSize - 1) + SSAdder * (StackSize - 1))");

            ConfigStackSizeMultiplier = Config.Bind("6. Transform - Rules", "Stack Size Multiplier", 0.2,
                "How much to multiply subsequent stacks' weight by when Stack Size Matters is enabled.\n" +
                "Weight is rounded down after calculating.\n" +
                "Eg: A weighted item at 10 and a multiplier of 0.5 would stack 10 -> 15 -> 20 -> 25\n" +
                "Eg: A weighted item at 50 and a multiplier of 0.2 would stack 50 -> 60 -> 70 -> 80");

            ConfigStackSizeAdder = Config.Bind("6. Transform - Rules", "Stack Size Adder", 3.0,
                "How much to add to weight per subsequent stack when Stack Size Matters is enabled.\n" +
                "Weight is rounded down after calculating.\n" +
                "Eg: A weighted item at 10 and an adder of 5 would stack 10 -> 15 -> 20 -> 25\n" +
                "Eg: A weighted item at 50 and an adder of 0.5 would stack 50 -> 50 (50.5) -> 51 -> 51 (51.5)");

            ConfigConversionSelectionType = Config.Bind("6. Transform - Rules", "Conversion Selection Type", "Weighted",
                "Determines method for choosing items. Case insensitive. Allowed values:\n" +
                "Weighted: tends towards higher weighted items and tiers but maintains randomness.\n" +
                "Priority: always chooses the highest priority item available. If there's a tie, selects one at random.");

            ConfigItemsToConvertTo = Config.Bind("6. Transform - Rules", "Items To Convert To",
                "LunarSun:1",
                "A list of item that Egocentrism can convert other items into. Items cannot be converted into themselves.\n" +
                "If this is empty, conversion is disabled completely.\n" +
                "If an item does not make an appearance in this list or has a value of 0, that item cannot be converted into.\n" +
                "Higher numbers means Egocentrism is more conditioned to convert to that item, possibly instead of Egocentrism.\n" +
                "Format: item1:integer, item2:int, item3:i, etc\n" +
                "Case sensitive, somewhat whitespace sensitive.\n" +
                "The diplay name might not always equal the codename of the item.\n" +
                "Eg: Egocentrism = LunarSun. To find the name out for yourself, download the DebugToolkit mod, open the console (ctrl + alt + backtick (`)) and type in \"list_item\"");

            ConfigRarityPriorityList = Config.Bind("6. Transform - Rules", "Rarity:Priority List",
                "voidyellow:100, voidred:70, voidgreen:60, red:50, yellow:40, voidwhite:35, green:30, white:15, blue:0",
                "A priority of 0 or a negative priority blacklists that tier from Egocentrism.\n" +
                "If a rarity is not listed here, it cannot be converted by Egocentrism.\n" +
                "Higher numbers means Egocentrism is more conditioned to select that tier of item.\n" +
                "Format: tier1:integer, tier2:int, tier3:i, etc\n" +
                "Case insensitive, mostly whitespace insensitive.\n" +
                "Valid Tiers:\n" +
                "white,green,red,blue,yellow,voidwhite,voidgreen,voidred,voidyellow,\n" +
                "common,uncommon,legendary,lunar,boss,voidcommon,voiduncommon,voidlegendary,voidboss");

            ConfigItemPriorityList = Config.Bind("6. Transform - Rules", "Item:Priority List",
                "BeetleGland:5, GhostOnKill:5, MinorConstructOnKill:5, RoboBallBuddy:10, ScrapGreen:30, ScrapWhite:10, ScrapYellow:60, ScrapRed:20, RegeneratingScrap:-20, ExtraStatsOnLevelUp:15, FreeChest:-20, ExtraShrineItem:10, CloverVoid:15, LowerPricedChests:-20, ResetChests:-5",
                "Egocentrism is always blacklisted and cannot be converted from.\n" +
                "A priority of 0 blacklists that item from Egocentrism.\n" +
                "Can be negative. If negative is of a greater magnitude than the rarity, the item is blacklisted.\n" +
                "If a rarity that an item is part of is blacklisted but the item shows up in this list with a positive value, that item won't be blacklisted.\n" +
                "If a rarity is not listed here, its priority is determined exclusively by its tier.\n" +
                "Higher numbers means Egocentrism is more conditioned to select that item.\n" +
                "Format: item1:integer, item2:int, item3:i, etc\n" +
                "Case sensitive, somewhat whitespace sensitive.\n" +
                "The diplay name might not always equal the codename of the item.\n" +
                "For example: Wax Quail = JumpBoost. To find the name out for yourself, download the DebugToolkit mod, open the console (ctrl + alt + backtick (`)) and type in \"list_item\"");
            #endregion

            ConfigCleanup();
        }

        private void ConfigCleanup()
        {
            Dictionary<ConfigDefinition, string> orphanedEntries = Config.GetPropertyValue<Dictionary<ConfigDefinition, string>>("OrphanedEntries");
            orphanedEntries.Clear();

            Config.Save();
        }

        private void HookLunarSunStats()
        {
            
            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender && sender.inventory)
                {
                    int count;
                    if (ConfigBombStacking.Value)
                        count = sender.master.GetDeployableCount(DeployableSlot.LunarSunBomb);
                    else
                        count = sender.inventory.GetItemCount(DLC1Content.Items.LunarSun);
                    
                    if (count > 0)
                    {
                        //flat health
                        args.baseHealthAdd += count * (float)ConfigMaxHealthPerStack.Value;

                        //health regen
                        args.baseRegenAdd += count * (float)ConfigRegenPerStack.Value;

                        //movement speed
                        args.baseMoveSpeedAdd += utils.determineStatBoost(ConfigMovementSpeedType.Value, (float)ConfigMovementSpeedPerStack.Value, (float)ConfigMovementSpeedBonusCap.Value, count);

                        //damage
                        args.baseDamageAdd += count * (float)ConfigDamagePerStack.Value;

                        //attack speed
                        args.attackSpeedMultAdd += utils.determineStatBoost(ConfigAttackSpeedType.Value, (float)ConfigAttackSpeedPerStack.Value, (float)ConfigAttackSpeedBonusCap.Value, count);

                        //crit chance
                        args.critAdd += 100 * count * (float)ConfigCritChancePerStack.Value;

                        //armor
                        float calcArmor = count * (float)ConfigArmorPerStack.Value;
                        if (ConfigArmorMax.Value > 0)
                            calcArmor = Math.Min(calcArmor, (float)ConfigArmorMax.Value);
                        args.armorAdd += calcArmor;
                    }
                }
            };
        }


        [Server]
        private void CharacterMaster_OnServerStageBegin(On.RoR2.CharacterMaster.orig_OnServerStageBegin orig, CharacterMaster self, Stage stage)
        {
            orig(self, stage);

            Inventory inventory = self.inventory;

            if (!inventory)
            {
                return;
            }

            int tokenCount = inventory.GetItemCount(transformToken);
            if (tokenCount > 0)
            {
                inventory.RemoveItem(transformToken, tokenCount);
            }

            int egoCount = inventory.GetItemCount(DLC1Content.Items.LunarSun);
            if (ConfigStageStartTransform.Value > 0 && egoCount > 0)
            {
                int amount = ConfigStageStartTransform.Value + (int) ((egoCount - 1) * ConfigStageStartTransformStack.Value);
                if (ConfigMaxTransformationsPerStage.Value > 0)
                {
                    amount = Math.Min(amount, ConfigMaxTransformationsPerStage.Value);
                }
                utils.TransformItems(inventory, amount, null, self);
            }
        }

    }
}
