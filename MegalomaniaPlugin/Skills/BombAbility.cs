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
    public class BombAbility : BaseSkillState
    {
        public static readonly float baseDuration = 0.5f;
        private float duration;
        public static SkillDef BombSkill;
        static readonly float damageCoefficient = 4.8f;
        static readonly float force = 1f;
        public static GameObject projectilePrefab;
        public static GameObject muzzleFlashPrefab;

        public static void initBombAbility(Sprite Icon)
        {
            projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarWispTrackingBomb");
            muzzleFlashPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MuzzleFlashes/MuzzleflashLunarShard");


            BombSkill = ScriptableObject.CreateInstance<EgoSkillDef>();

            BombSkill.activationState = new SerializableEntityStateType(typeof(BombAbility));
            BombSkill.activationStateMachineName = "Weapon";
            BombSkill.canceledFromSprinting = false;
            BombSkill.cancelSprintingOnActivation = true;
            BombSkill.fullRestockOnAssign = true;
            BombSkill.interruptPriority = InterruptPriority.Any;
            BombSkill.isCombatSkill = true;
            BombSkill.mustKeyPress = false;
            BombSkill.baseMaxStock = 1;
            BombSkill.rechargeStock = 1;
            BombSkill.requiredStock = 1;
            BombSkill.stockToConsume = 1;
            BombSkill.baseRechargeInterval = 3;
            BombSkill.interruptPriority = InterruptPriority.Skill;
            BombSkill.autoHandleLuminousShot = true;
            BombSkill.icon = Icon;
            BombSkill.skillDescriptionToken = "MEGALOMANIA_BOMB_DESCRIPTION";
            BombSkill.skillName = "Chimera Bomb";
            BombSkill.skillNameToken = "MEGALOMANIA_BOMB_NAME";

            ContentAddition.AddSkillDef(BombSkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            Ray aimRay = GetAimRay();
            StartAimMode(aimRay, 2f, false);
            fire();

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
            //TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, projectilePrefab, base.gameObject, 50);
            Util.PlaySound("Play_lunar_exploder_m1_fire", gameObject);//, attackSpeedStat);
            ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin + aimRay.direction * 1.0f, Util.QuaternionSafeLookRotation(aimRay.direction), gameObject, damageStat * damageCoefficient, force, RollCrit());
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
