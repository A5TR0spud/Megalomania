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
    internal class MinigunSpinDown : BaseMinigunState
    {
        public static float baseDuration = 0.15f;
        public float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / base.attackSpeedStat;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= duration && base.isAuthority)
            {
                outer.SetNextState(new MinigunFinalBurst
                {
                    activatorSkillSlot = base.activatorSkillSlot
                });
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
