using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Diagnostics;
using UnityEngine.Networking;

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

        #region conversion
        private static ConfigEntry<double> ConfigTransformTime { get; set; }
        private static ConfigEntry<double> ConfigTransformTimePerStack { get; set; }
        private static ConfigEntry<double> ConfigTransformTimeDiminishing { get; set; }
        private static ConfigEntry<double> ConfigTransformTimeMax {  get; set; }
        #endregion

        #endregion


        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);
            CreateConfig();

            //Override Egocentrism code, haha. Sorry mate.
            On.RoR2.LunarSunBehavior.FixedUpdate += LunarSunBehavior_FixedUpdate;

            if (!ConfigCompatibilityMode.Value)
                HookLunarSunStats();
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
               "If true, the amount of bombs currently orbiting the player is used instead of the amount of Egocentrism, for stacking calculations.");
            ConfigPassiveBombAttack = Config.Bind("4. Bombs - Toggles", "Passive Bomb Attack", true,
                "Whether the vanilla seeking behavior should apply.");

            //TRANSFORMING
            //Time
            ConfigTransformTime = Config.Bind("5. Transform - Time", "Default Transform Timer", 60.0,
                "The time it takes for Egocentrism to transform another item");
            ConfigTransformTimePerStack = Config.Bind("5. Transform - Time", "Flat Time Per Stack", 0.0,
                "Added to transform timer to increase wait time when positive and decrease when negative.");
            ConfigTransformTimeDiminishing = Config.Bind("5. Transform - Time", "Multiplier Per Stack", 0.9,
                "Every stack multiplies the transform timer by this value.");
            ConfigTransformTimeMax = Config.Bind("5. Transform - Time", "Max Time", 120.0,
                "The maximum time Egocentrism can take before transforming an item.\n" +
                "Anything less than 1/60th of a second is forced back up to 1/60th of a second.");


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

            int configuredStackSize = stack;
            if (ConfigBombStacking.Value)
                configuredStackSize = body.master.GetDeployableCount(DeployableSlot.LunarSunBomb);

            #region Handle Bombs
            if (ConfigEnableBombs.Value &&
                !body.master.IsDeployableLimited(DeployableSlot.LunarSunBomb) &&
                projectileTimer > 3f / (float)stack)
            {
                projectileTimer = 0f;
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = projectilePrefab;
                fireProjectileInfo.crit = body.RollCrit();
                fireProjectileInfo.damage = body.damage * 3.6f;
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
            #endregion

            #region Handle Transforming
            transformTimer += Time.fixedDeltaTime;
            double calcTimer = Math.Min(
                ConfigTransformTime.Value * Math.Pow(ConfigTransformTimeDiminishing.Value, configuredStackSize)
                + configuredStackSize * ConfigTransformTimePerStack.Value
                , ConfigTransformTimeMax.Value);
            if (!(transformTimer > calcTimer))
            {
                goto EndTransformHandling;
            }
            transformTimer = 0f;
            if (!body.master || !body.inventory)
            {
                goto EndTransformHandling;
            }
            List<ItemIndex> list = new List<ItemIndex>(body.inventory.itemAcquisitionOrder);
            ItemIndex itemIndex = ItemIndex.None;
            Util.ShuffleList(list, transformRng);
            foreach (ItemIndex item in list)
            {
                if (item != DLC1Content.Items.LunarSun.itemIndex)
                {
                    ItemDef itemDef = ItemCatalog.GetItemDef(item);
                    if ((bool)itemDef && itemDef.tier != ItemTier.NoTier)
                    {
                        itemIndex = item;
                        break;
                    }
                }
            }
            if (itemIndex != ItemIndex.None)
            {
                body.inventory.RemoveItem(itemIndex);
                body.inventory.GiveItem(DLC1Content.Items.LunarSun);
                CharacterMasterNotificationQueue.SendTransformNotification(body.master, itemIndex, DLC1Content.Items.LunarSun.itemIndex, CharacterMasterNotificationQueue.TransformationType.LunarSun);
            }
            #endregion
            EndTransformHandling:;

            self.SetFieldValue("projectileTimer", projectileTimer);
            self.SetFieldValue("transformTimer", transformTimer);
        }

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            if (Input.GetKeyDown(KeyCode.F2))
            {
                // Get the player body to use a position:
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                // And then drop EGOCENTRISM in front of the player.

                Log.Info($"Player pressed F2. Spawning items at coordinates {transform.position}");
                //ego
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DLC1Content.Items.LunarSun.itemIndex), transform.position, transform.forward * 20f);
                //white scrap
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex), transform.position, transform.forward * 20f);
                //green scrap
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapGreen.itemIndex), transform.position, transform.forward * 20f);
                //red scrap
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapRed.itemIndex), transform.position, transform.forward * 20f);
                //yellow scrap
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapYellow.itemIndex), transform.position, transform.forward * 20f);
                //beads of fealty (lunar)
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.LunarTrinket.itemIndex), transform.position, transform.forward * 20f);
                //safer spaces (void white)
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DLC1Content.Items.BearVoid.itemIndex), transform.position, transform.forward * 20f);
                //plasma shrimp (void green)
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DLC1Content.Items.MissileVoid.itemIndex), transform.position, transform.forward * 20f);
                //pluripotent larva (void red)
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DLC1Content.Items.ExtraLifeVoid.itemIndex), transform.position, transform.forward * 20f);
                //newly hatched zoea (void yellow)
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DLC1Content.Items.VoidMegaCrabItem.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
