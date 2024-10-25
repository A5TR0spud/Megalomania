using EntityStates.Toolbot;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static UnityEngine.ParticleSystem.PlaybackState;
using UnityEngine;
using EntityStates;

namespace MegalomaniaPlugin.Skills.MinigunAbility
{
    internal class BaseMinigunState : BaseSkillState
    {
        public static float damageCoefficient = 0.8f; //less dps than nailgun (.84)
        public static float force = 0.2f;
        public static float procCoefficient = 0.8f; //more procs/s than nailgun (.72)
        public static float maxDistance = 300;//300
        public static GameObject muzzleFlashPrefab;
        public static GameObject hitEffectPrefab;
        public static GameObject tracerEffectPrefab;
        public static bool loaded = false;
        public static float spreadBloomValue = 0.15f;

        public static void loadPrefabs()
        {
            muzzleFlashPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/LunarWispMinigunHitspark");
            hitEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/LunarWispMinigunHitspark");
            tracerEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/TracerLunarWispMinigun");
            loaded = (bool)muzzleFlashPrefab && (bool)hitEffectPrefab && (bool)tracerEffectPrefab;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            if (!loaded)
            {
                loadPrefabs();
            }
        }

        protected void FireBullet(Ray aimRay, int bulletCount, float spreadPitchScale, float spreadYawScale)
        {
            StartAimMode(aimRay, 3f);
            if (base.isAuthority)
            {
                BulletAttack bulletAttack = new BulletAttack();
                bulletAttack.aimVector = aimRay.direction;
                bulletAttack.origin = aimRay.origin;
                bulletAttack.owner = base.gameObject;
                bulletAttack.weapon = null;
                bulletAttack.bulletCount = (uint)bulletCount;
                bulletAttack.damage = base.damageStat * damageCoefficient;
                bulletAttack.damageColorIndex = DamageColorIndex.Default;
                bulletAttack.damageType = DamageType.Generic;
                bulletAttack.falloffModel = BulletAttack.FalloffModel.DefaultBullet;
                bulletAttack.force = force;
                bulletAttack.HitEffectNormal = false;
                bulletAttack.procChainMask = default(ProcChainMask);
                bulletAttack.procCoefficient = procCoefficient;
                bulletAttack.maxDistance = maxDistance;
                bulletAttack.radius = 0f;
                bulletAttack.isCrit = RollCrit();
                //bulletAttack.muzzleName = base.characterDirection;
                bulletAttack.stopperMask = LayerIndex.CommonMasks.bullet;
                bulletAttack.smartCollision = false;
                bulletAttack.minSpread = 0f;
                bulletAttack.hitEffectPrefab = hitEffectPrefab;
                bulletAttack.maxSpread = 2f;
                bulletAttack.smartCollision = false;
                bulletAttack.sniper = false;
                bulletAttack.spreadPitchScale = spreadPitchScale;
                bulletAttack.spreadYawScale = spreadYawScale;
                bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
                bulletAttack.allowTrajectoryAimAssist = false;
                bulletAttack.Fire();
            }
            if ((bool)base.characterBody)
            {
                base.characterBody.AddSpreadBloom(spreadBloomValue);
            }
            Util.PlaySound("Play_lunar_wisp_attack1_shoot_bullet", base.gameObject);
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
            //EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
        }
    }
}
