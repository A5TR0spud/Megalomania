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
    public class ConceitAbility : BaseSkillState
    {
        public static float baseDuration = 1f;
        public static float burstShotsPerSecond = 8f;
        public static int shotsPerBurst = 3;
        public static float baseBurstDuration = shotsPerBurst / burstShotsPerSecond;
        private float burstDuration;
        private int shotsFiredInBurst = 0;
        private float duration;
        public static SkillDef ConceitSkill;
        static float damageCoefficient = 0.6f;
        static float force = 0.1f;
        public static GameObject projectilePrefab;
        //public static GameObject muzzleFlashPrefab;

        public static void initEgoPrimary(Sprite Icon)
        {
            //LunarGolemTwinShotProjectile
            //LunarWispTrackingBomb
            //LunarExploderShardProjectile
            projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarExploderShardProjectile");
            //muzzleFlashPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MuzzleFlashes/MuzzleflashLunarShard");


            ConceitSkill = ScriptableObject.CreateInstance<EgoSkillDef>();

            ConceitSkill.activationState = new SerializableEntityStateType(typeof(ConceitAbility));
            ConceitSkill.activationStateMachineName = "Weapon";
            ConceitSkill.canceledFromSprinting = false;
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
            ConceitSkill.interruptPriority = InterruptPriority.Any;
            ConceitSkill.autoHandleLuminousShot = true;
            ConceitSkill.icon = Icon;
            ConceitSkill.skillDescriptionToken = "MEGALOMANIA_PRIMARY_DESCRIPTION";
            ConceitSkill.skillName = "Conceit";
            ConceitSkill.skillNameToken = "MEGALOMANIA_PRIMARY_NAME";

            ContentAddition.AddSkillDef(ConceitSkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            burstDuration = baseBurstDuration / attackSpeedStat;
            shotsFiredInBurst = 0;
            Ray aimRay = GetAimRay();
            StartAimMode(aimRay, 2f, false);

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
            int num = Mathf.FloorToInt(fixedAge / burstDuration * shotsPerBurst);
            if (shotsFiredInBurst <= num && shotsFiredInBurst < shotsPerBurst)
            {
                fire();

                shotsFiredInBurst++;
            }
            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        public void fire()
        {
            Ray aimRay = GetAimRay();

            /*if ((bool)muzzleFlashPrefab)
            {
                EffectData effectData = new EffectData
                {
                    origin = aimRay.origin + aimRay.direction,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction)
                };
                //effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
                EffectManager.SpawnEffect(muzzleFlashPrefab, effectData, false);
                //SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, "MuzzleLaser", transmit: false);
            }*/
            //TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, projectilePrefab, base.gameObject, 50);
            Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 1f, 1f, 0.5f);
            Util.PlaySound("Play_lunar_exploder_m1_fire", gameObject);//, attackSpeedStat);
            ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), gameObject, damageStat * damageCoefficient, force, RollCrit(), speedOverride: 150);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
