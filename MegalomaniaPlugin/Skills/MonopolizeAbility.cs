﻿using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using static UnityEngine.ParticleSystem.PlaybackState;
using static UnityEngine.UI.GridLayoutGroup;

namespace MegalomaniaPlugin.Skills
{
    public class MonopolizeAbility : BaseSkillState
    {
        public static float baseDuration = 1f;
        private float duration;
        public static SkillDef MonopolizeSkill;
        public static GameObject muzzleFlashPrefab;
        public static Utils utils;

        public static void initEgoMonopolize(Sprite Icon, Utils ut)
        {
            muzzleFlashPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MuzzleFlashes/MuzzleflashLunarShard");
            utils = ut;

            MonopolizeSkill = ScriptableObject.CreateInstance<EgoSkillDef>();

            MonopolizeSkill.activationState = new SerializableEntityStateType(typeof(MonopolizeAbility));
            MonopolizeSkill.activationStateMachineName = "Weapon";
            MonopolizeSkill.beginSkillCooldownOnSkillEnd = true;
            MonopolizeSkill.canceledFromSprinting = false;
            MonopolizeSkill.cancelSprintingOnActivation = true;
            MonopolizeSkill.fullRestockOnAssign = true;
            MonopolizeSkill.interruptPriority = InterruptPriority.Any;
            MonopolizeSkill.isCombatSkill = false;
            MonopolizeSkill.mustKeyPress = true;
            MonopolizeSkill.baseMaxStock = 1;
            MonopolizeSkill.rechargeStock = 1;
            MonopolizeSkill.requiredStock = 1;
            MonopolizeSkill.stockToConsume = 1;
            MonopolizeSkill.baseRechargeInterval = 60f;
            //MonopolizeSkill.attackSpeedBuffsRestockSpeed = false;
            //ConceitSkill.attackSpeedBuffsRestockSpeed_Multiplier = 0.5f;
            MonopolizeSkill.icon = Icon;
            MonopolizeSkill.skillDescriptionToken = "MEGALOMANIA_MONOPOLIZE_DESC";
            MonopolizeSkill.skillName = "Monopolize";
            MonopolizeSkill.skillNameToken = "MEGALOMANIA_MONOPOLIZE_NAME";
            MonopolizeSkill.autoHandleLuminousShot = false;

            ContentAddition.AddSkillDef(MonopolizeSkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration;// / attackSpeedStat;
            Util.PlaySound("Play_voidman_R_activate", gameObject);

        }

        public override void OnExit()
        {
            base.OnExit();
            Trigger();
            Util.PlaySound("Play_voidman_R_pop", gameObject);
            //Util.PlaySound("Play_lunarsun_transform", gameObject);
        }

        public void Trigger()
        {
            Ray aimRay = GetAimRay();
            Vector3 muzzleLocation = aimRay.origin;
            GameObject root = base.gameObject;

            CharacterModel model = base.characterBody.modelLocator.modelTransform.gameObject.GetComponent<CharacterModel>();
            List<CharacterModel.ParentedPrefabDisplay> li = model.parentedPrefabDisplays;

            foreach (CharacterModel.ParentedPrefabDisplay iaa in li)
            {
                if (iaa.itemIndex == DLC1Content.Items.LunarSun.itemIndex)
                {
                    muzzleLocation = iaa.itemDisplay.transform.gameObject.transform.position;
                    root = iaa.itemDisplay.transform.gameObject;
                    break;
                }
            }

            if ((bool)muzzleFlashPrefab)
            {
                EffectData effectData = new EffectData
                {
                    origin = muzzleLocation,
                    rootObject = root
                };
                EffectManager.SpawnEffect(muzzleFlashPrefab, effectData, false);
            }
            List<ItemIndex> transList = utils.TransformItems(base.characterBody.inventory, 5, null, base.characterBody.master, true, notifUI: false);
            int transCount = transList.Count;
            //base.characterBody.inventory.GiveItem(DLC1Content.Items.LunarSun, Math.Max(transCount, 1));
            if (transCount < 1)
            {
                //Util.PlaySound("Play_lunar_exploder_m1_fire", gameObject);
                //base.characterBody.inventory.GiveItem(DLC1Content.Items.LunarSun);
                ItemTransferOrb item = ItemTransferOrb.DispatchItemTransferOrb(muzzleLocation, null, DLC1Content.Items.LunarSun.itemIndex, 1, delegate (ItemTransferOrb orb)
                {
                    base.characterBody.inventory.GiveItem(DLC1Content.Items.LunarSun);
                    CharacterMasterNotificationQueue.PushItemNotification(base.characterBody.master, DLC1Content.Items.LunarSun.itemIndex);
                }, orbDestinationOverride: base.characterBody.mainHurtBox);
                //CharacterMasterNotificationQueue.PushItemNotification(base.characterBody.master, DLC1Content.Items.LunarSun.itemIndex);
            }
            else
            {
                MonopolizeOrbUpdater orbUpdater = base.characterBody.gameObject.GetComponent<MonopolizeOrbUpdater>();
                if (!orbUpdater)
                    orbUpdater = base.characterBody.gameObject.AddComponent<MonopolizeOrbUpdater>();

                foreach (ItemIndex itemIndex in transList)
                {
                    ItemTransferOrb itemTransferOrb = ItemTransferOrb.DispatchItemTransferOrb(muzzleLocation, null, itemIndex, 1, delegate (ItemTransferOrb orb)
                    {
                        base.characterBody.inventory.GiveItem(DLC1Content.Items.LunarSun);
                        orbUpdater.currentOrbs.Remove(orb);
                    }, orbDestinationOverride: base.characterBody.mainHurtBox);

                    orbUpdater.currentOrbs.Add(itemTransferOrb);
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }

    public class MonopolizeOrbUpdater : MonoBehaviour
    {
        public List<ItemTransferOrb> currentOrbs = new List<ItemTransferOrb>();

        public void FixedUpdate()
        {
            List<ItemTransferOrb> toremove = new List<ItemTransferOrb>();
            foreach (ItemTransferOrb orb in currentOrbs)
            {
                if (orb.timeUntilArrival <= 0.5f)
                {
                    Chat.AddMessage("halfway");
                    GameObject orbEffectPrefab = orb.GetFieldValue<GameObject>("orbEffectPrefab");
                    Vector3 previousPosition = orbEffectPrefab.GetFieldValue<Vector3>("previousPosition");

                    ItemTransferOrb itemTransferOrb = ItemTransferOrb.DispatchItemTransferOrb(previousPosition, null, DLC1Content.Items.LunarSun.itemIndex, 1,
                        orbDestinationOverride: orb.target);

                    toremove.Add(orb);
                }
            }
            foreach (ItemTransferOrb orb in toremove)
            {
                currentOrbs.Remove(orb);
            }
            toremove.Clear();
        }
    }
}
