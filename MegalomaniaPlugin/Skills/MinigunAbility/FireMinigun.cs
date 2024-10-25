using EntityStates;
using MegalomaniaPlugin.Utilities;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MegalomaniaPlugin.Skills.MinigunAbility
{
    internal class FireMinigun : BaseMinigunState
    {
        public static SkillDef MinigunSkill;
        public static BuffIndex skillsDisabledBuffIndex;
        public float refireStopwatch;
        public float duration;
        public static float baseDuration = 0.1f;
        public static int fireNumber = 1;

        public static void initMinigunAbility(Sprite Icon)
        {
            MinigunSkill = ScriptableObject.CreateInstance<EgoSkillDef>();

            MinigunSkill.activationState = new SerializableEntityStateType(typeof(FireMinigun));
            MinigunSkill.activationStateMachineName = "Weapon";
            MinigunSkill.canceledFromSprinting = false;
            MinigunSkill.cancelSprintingOnActivation = true;
            MinigunSkill.fullRestockOnAssign = true;
            MinigunSkill.isCombatSkill = true;
            MinigunSkill.mustKeyPress = false;
            MinigunSkill.baseMaxStock = 1;
            MinigunSkill.rechargeStock = 1;
            MinigunSkill.requiredStock = 1;
            MinigunSkill.stockToConsume = 1;
            MinigunSkill.baseRechargeInterval = 0;
            MinigunSkill.interruptPriority = InterruptPriority.Any;
            MinigunSkill.autoHandleLuminousShot = true;
            MinigunSkill.icon = Icon;
            MinigunSkill.skillDescriptionToken = "MEGALOMANIA_MINIGUN_DESCRIPTION";
            MinigunSkill.skillName = "Chimera Minigun";
            MinigunSkill.skillNameToken = "MEGALOMANIA_MINIGUN_NAME";

            ContentAddition.AddSkillDef(MinigunSkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / base.attackSpeedStat;
            refireStopwatch = duration;
            skillsDisabledBuffIndex = DLC2Content.Buffs.DisableAllSkills.buffIndex;
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            refireStopwatch += GetDeltaTime();
            if (refireStopwatch >= duration)
            {
                refireStopwatch -= duration;
                Ray ray = GetAimRay();
                TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, BaseMinigunState.maxDistance, base.gameObject);
                Vector3 direction = ray.direction;
                Vector3 axis = Vector3.Cross(Vector3.up, direction);
                float num = Mathf.Sin((float)fireNumber * 0.5f);
                Vector3 vector = Quaternion.AngleAxis(0.5f * num, axis) * direction;
                vector = Quaternion.AngleAxis((float)fireNumber * -65.454544f, direction) * vector;
                ray.direction = vector;
                FireBullet(ray, 1, 1f, 1f);
            }
            if (base.isAuthority && (!IsKeyDownAuthority() || base.characterBody.isSprinting || base.characterBody.HasBuff(skillsDisabledBuffIndex)))
            {
                outer.SetNextState(new MinigunSpinDown
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
