using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MegalomaniaPlugin
{
    public class EgoPrimaryAbility : BaseSkillState
    {
        public float baseDuration = 0.1f;
        private float duration;
        public static SkillDef EgoPrimarySkill;
        private readonly BullseyeSearch search = new BullseyeSearch();
        float maxTrackingDistance = float.MaxValue;
        float maxTrackingAngle = 30f * 0.01745329f;

        public static void initEgoPrimary(Sprite Icon)
        {
            EgoPrimarySkill = ScriptableObject.CreateInstance<SkillDef>();

            //Check step 2 for the code of the CustomSkillsTutorial.MyEntityStates.SimpleBulletAttack class
            EgoPrimarySkill.activationState = new SerializableEntityStateType(typeof(EgoPrimaryAbility));
            EgoPrimarySkill.activationStateMachineName = "Weapon";
            EgoPrimarySkill.baseMaxStock = 1;
            EgoPrimarySkill.baseRechargeInterval = 0f;
            EgoPrimarySkill.beginSkillCooldownOnSkillEnd = true;
            EgoPrimarySkill.canceledFromSprinting = true;
            EgoPrimarySkill.cancelSprintingOnActivation = true;
            EgoPrimarySkill.fullRestockOnAssign = true;
            EgoPrimarySkill.interruptPriority = InterruptPriority.Any;
            EgoPrimarySkill.isCombatSkill = true;
            EgoPrimarySkill.mustKeyPress = false;
            EgoPrimarySkill.rechargeStock = 1;
            EgoPrimarySkill.requiredStock = 0;
            EgoPrimarySkill.stockToConsume = 1;
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
            this.duration = this.baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);

            Chat.AddMessage("conceit fired");

            //Util.PlaySound(, base.gameObject);

            if (base.isAuthority)
            {
                CharacterBody characterBody = this.characterBody;
                CharacterMaster master = characterBody.master;
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

                            targetFinder.lookRange = float.MaxValue;
                            targetFinder.SetTarget(getTrackingTarget(aimRay));
                            Chat.AddMessage("conceit supposed to increase range");
                            break;
                        }
                    }
                }
            }
        }

        public HurtBox getTrackingTarget(Ray aimRay)
        {
            search.teamMaskFilter = TeamMask.all;
            search.teamMaskFilter.RemoveTeam(teamComponent.teamIndex);
            search.filterByLoS = true;
            search.searchOrigin = aimRay.origin;
            search.searchDirection = aimRay.direction;
            search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
            search.maxDistanceFilter = maxTrackingDistance;
            search.maxAngleFilter = maxTrackingAngle;
            search.RefreshCandidates();
            search.FilterOutGameObject(base.gameObject);
            return search.GetResults().FirstOrDefault();
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
