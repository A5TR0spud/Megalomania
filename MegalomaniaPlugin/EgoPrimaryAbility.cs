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
    public class EgoPrimaryAbility : BaseSkillState
    {
        public static float baseDuration = 0.3333f;
        private float duration;
        public static SkillDef EgoPrimarySkill;
        static float damageCoefficient = 3.6f;
        static float force = 0.1f;
        public static GameObject projectilePrefab;

        public static void initEgoPrimary(Sprite Icon)
        {
            //LunarGolemTwinShotProjectile
            //LunarWispTrackingBomb
            //LunarExploderShardProjectile
            projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarExploderShardProjectile");


            EgoPrimarySkill = ScriptableObject.CreateInstance<EgoPrimarySkillDef>();

            EgoPrimarySkill.activationState = new SerializableEntityStateType(typeof(EgoPrimaryAbility));
            EgoPrimarySkill.activationStateMachineName = "Weapon";
            EgoPrimarySkill.beginSkillCooldownOnSkillEnd = false;
            EgoPrimarySkill.canceledFromSprinting = true;
            EgoPrimarySkill.cancelSprintingOnActivation = true;
            EgoPrimarySkill.fullRestockOnAssign = true;
            EgoPrimarySkill.interruptPriority = InterruptPriority.Any;
            EgoPrimarySkill.isCombatSkill = true;
            EgoPrimarySkill.mustKeyPress = false;
            EgoPrimarySkill.baseMaxStock = 1;
            EgoPrimarySkill.rechargeStock = 1;
            EgoPrimarySkill.requiredStock = 1;
            EgoPrimarySkill.stockToConsume = 1;
            EgoPrimarySkill.baseRechargeInterval = 0f;
            //EgoPrimarySkill.attackSpeedBuffsRestockSpeed = true;
            //EgoPrimarySkill.attackSpeedBuffsRestockSpeed_Multiplier = 1.0f;
            EgoPrimarySkill.icon = Icon;
            EgoPrimarySkill.skillDescriptionToken = "MEGALOMANIA_PRIMARY_DESCRIPTION";
            EgoPrimarySkill.skillName = "MEGALOMANIA_PRIMARY_NAME";
            EgoPrimarySkill.skillNameToken = "MEGALOMANIA_PRIMARY_NAME";

            ContentAddition.AddSkillDef(EgoPrimarySkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);

            //Chat.AddMessage("conceit fired");

            //Util.PlaySound(, base.gameObject);

            if (base.isAuthority)
            {
                ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction, Vector3.up), base.gameObject, damageStat * damageCoefficient, force, RollCrit());
                //ProjectileManager.instance.FireProjectile(projectilePrefab, base.characterBody.aimOrigin, Util.QuaternionSafeLookRotation(aimRay.direction, Vector3.down), base.gameObject, damageStat * damageCoefficient, force, RollCrit());
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
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
