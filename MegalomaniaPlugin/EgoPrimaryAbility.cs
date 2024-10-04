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

namespace MegalomaniaPlugin
{
    public class EgoPrimaryAbility : BaseSkillState
    {
        public float baseDuration = 0.3333f;
        private float duration;
        public static SkillDef EgoPrimarySkill;
        private readonly BullseyeSearch search = new BullseyeSearch();
        float maxTrackingDistance = float.MaxValue;
        float maxTrackingAngle = 30f;

        public static void initEgoPrimary(Sprite Icon)
        {
            EgoPrimarySkill = ScriptableObject.CreateInstance<EgoPrimarySkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            EgoPrimarySkill.activationState = new SerializableEntityStateType(typeof(EgoPrimaryAbility));
            EgoPrimarySkill.activationStateMachineName = "Weapon";
            EgoPrimarySkill.beginSkillCooldownOnSkillEnd = false;
            EgoPrimarySkill.canceledFromSprinting = true;
            EgoPrimarySkill.cancelSprintingOnActivation = true;
            EgoPrimarySkill.fullRestockOnAssign = false;
            EgoPrimarySkill.interruptPriority = InterruptPriority.Any;
            EgoPrimarySkill.isCombatSkill = true;
            EgoPrimarySkill.mustKeyPress = false;
            EgoPrimarySkill.baseMaxStock = 3;
            EgoPrimarySkill.rechargeStock = 1;
            EgoPrimarySkill.requiredStock = 1;
            EgoPrimarySkill.stockToConsume = 1;
            EgoPrimarySkill.baseRechargeInterval = 1f;
            EgoPrimarySkill.attackSpeedBuffsRestockSpeed = true;
            EgoPrimarySkill.attackSpeedBuffsRestockSpeed_Multiplier = 1.0f;
            // For the skill icon, you will have to load a Sprite from your own AssetBundle
            EgoPrimarySkill.icon = Icon;
            EgoPrimarySkill.skillDescriptionToken = "MEGALOMANIA_PRIMARY_DESCRIPTION";
            EgoPrimarySkill.skillName = "MEGALOMANIA_PRIMARY_NAME";
            EgoPrimarySkill.skillNameToken = "MEGALOMANIA_PRIMARY_NAME";

            // This adds our skilldef. If you don't do this, the skill will not work.
            ContentAddition.AddSkillDef(EgoPrimarySkill);
        }

        public override void OnEnter()
        {
            base.OnEnter();
            /*this.duration = this.baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);

            //Chat.AddMessage("conceit fired");

            //Util.PlaySound(, base.gameObject);

            if (base.isAuthority)
            {
                CharacterBody characterBody = this.characterBody;
                CharacterMaster master = characterBody.master;
                //FireEgoOrb(characterBody.aimOrigin, getTrackingTarget(aimRay));
                
                if (master.GetDeployableCount(DeployableSlot.LunarSunBomb) > 0)
                {
                    List<DeployableInfo> list = master.deployablesList;
                    foreach (DeployableInfo info in list)
                    {
                        if (info.slot == DeployableSlot.LunarSunBomb)
                        {
                            ProjectileSphereTargetFinder targetFinder = info.deployable.gameObject.GetComponent<ProjectileSphereTargetFinder>();
                            if (!(bool)targetFinder)
                                continue;
                            if (targetFinder.hasTarget)
                                continue;

                            //targetFinder.lookRange = float.MaxValue;
                            targetFinder.SetTarget(getTrackingTarget(aimRay));
                            //Chat.AddMessage("conceit supposed to increase range");
                            break;
                        }
                    }
                }
            }*/
        }

        /*protected virtual GenericDamageOrb CreateEgoOrb()
        {
            return new EgomaniaOrb();
        }

        private void FireEgoOrb(Vector3 origin, HurtBox target)
        {
            if (NetworkServer.active)
            {
                GenericDamageOrb genericDamageOrb = CreateEgoOrb();
                genericDamageOrb.damageValue = base.characterBody.damage * 3.6f;
                genericDamageOrb.isCrit = base.characterBody.RollCrit();
                genericDamageOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
                genericDamageOrb.attacker = base.gameObject;
                genericDamageOrb.procCoefficient = 1;
                HurtBox hurtBox = target;
                if ((bool)hurtBox)
                {
                    //Transform transform = childLocator.FindChild(muzzleString);
                    //EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
                    genericDamageOrb.origin = origin;
                    genericDamageOrb.target = hurtBox;
                    OrbManager.instance.AddOrb(genericDamageOrb);
                }
            }
        }*/

        

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
