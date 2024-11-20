using BepInEx;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MegalomaniaPlugin.Skills.MinigunAbility
{
    internal class MinigunFinalBurst : BaseMinigunState
    {
        public static int finalBurstBulletCount = 10;
        public static float burstTimeCostCoefficient = 1.2f;
        public static float selfForce = 10f;
        public static float baseDuration = 1f;
        public float duration = 0;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / base.attackSpeedStat;
            if (base.characterBody == null || !(bool)base.characterBody)
                return;
            if (base.gameObject == null || !(bool)base.gameObject)
                return;

            if (base.characterBody.isPlayerControlled)
                base.characterBody.SetSpreadBloom(1f, canOnlyIncreaseBloom: false);
            if (!base.characterBody.HasBuff(DLC2Content.Buffs.DisableAllSkills.buffIndex))
            {
                Ray ray = GetAimRay();
                TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, BaseMinigunState.maxDistance, base.gameObject);
                FireBullet(GetAimRay(), finalBurstBulletCount, 2f, 2f);

                if (base.isAuthority && base.characterMotor != null && (bool)base.characterMotor && base.characterMotor.canWalk)
                {
                    float num = selfForce * (base.characterMotor.isGrounded ? 0.5f : 1f) * base.characterMotor.mass;
                    base.characterMotor.ApplyForce(ray.direction * (0f - num));
                }
                Util.PlaySound("Play_lunar_wisp_attack1_shoot_bullet", base.gameObject);
                Util.PlaySound("Play_lunar_wisp_attack1_shoot_bullet", base.gameObject);
                Util.PlaySound("Play_lunar_wisp_attack1_shoot_bullet", base.gameObject);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (outer.mainStateType.stateType == null)
                return;
            if (outer.mainStateType.typeName.IsNullOrWhiteSpace())
                return;
            if (outer.IsInMainState())
                return; //??
            if (base.fixedAge >= duration && base.isAuthority)
            { 
                outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
