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
    public class TwinShotAbility : BaseSkillState
    {
        public static float baseDuration = 2f;
        public static float burstShotsPerSecond = 6f;
        public static int shotsPerBurst = 6;
        public static float baseBurstDuration = shotsPerBurst / burstShotsPerSecond;
        private float burstDuration;
        private int shotsFiredInBurst = 0;
        private float duration;
        public static SkillDef TwinShotSkill;
        static float damageCoefficient = 1.8f;
        static float force = 0.2f;
        public static GameObject projectilePrefab;
        public static GameObject muzzleFlashPrefab;
        public static float spreadBloomValue = 0.3f;

        public static void initEgoTwinShot(Sprite Icon)
        {
            projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarGolemTwinShotProjectile");
            muzzleFlashPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MuzzleFlashes/MuzzleflashLunarGolemTwinShot");


            TwinShotSkill = ScriptableObject.CreateInstance<EgoSkillDef>();

            TwinShotSkill.activationState = new SerializableEntityStateType(typeof(TwinShotAbility));
            TwinShotSkill.activationStateMachineName = "Weapon";
            TwinShotSkill.canceledFromSprinting = false;
            TwinShotSkill.cancelSprintingOnActivation = true;
            TwinShotSkill.fullRestockOnAssign = false;
            TwinShotSkill.isCombatSkill = true;
            TwinShotSkill.mustKeyPress = false;
            TwinShotSkill.baseMaxStock = 1;
            TwinShotSkill.rechargeStock = 1;
            TwinShotSkill.requiredStock = 1;
            TwinShotSkill.stockToConsume = 1;
            TwinShotSkill.baseRechargeInterval = 6;
            TwinShotSkill.interruptPriority = InterruptPriority.Skill;
            TwinShotSkill.autoHandleLuminousShot = true;
            TwinShotSkill.beginSkillCooldownOnSkillEnd = true;
            TwinShotSkill.icon = Icon;
            TwinShotSkill.skillDescriptionToken = "MEGALOMANIA_TWINSHOT_DESCRIPTION";
            TwinShotSkill.skillName = "Twin Shot";
            TwinShotSkill.skillNameToken = "MEGALOMANIA_TWINSHOT_NAME";

            ContentAddition.AddSkillDef(TwinShotSkill);
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

            if ((bool)muzzleFlashPrefab)
            {
                EffectData effectData = new EffectData
                {
                    origin = aimRay.origin + aimRay.direction,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction)
                };
                //effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
                EffectManager.SpawnEffect(muzzleFlashPrefab, effectData, false);
                //SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, "MuzzleLaser", transmit: false);
            }
            if ((bool)base.characterBody)
            {
                base.characterBody.AddSpreadBloom(spreadBloomValue);
            }
            //TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, projectilePrefab, base.gameObject, 50);
            Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 1f, 15f, 2f);
            Util.PlaySound("Play_lunar_golem_attack1_launch", gameObject);//, attackSpeedStat);
            ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward, (shotsFiredInBurst % 2 == 0) ? Vector3.up : Vector3.down), gameObject, damageStat * damageCoefficient, force, RollCrit());
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
