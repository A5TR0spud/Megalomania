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
            ShellSkill.cancelSprintingOnActivation = true;
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
            //float maxBarrierToGain = base.characterBody.healthComponent.fullCombinedHealth * 0.5f - base.characterBody.healthComponent.barrier;
            //float barrierToGain = base.characterBody.healthComponent.fullCombinedHealth * 0.25f;
            //if (barrierToGain > maxBarrierToGain) barrierToGain = maxBarrierToGain;
            //if (barrierToGain > 0)
            base.characterBody.healthComponent.AddBarrier(base.characterBody.healthComponent.fullCombinedHealth * 0.25f);
            base.characterBody.AddTimedBuff(EgoShelledBuff.EgoShellBuff, 7);
            //base.characterBody.AddBuff(EgoShelledBuff.EgoShellBuff);
            /*base.characterBody.AddTimedBuff(RoR2Content.Buffs.LunarShell, 7);
            base.characterBody.AddTimedBuff(RoR2Content.Buffs.Slow50, 7);
            base.characterBody.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, 7);*/
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
