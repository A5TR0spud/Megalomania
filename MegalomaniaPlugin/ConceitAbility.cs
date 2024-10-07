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

namespace MegalomaniaPlugin
{
    public class ConceitAbility : BaseSkillState
    {
        public static float baseDuration = 1f;
        public static float burstShotsPerSecond = 12;
        public static int shotsPerBurst = 3;
        public static float baseBurstDuration = (float)shotsPerBurst / burstShotsPerSecond;
        private float burstDuration;
        private int shotsLeftInBurst = 0;
        private float duration;
        public static SkillDef ConceitSkill;
        static float damageCoefficient = 0.9f;
        static float force = 0.1f;
        public static GameObject projectilePrefab;

        public static void initEgoPrimary(Sprite Icon)
        {
            //LunarGolemTwinShotProjectile
            //LunarWispTrackingBomb
            //LunarExploderShardProjectile
            projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarExploderShardProjectile");


            ConceitSkill = ScriptableObject.CreateInstance<EgoPrimarySkillDef>();

            ConceitSkill.activationState = new SerializableEntityStateType(typeof(ConceitAbility));
            ConceitSkill.activationStateMachineName = "Weapon";
            ConceitSkill.beginSkillCooldownOnSkillEnd = false;
            ConceitSkill.canceledFromSprinting = true;
            ConceitSkill.cancelSprintingOnActivation = true;
            ConceitSkill.fullRestockOnAssign = true;
            ConceitSkill.interruptPriority = InterruptPriority.Any;
            ConceitSkill.isCombatSkill = true;
            ConceitSkill.mustKeyPress = false;
            ConceitSkill.baseMaxStock = 1;
            ConceitSkill.rechargeStock = 1;
            ConceitSkill.requiredStock = 1;
            ConceitSkill.stockToConsume = 1;
            ConceitSkill.baseRechargeInterval = 0;
            //ConceitSkill.attackSpeedBuffsRestockSpeed = true;
            //ConceitSkill.attackSpeedBuffsRestockSpeed_Multiplier = 0.5f;
            ConceitSkill.icon = Icon;
            ConceitSkill.skillDescriptionToken = "MEGALOMANIA_PRIMARY_DESCRIPTION";
            ConceitSkill.skillName = "Conceit";
            ConceitSkill.skillNameToken = "MEGALOMANIA_PRIMARY_NAME";

            ContentAddition.AddSkillDef(ConceitSkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = baseDuration / base.attackSpeedStat;
            this.burstDuration = baseBurstDuration / base.attackSpeedStat;
            shotsLeftInBurst = shotsPerBurst;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);

            //Chat.AddMessage("conceit fired");

            //Util.PlaySound(, base.gameObject);
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority && shotsLeftInBurst > 0 && this.fixedAge > this.burstDuration * (((double)shotsPerBurst - (double)shotsLeftInBurst) / (double)shotsPerBurst))
            {
                Ray aimRay = base.GetAimRay();
                ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction, Vector3.up), base.gameObject, damageStat * damageCoefficient, force, RollCrit());
                shotsLeftInBurst -= 1;
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
