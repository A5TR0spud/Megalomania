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

        #endregion


        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            Log.Init(Logger);
            CreateConfig();

            //Override Egocentrism code, haha. Sorry mate.
            On.RoR2.LunarSunBehavior.FixedUpdate += LunarSunBehavior_FixedUpdate;

            HookLunarSunStats();
        }

        private void CreateConfig()
        {
            // STATS
            //Defense
            ConfigMaxHealthPerStack = Config.Bind("Stats - Defensive", "Stacking Max Health", 5.0,
               "A flat amount added to max health per stack.");
            ConfigRegenPerStack = Config.Bind("Stats - Defensive", "Stacking Regeneration", 0.3,
               "A flat amount added to base regeneration per stack. Measured in health per second.");
            ConfigArmorPerStack = Config.Bind("Stats - Defensive", "Stacking Armor", 2.0,
               "A flat amount added to armor per stack.");
            ConfigArmorMax = Config.Bind("Stats - Defensive", "Stacking Armor Cap", 200.0,
               "Used to determine maximum armor benefit from stacking.\n" +
               "Set cap to a negative value to disable the cap.");
            //Offense
            ConfigDamagePerStack = Config.Bind("Stats - Offensive", "Stacking Damage", 0.02,
                "A percentage increase to damage per stack.");
            ConfigCritChancePerStack = Config.Bind("Stats - Offensive", "Stacking Crit Chance", 0.01,
                "A percentage increase to critical hit chance per stack.");
            ConfigAttackSpeedType = Config.Bind("Stats - Offensive", "Attack Speed Diminishing Returns", false,
                "If true, attack speed will have dimishing returns, with the limit towards infinity approaching the bonus cap.\n" +
                "If false, attack speed will stack linearly and cap at the bonus cap.");
            ConfigAttackSpeedPerStack = Config.Bind("Stats - Offensive", "Stacking Attack Speed", 0.028,
                "A percentage used to determine how much attack speed is given per item stack.");
            ConfigAttackSpeedBonusCap = Config.Bind("Stats - Offensive", "Bonus Attack Speed Cap", 9.0,
                "A percentage used to determine the maximum attack speed boost from Egocentrism stacking.\n" +
                "In linear mode, set cap to a negative value to disable the cap.\n" +
                "In any mode, set cap to 0 to disable attack speed bonus entirely.");
            //Movement Speed
            ConfigMovementSpeedType = Config.Bind("Stats - Movement Speed", "Movement Speed Diminishing Returns", true,
                "If true, movement speed will have dimishing returns, with the limit towards infinity approaching the bonus cap.\n" +
                "If false, movement speed will stack linearly and cap at the bonus cap.");
            ConfigMovementSpeedPerStack = Config.Bind("Stats - Movement Speed", "Stacking Movement Speed", 0.028,
                "A percentage used to determine how much speed is given per item stack.");
            ConfigMovementSpeedBonusCap = Config.Bind("Stats - Movement Speed", "Bonus Movement Speed Cap", -1.0,
                "A percentage used to determine the maximum speed boost from Egocentrism stacking.\n" +
                "In linear mode, set cap to a negative value to disable the cap.\n" +
                "In any mode, set cap to 0 to disable speed bonus entirely.");

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
                    int count = sender.inventory.GetItemCount(DLC1Content.Items.LunarSun);
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

            if (!body.master.IsDeployableLimited(DeployableSlot.LunarSunBomb) && projectileTimer > 3f / (float)stack)
            {
                projectileTimer = 0f;
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = projectilePrefab;
                fireProjectileInfo.crit = body.RollCrit();
                fireProjectileInfo.damage = body.damage * 3.6f;
                fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
                fireProjectileInfo.force = 0f;
                fireProjectileInfo.owner = base.gameObject;
                fireProjectileInfo.position = body.transform.position;
                fireProjectileInfo.rotation = Quaternion.identity;
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
            transformTimer += Time.fixedDeltaTime;
            if (!(transformTimer > 1f))
            {
                return;
            }
            transformTimer = 0f;
            if (!body.master || !body.inventory)
            {
                return;
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
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(DLC1Content.Items.LunarSun.itemIndex), transform.position, transform.forward * 20f);
            }
        }
    }
}
