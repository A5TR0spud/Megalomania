using EntityStates;
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
using static UnityEngine.ParticleSystem.PlaybackState;

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
            duration = baseDuration / attackSpeedStat;
            Ray aimRay = GetAimRay();
            Vector3 muzzleLocation = aimRay.origin;
            GameObject root = base.gameObject;
            
            CharacterModel model = base.characterBody.modelLocator.modelTransform.gameObject.GetComponent<CharacterModel>();
            List<CharacterModel.ParentedPrefabDisplay> li = model.parentedPrefabDisplays;

            foreach (CharacterModel.ParentedPrefabDisplay iaa in li)
            {
                if (iaa.itemIndex == DLC1Content.Items.LunarSun.itemIndex)
                {
                    muzzleLocation = new Vector3(0, 0, 0);
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
            int iUsedToFloat = utils.TransformItems(base.characterBody.inventory, 5, null, base.characterBody.master, true);
            base.characterBody.inventory.GiveItem(DLC1Content.Items.LunarSun, Math.Max(iUsedToFloat, 1));
            if (iUsedToFloat < 1)
            {
                CharacterMasterNotificationQueue.PushItemNotification(base.characterBody.master, DLC1Content.Items.LunarSun.itemIndex);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
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
}
