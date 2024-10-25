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
        public float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / base.attackSpeedStat;
            if ((bool)base.characterBody)
            {
                base.characterBody.SetSpreadBloom(1f, canOnlyIncreaseBloom: false);
            }
            if (!base.characterBody.HasBuff(DLC2Content.Buffs.DisableAllSkills.buffIndex))
            {
                Ray ray = GetAimRay();
                TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, BaseMinigunState.maxDistance, base.gameObject);
                FireBullet(GetAimRay(), finalBurstBulletCount, 2f, 2f);

                //Util.PlaySound(burstSound, base.gameObject);
                if (base.isAuthority)
                {
                    float num = selfForce * (base.characterMotor.isGrounded ? 0.5f : 1f) * base.characterMotor.mass;
                    base.characterMotor.ApplyForce(ray.direction * (0f - num));
                }
                Util.PlaySound("Play_lunar_wisp_attack1_shoot_bullet", base.gameObject);
                Util.PlaySound("Play_lunar_wisp_attack1_shoot_bullet", base.gameObject);
                Util.PlaySound("Play_lunar_wisp_attack1_shoot_bullet", base.gameObject);
                /*Util.PlaySound(BaseNailgunState.fireSoundString, base.gameObject);
                Util.PlaySound(BaseNailgunState.fireSoundString, base.gameObject);
                Util.PlaySound(BaseNailgunState.fireSoundString, base.gameObject);*/
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= duration && base.isAuthority)
            {
                outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
