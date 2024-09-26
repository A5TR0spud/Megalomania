using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Diagnostics;
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
        //Desc:
        // Buffs Egocentrism to give some stat boosts. Adds blacklist. Highly configurable.
        public const string PluginVersion = "0.1.0";

        #region Constants and Configs

        private static ConfigEntry<bool> ConfigCompatibilityMode {  get; set; }

        #region defensive
        private static ConfigEntry<double> ConfigMaxHealthPerStack {  get; set; }
        private static ConfigEntry<double> ConfigRegenPerStack { get; set; }
        private static ConfigEntry<double> ConfigArmorPerStack { get; set; }
        private static ConfigEntry<double> ConfigArmorMax {  get; set; }
        #endregion

        #region offensive
        private static ConfigEntry<double> ConfigDamagePerStack { get; set; }
        private static ConfigEntry<double> ConfigCritChancePerStack { get; set; }
        private static ConfigEntry<bool> ConfigAttackSpeedType { get; set; }
        private static ConfigEntry<double> ConfigAttackSpeedPerStack { get; set; }
        private static ConfigEntry<double> ConfigAttackSpeedBonusCap { get; set; }
        #endregion

        #region movement
        private static ConfigEntry<bool> ConfigMovementSpeedType { get; set; }
        private static ConfigEntry<double> ConfigMovementSpeedPerStack { get; set; }
        private static ConfigEntry<double> ConfigMovementSpeedBonusCap { get; set; }
        #endregion

        #region bomb toggles
        private static ConfigEntry<bool> ConfigEnableBombs { get; set; }
        private static ConfigEntry<bool> ConfigBombStacking { get; set; }
        //private static ConfigEntry<bool> ConfigNewPrimary { get; set; }
        private static ConfigEntry<bool> ConfigPassiveBombAttack { get; set; }
        //private static ConfigEntry<bool> ConfigActiveBombAttack { get; set; }
        //private static ConfigEntry<bool> ConfigOnHitBombAttack { get; set; }
        #endregion

        #region bomb stats
        private static ConfigEntry<double> ConfigBombCreationRate { get; set; }
        private static ConfigEntry<double> ConfigBombCreationStackingMultiplier { get; set; }
        private static ConfigEntry<double> ConfigBombCreationStackingAdder { get; set; }
        private static ConfigEntry<double> ConfigBombDamage { get; set; }
        private static ConfigEntry<double> ConfigBombStackingDamage {  get; set; }
        private static ConfigEntry<int> ConfigBombCap { get; set; }
        private static ConfigEntry<double> ConfigBombStackingCap { get; set; }
        private static ConfigEntry<double> ConfigBombRange { get; set; }
        private static ConfigEntry<double> ConfigBombStackingRange { get; set; }

        #endregion

        #region transform time
        private static ConfigEntry<double> ConfigTransformTime { get; set; }
        private static ConfigEntry<double> ConfigTransformTimePerStack { get; set; }
        private static ConfigEntry<double> ConfigTransformTimeDiminishing { get; set; }
        private static ConfigEntry<double> ConfigTransformTimeMax {  get; set; }
        private static ConfigEntry<int> ConfigTransformMaxPerStage { get; set; }
        private static ConfigEntry<int> ConfigTransformMaxPerStageStacking { get; set; }
        #endregion

        #region transform rules
        private static ConfigEntry<string> ConfigRarityPriorityList {  get; set; }
        private static ConfigEntry<string> ConfigItemPriorityList { get; set; }
        #endregion

        #endregion

        #region Items
        private static ItemDef transformToken;
        #endregion

        //Parsed Rarity:Priority List
        private Dictionary<ItemTier, int> parsedRarityPriorityList;

        //Parsed Item:Priority List
        private Dictionary<ItemIndex, int> parsedItemPriorityList;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);
            CreateConfig();
            ParseRarityPriorityList();
            //parse item priority list after items have loaded
            On.RoR2.ItemCatalog.SetItemDefs += ItemCatalog_SetItemDefs;

            HookLunarSunStats();

            if (ConfigCompatibilityMode.Value)
                return;

            InitItems();

            //Override Egocentrism code, haha. Sorry mate.
            On.RoR2.LunarSunBehavior.FixedUpdate += LunarSunBehavior_FixedUpdate;
            On.RoR2.LunarSunBehavior.GetMaxProjectiles += LunarSunBehavior_GetMaxProjectiles;
            //Helper for transform time modality (benthic and timed max)
            On.RoR2.CharacterMaster.OnServerStageBegin += CharacterMaster_OnServerStageBegin;
        }

        private void ItemCatalog_SetItemDefs(On.RoR2.ItemCatalog.orig_SetItemDefs orig, ItemDef[] newItemDefs)
        {
            orig(newItemDefs);
            ParseItemPriorityList();
        }

        private void ParseRarityPriorityList()
        {
            parsedRarityPriorityList = new Dictionary<ItemTier, int>();

            string[] rarityPriority = ConfigRarityPriorityList.Value.Split(',');
            
            foreach (string rP in rarityPriority)
            {
                string[] Rapier = rP.Split(":");
                //if there's an incorrect amount of colons, skip
                if (Rapier.Length != 2)
                {
                    Log.Warning($"Invalid amount of colons: `{rP}`");
                    continue;
                }
                string tierString = Rapier[0].Trim().ToLower();
                string priorityString = Rapier[1].Trim();
                //if either side of the colon is blank, skip
                if (tierString.IsNullOrWhiteSpace() || priorityString.IsNullOrWhiteSpace())
                {
                    Log.Warning($"Invalid empty tier or priority: `{rP}`");
                    continue;
                }
                int priority;
                //if the priority is not an integer, skip
                if (!int.TryParse(priorityString, out priority) || priority < 0)
                {
                    Log.Warning($"Invalid priority: `{rP}`");
                    continue;
                }
                //if the priority is 0, skip
                if (priority == 0)
                {
                    Log.Info($"Blacklisting Rarity:Priority '{rP}'");
                    continue;
                }
                //if the rarity is undefined, skip
                if (!Enum.TryParse(tierString, out Utils.ItemTierLookup tier))
                {
                    Log.Warning($"Invalid rarity: `{rP}`");
                    continue;
                }
                ItemTier rarity = (ItemTier)tier;
                //if the rarity is already in the list, skip
                if (parsedRarityPriorityList.ContainsKey(rarity))
                {
                    Log.Warning($"Rarity already in list: `{rP}`");
                    continue;
                }
                parsedRarityPriorityList.Add(rarity, priority);
                Log.Info($"Rarity:Priority added! `{rP}`");
            }
        }

        private void ParseItemPriorityList()
        {
            parsedItemPriorityList = new Dictionary<ItemIndex, int>();

            string[] itemPriority = ConfigItemPriorityList.Value.Split(',');

            foreach (string iP in itemPriority)
            {
                string[] ItePrio = iP.Split(":");
                //if there's an incorrect amount of colons, skip
                if (ItePrio.Length != 2)
                {
                    Log.Warning($"Invalid amount of colons: `{iP}`");
                    continue;
                }
                string indexString = ItePrio[0].Trim();
                string priorityString = ItePrio[1].Trim();
                //if either side of the colon is blank, skip
                if (indexString.IsNullOrWhiteSpace() || priorityString.IsNullOrWhiteSpace())
                {
                    Log.Warning($"Invalid empty item or priority: `{iP}`");
                    continue;
                }
                int priority;
                //if the priority is not an integer, skip
                if (!int.TryParse(priorityString, out priority))
                {
                    Log.Warning($"Invalid priority: `{iP}`");
                    continue;
                }
                //if the item is undefined, skip
                ItemIndex index = ItemCatalog.FindItemIndex(indexString);
                if (index == ItemIndex.None)
                {
                    Log.Warning($"Invalid item: `{iP}`");
                    continue;
                }
                //if the rarity is already in the list, skip
                if (parsedItemPriorityList.ContainsKey(index))
                {
                    Log.Warning($"Item already in list: `{iP}`");
                    continue;
                }
                parsedItemPriorityList.Add(index, priority);
                Log.Info($"Item:Priority added! `{iP}`");
            }
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

        private void CreateConfig()
        {
            ConfigCompatibilityMode = Config.Bind("0. Main", "Compatibility Mode", false,
               "If true, skips the hook to override Egocentrism's behavior:\n" +
               "Disables all bomb stat and transformation changes.\n" +
               "Other features, including stats (eg. max health), will still work.\n" +
               "Changing requires a restart.");

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

            //BOMBS
            //Toggles
            ConfigEnableBombs = Config.Bind("4. Bombs - Toggles", "Enable Bomb Generation", true,
                "Should bombs be generated over time at all?");
            ConfigBombStacking = Config.Bind("4. Bombs - Toggles", "Bomb Stacking", false,
               "If true, the amount of bombs currently orbiting the player is used instead of the amount of Egocentrism, for stacking calculations of player stats.");
            ConfigPassiveBombAttack = Config.Bind("4. Bombs - Toggles", "Passive Bomb Attack", true,
                "Whether the vanilla seeking behavior should apply.");
            //Stats
            ConfigBombCreationRate = Config.Bind("5. Bombs - Stats", "Initial Bomb Creation Rate", 3.0,
                "How many seconds it takes to generate a bomb at stack size 1.");
            ConfigBombCreationStackingMultiplier = Config.Bind("5. Bombs - Stats", "Bomb Creation Stacking Multiplier", 1.0,
                "Scales the rate at which additional stacks decrease cooldown.\n" +
                "Lower values require more Egocentrism to reduce the cooldown by the same amount.\n" +
                "For example, 0.5 would require 2x as many stacks to reduce the time by the same amount.");
            ConfigBombCreationStackingAdder = Config.Bind("5. Bombs - Stats", "Bomb Creation Stacking Adder", 0.0,
                "Time to add to bomb creation rate per stack. Can be negative.");
            ConfigBombDamage = Config.Bind("5. Bombs - Stats", "Initial Bomb Damage", 2.0,
                "A percentage of damage the bombs should do at stack size 1. Vanilla is 3.6 (360%).");
            ConfigBombStackingDamage = Config.Bind("5. Bombs - Stats", "Stacking Bomb Damage", 0.1,
                "How much damage to add to each bomb per stack.");
            ConfigBombCap = Config.Bind("5. Bombs - Stats", "Initial Bomb Cap", 3,
                "How many bombs can be generated at stack size 1.");
            ConfigBombStackingCap = Config.Bind("5. Bombs - Stats", "Stacking Bomb Cap", 1.0,
                "How many bombs to add to the bomb cap per stack.");
            ConfigBombRange = Config.Bind("5. Bombs - Stats", "Bomb Range", 15.0,
                "The distance at which bombs can target enemies.");
            ConfigBombStackingRange = Config.Bind("5. Bombs - Stats", "Stacking Bomb Range", 1.0,
                "The distance to add to bomb range per stack.");

            //TRANSFORMING
            //Time
            ConfigTransformTime = Config.Bind("5. Transform - When to Transform", "Default Transform Timer", 0.0,
                "The time it takes for Egocentrism to transform another item.\n" +
                "If this is set to 0, behavior is overriden to happen on entering a new stage instead of over time, like Benthic Bloom.\n" +
                "Minimum allowed value is 1/60th of a second.");
            ConfigTransformTimePerStack = Config.Bind("5. Transform - When to Transform", "Flat Time Per Stack", 0.0,
                "Time to add to transform timer per stack. Can be negative.\n" +
                "Ignored if Default Transform Timer is 0");
            ConfigTransformTimeDiminishing = Config.Bind("5. Transform - When to Transform", "Multiplier Per Stack", 0.9,
                "Every stack multiplies the transform timer by this value.");
            ConfigTransformTimeMax = Config.Bind("5. Transform - When to Transform", "Max Time", 120.0,
                "The maximum time Egocentrism can take before transforming an item.\n" +
                "Anything less than 1/60th of a second is forced back up to 1/60th of a second.");
            ConfigTransformMaxPerStage = Config.Bind("5. Transform - When to Transform", "Max Transforms Per Stage", 5,
                "Caps how many transformations can happen per stage.\n" +
                "Set negative to disable cap, unless Default Transform Timer is 0.");
            ConfigTransformMaxPerStageStacking = Config.Bind("5. Transform - When to Transform", "Max Transforms Per Stage Per Stack", 0,
                "How many transformations to add to the cap per stack.\n" +
                "The system is intelligent and won't count stacks added by conversion from the current stage.");
            //Rules
            ConfigRarityPriorityList = Config.Bind("6. Transform - Rules", "Rarity:Priority List",
                "voidyellow:50, voidred:40, red:40, yellow:30, voidgreen:20, green:20, voidwhite:10, white:10, blue:0",
                "A priority of 0 or a negative priority blacklists that tier from Egocentrism.\n" +
                "If a rarity is not listed here, it cannot be converted by Egocentrism.\n" +
                "Higher numbers means Egocentrism is more conditioned to select that tier of item.\n" +
                "Format: tier1:integer, tier2:int, tier3:i, etc\n" +
                "Case insensitive, mostly whitespace insensitive.\n" +
                "Valid Tiers:\n" +
                "white,green,red,blue,yellow,voidwhite,voidgreen,voidred,voidyellow,\n" +
                "common,uncommon,legendary,lunar,boss,voidcommon,voiduncommon,voidlegendary,voidboss");
            ConfigItemPriorityList = Config.Bind("6. Transform - Rules", "Item:Priority List",
                "BeetleGland:5, GhostOnKill:5, MinorConstructOnKill:5, RoboBallBuddy:10, ScrapGreen:15, ScrapWhite:10, ScrapYellow:5, ScrapRed:1, RegeneratingScrap:-10, ExtraStatsOnLevelUp:20, FreeChest:-19, ExtraShrineItem:10, CloverVoid:-15, LowerPricedChests:-19, ResetChests:-5",
                "A priority of 0 blacklists that item from Egocentrism.\n" +
                "Can be negative. If negative is of a greater magnitude than the rarity, the item is blacklisted.\n" +
                "If a rarity that an item is part of is blacklisted but the item shows up in this list with a positive value, that item won't be blacklisted.\n" +
                "If a rarity is not listed here, its priority is determined exclusively by its tier.\n" +
                "Higher numbers means Egocentrism is more conditioned to select that item.\n" +
                "Format: item1:integer, item2:int, item3:i, etc\n" +
                "Case sensitive, somewhat whitespace sensitive.\n" +
                "The diplay name might not always equal the codename of the item.\n" +
                "For example: Wax Quail = JumpBoost. To find the name out for yourself, download the DebugToolkit mod, open the console (ctrl + alt + backtick (`)) and type in \"list_item\"");


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
                        args.baseMoveSpeedAdd += determineStatBoost(ConfigMovementSpeedType.Value, (float)ConfigMovementSpeedPerStack.Value, (float)ConfigMovementSpeedBonusCap.Value, count);

                        //damage
                        args.baseDamageAdd += count * (float)ConfigDamagePerStack.Value;

                        //attack speed
                        args.attackSpeedMultAdd += determineStatBoost(ConfigAttackSpeedType.Value, (float)ConfigAttackSpeedPerStack.Value, (float)ConfigAttackSpeedBonusCap.Value, count);

                        //crit chance
                        args.critAdd += count * (float)ConfigCritChancePerStack.Value;

                        //armor
                        float calcArmor = count * (float)ConfigArmorPerStack.Value;
                        if (ConfigArmorMax.Value > 0)
                            calcArmor = Math.Min(calcArmor, (float)ConfigArmorMax.Value);
                        args.armorAdd += calcArmor;
                    }
                }
            };
        }

        private float determineStatBoost(bool diminishing, float perStack, float max, float stacksize)
        {
            if (max == 0)
                //no buff
                return 0f;
            else if (diminishing)
                //diminishing returns
                return max - max * (float)Math.Pow(1f - (perStack / max), stacksize);
            else if (max > 0)
                //capped linear
                return Math.Min(perStack * stacksize, max);
            else
                //uncapped linear
                return perStack * stacksize;
        }

        private void LunarSunBehavior_FixedUpdate(On.RoR2.LunarSunBehavior.orig_FixedUpdate orig, LunarSunBehavior self)
        {
            //Grab private variables first, makes the code readable
            CharacterBody body = self.GetFieldValue<CharacterBody>("body");
            GameObject projectilePrefab = self.GetFieldValue<GameObject>("projectilePrefab");
            int stack = self.GetFieldValue<int>("stack");
            float projectileTimer = self.GetFieldValue<float>("projectileTimer");
            float transformTimer = self.GetFieldValue<float>("transformTimer");
            Xoroshiro128Plus transformRng = self.GetFieldValue<Xoroshiro128Plus>("transformRng");
            projectileTimer += Time.fixedDeltaTime;

            handleBombs(body, ref projectileTimer, stack, projectilePrefab);

            if (ConfigTransformTime.Value > 0)
                handleTransUpdate(body, ref transformTimer, stack, transformRng);

            self.SetFieldValue("projectileTimer", projectileTimer);
            self.SetFieldValue("transformTimer", transformTimer);
        }

        private int LunarSunBehavior_GetMaxProjectiles(On.RoR2.LunarSunBehavior.orig_GetMaxProjectiles orig, Inventory inventory)
        {
            return (int)(ConfigBombCap.Value + (inventory.GetItemCount(DLC1Content.Items.LunarSun) - 1) * ConfigBombStackingCap.Value);
        }

        private void handleBombs(CharacterBody body, ref float projectileTimer, int stack, GameObject projectilePrefab)
        {
            float denominator = (float)(stack - 1) * (float)ConfigBombCreationStackingMultiplier.Value + 1;
            if (ConfigEnableBombs.Value &&
                !body.master.IsDeployableLimited(DeployableSlot.LunarSunBomb) &&
                projectileTimer > ConfigBombCreationRate.Value / denominator + ConfigBombCreationStackingAdder.Value * stack)
            {
                projectileTimer = 0f;

                ProjectileSphereTargetFinder targetFinder = projectilePrefab.GetComponent<ProjectileSphereTargetFinder>();
                if (targetFinder)
                {
                    if (ConfigPassiveBombAttack.Value)
                        targetFinder.lookRange = (float)(ConfigBombRange.Value + (ConfigBombStackingRange.Value * (stack - 1)));
                    else
                        targetFinder.lookRange = 0;
                }
                else
                    Log.Error("LunarSunBehavior: Unable to modify projectile Range (ProjectileSphereTargetFinder component not found)");

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = projectilePrefab;
                fireProjectileInfo.crit = body.RollCrit();
                fireProjectileInfo.damage = body.damage * (float)(ConfigBombDamage.Value + ConfigBombStackingDamage.Value * stack);
                fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
                fireProjectileInfo.force = 0f;
                fireProjectileInfo.owner = body.gameObject;
                fireProjectileInfo.position = body.transform.position;
                fireProjectileInfo.rotation = Quaternion.identity;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                if (ConfigBombStacking.Value)
                {
                    body.statsDirty = true;
                    //DamageInfo damageInfo = new DamageInfo();
                    //damageInfo.damage = (float)ConfigMaxHealthPerStack.Value;
                    //damageInfo.damageType = DamageType.Silent;
                    //body.healthComponent.TakeDamage(damageInfo);
                }
            }
        }

        private void handleTransUpdate(CharacterBody body, ref float transformTimer, int stack, Xoroshiro128Plus transformRng)
        {
            //with acceptance
            transformTimer += Time.fixedDeltaTime;
            double calcTimer = Math.Min(
                ConfigTransformTime.Value * Math.Pow(ConfigTransformTimeDiminishing.Value, stack)
                + stack * ConfigTransformTimePerStack.Value
                , ConfigTransformTimeMax.Value);
            if (!(transformTimer > calcTimer))
            {
                return;
            }
            transformTimer = 0f;
            if (!body.master || !body.inventory)
            {
                return;
            }
            if (ConfigTransformMaxPerStage.Value > 0
                && body.inventory.GetItemCount(transformToken.itemIndex) >= ConfigTransformMaxPerStage.Value + (stack - 1) * ConfigTransformMaxPerStageStacking.Value)
            {
                return;
            }

            TransformItems(body.inventory, 1, transformRng, body.master);
        }

        private void TransformItems(Inventory inventory, int amount, Xoroshiro128Plus transformRng, CharacterMaster master)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("[Server] function 'TransformItems' called on client");
                return;
            }

            if (!inventory || !master)
                return;

            if (amount <= 0)
                return;

            if (transformRng == null)
            {
                transformRng = new Xoroshiro128Plus(Run.instance.seed);
            }

            List<ItemIndex> list = new List<ItemIndex>(inventory.itemAcquisitionOrder);
            //Util.ShuffleList(list, transformRng);

            int i = 0;
            int items = list.Count;
            if (items <= 0)
                return;

            Dictionary<ItemIndex, int> acceptableItems = new Dictionary<ItemIndex, int>();
            while (amount > 0 && i < items)
            {
                ItemIndex itemIndex = list[i];
                //don't convert egocentrism
                if (itemIndex == DLC1Content.Items.LunarSun.itemIndex)
                {
                    goto DiscardItem;
                }
                //don't convert things that don't exist
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                if (!(bool)itemDef)
                {
                    goto DiscardItem;
                }
                //don't convert untiered items
                if (itemDef.tier == ItemTier.NoTier)
                {
                    goto DiscardItem;
                }
                //get tier weight
                int weight = 0;
                if (!parsedRarityPriorityList.TryGetValue(itemDef.tier, out weight))
                {
                    weight = 0;
                }
                //don't convert blacklisted items
                int itemWeight = 1;
                if (parsedItemPriorityList.TryGetValue(itemIndex, out itemWeight) && itemWeight == 0)
                {
                    goto DiscardItem;
                }
                weight += itemWeight;
                //discard combination blacklisted items
                if (weight <= 0)
                {
                    goto DiscardItem;
                }

                //allow item transform
                acceptableItems.Add(itemIndex, weight);
                //amount--;
                //continue

                DiscardItem:
                i++;
            }

            while (amount > 0)
            {
                //tranform weighted random item
                ItemIndex itemToTransform = getRandomWeightedDictKey(acceptableItems, transformRng);
                inventory.RemoveItem(itemToTransform);
                inventory.GiveItem(DLC1Content.Items.LunarSun);
                if (ConfigTransformMaxPerStage.Value > 0)
                    inventory.GiveItem(transformToken, 1 + ConfigTransformMaxPerStageStacking.Value);
                CharacterMasterNotificationQueue.SendTransformNotification(master, itemToTransform, DLC1Content.Items.LunarSun.itemIndex, CharacterMasterNotificationQueue.TransformationType.LunarSun);
                
                if (inventory.GetItemCount(itemToTransform) <= 0)
                {
                    acceptableItems.Remove(itemToTransform);
                }
                
                amount--;
            }
        }

        private static T getRandomWeightedDictKey<T>(Dictionary<T, int> dict, Xoroshiro128Plus rng)
        {
            int totalWeight = 0;
            foreach (var weight in dict.Values)
            {
                totalWeight += weight;
            }

            int randomNumber = rng.RangeInt(0, totalWeight);
            foreach (var kvp in dict)
            {
                randomNumber -= kvp.Value;
                if (randomNumber < 0)
                {
                    return kvp.Key;
                }
            }

            Log.Error("Couldn't return a random weighted dictionary key! This shouldn't happen if all weights are positive. Returned FirstOrDefault() instead.");
            return dict.FirstOrDefault().Key;
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

            int egoCount = inventory.GetItemCount(DLC1Content.Items.LunarSun);
            if (ConfigTransformTime.Value == 0 && egoCount > 0)
            {
                int amount = ConfigTransformMaxPerStage.Value + (egoCount - 1) * ConfigTransformMaxPerStageStacking.Value;
                TransformItems(inventory, amount, null, self);
            }

            int tokenCount = inventory.GetItemCount(transformToken);
            if (tokenCount > 0)
            {
                inventory.RemoveItem(transformToken, tokenCount);
            }
        }

        // The Update() method is run on every frame of the game.
        /*private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                CharacterMaster player = PlayerCharacterMasterController.instances[0].master;
                // Get the player body to use a position:
                var transform = player.GetBodyObject().transform;

                // And then drop EGOCENTRISM in front of the player.

                Log.Info($"Player pressed F2. Spawning items at coordinates {transform.position}");
                //ego
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DLC1Content.Items.LunarSun.itemIndex), transform.position, transform.forward * 20f);
                player.inventory.GiveItem(RoR2Content.Items.ScrapWhite.itemIndex);
                player.inventory.GiveItem(RoR2Content.Items.ScrapGreen.itemIndex);
                player.inventory.GiveItem(RoR2Content.Items.ScrapRed.itemIndex);
                player.inventory.GiveItem(RoR2Content.Items.ScrapYellow.itemIndex);
                player.inventory.GiveItem(RoR2Content.Items.FocusConvergence.itemIndex);
                player.inventory.GiveItem(DLC1Content.Items.BearVoid.itemIndex);
                player.inventory.GiveItem(DLC1Content.Items.MissileVoid.itemIndex);
                player.inventory.GiveItem(DLC1Content.Items.ExtraLifeVoid.itemIndex);
                player.inventory.GiveItem(DLC1Content.Items.CloverVoid.itemIndex);
                player.inventory.GiveItem(DLC1Content.Items.RegeneratingScrapConsumed.itemIndex);
                player.inventory.GiveItem(DLC2Content.Items.LowerPricedChestsConsumed.itemIndex);
            }
        }*/
    }
}
