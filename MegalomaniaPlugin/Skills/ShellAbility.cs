using EntityStates;
using MegalomaniaPlugin.Buffs;
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
using System.Numerics;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using static UnityEngine.ParticleSystem.PlaybackState;
using static UnityEngine.UI.GridLayoutGroup;
using static UnityEngine.UI.Image;

namespace MegalomaniaPlugin.Skills
{
    public class ShellAbility : BaseSkillState
    {
        public static float baseDuration = 1f;
        private float duration;
        public static SkillDef ShellSkill;
        public static GameObject muzzleFlashPrefab;

        public static void initEgoShell(Sprite Icon)
        {
            muzzleFlashPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MuzzleFlashes/MuzzleflashLunarShard");

            ShellSkill = ScriptableObject.CreateInstance<EgoSkillDef>();

            ShellSkill.activationState = new SerializableEntityStateType(typeof(ShellAbility));
            ShellSkill.activationStateMachineName = "Weapon";
            ShellSkill.beginSkillCooldownOnSkillEnd = true;
            ShellSkill.canceledFromSprinting = false;
            ShellSkill.cancelSprintingOnActivation = false;
            ShellSkill.fullRestockOnAssign = false;
            ShellSkill.interruptPriority = InterruptPriority.Any;
            ShellSkill.isCombatSkill = false;
            ShellSkill.mustKeyPress = false;
            ShellSkill.baseMaxStock = 1;
            ShellSkill.rechargeStock = 1;
            ShellSkill.requiredStock = 1;
            ShellSkill.stockToConsume = 1;
            ShellSkill.baseRechargeInterval = 14f;
            ShellSkill.interruptPriority = InterruptPriority.Skill;
            ShellSkill.icon = Icon;
            ShellSkill.skillDescriptionToken = "MEGALOMANIA_SHELL_DESCRIPTION";
            ShellSkill.skillName = "Chimera Shell";
            ShellSkill.skillNameToken = "MEGALOMANIA_SHELL_NAME";
            ShellSkill.autoHandleLuminousShot = true;

            ContentAddition.AddSkillDef(ShellSkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / base.attackSpeedStat;
            base.characterBody.healthComponent.ForceShieldRegen();
            base.characterBody.healthComponent.AddBarrier(base.characterBody.healthComponent.fullCombinedHealth * 0.25f);
            Util.PlayAttackSpeedSound("Play_lunar_golem_attack2_buildUp", gameObject, base.attackSpeedStat + 1.4f);
        }

        public override void OnExit()
        {
            base.OnExit();
            base.characterBody.AddTimedBuff(EgoShelledBuff.EgoShellBuff, 7);
            Util.PlaySound("Play_lunar_golem_attack2_shieldActivate", gameObject);
            EffectData effectData = new EffectData
            {
                origin = base.characterBody.footPosition,
                rotation = UnityEngine.Quaternion.identity
            };
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MonstersOnShrineUse"), effectData, transmit: true);
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
