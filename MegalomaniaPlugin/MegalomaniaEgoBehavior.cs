using RoR2.Projectile;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API.Utils;
using System.Linq;
using System.Numerics;

namespace MegalomaniaPlugin
{
    public class MegalomaniaEgoBehavior
    {
        Utils utils { get; set; }
        private readonly BullseyeSearch search = new BullseyeSearch();

        public void init(Utils ut)
        {
            utils = ut;
            On.RoR2.LunarSunBehavior.FixedUpdate += LunarSunBehavior_FixedUpdate;
            On.RoR2.LunarSunBehavior.GetMaxProjectiles += LunarSunBehavior_GetMaxProjectiles;

            if (MegalomaniaPlugin.ConfigPrimaryEnhancement.Value)
            {
                On.RoR2.LunarSunBehavior.OnEnable += LunarSunBehavior_OnEnable;
                On.RoR2.LunarSunBehavior.OnDisable += LunarSunBehavior_OnDisable;
            }
        }

        private void LunarSunBehavior_OnEnable(On.RoR2.LunarSunBehavior.orig_OnEnable orig, LunarSunBehavior self)
        {
            orig(self);
            CharacterBody body = self.GetFieldValue<CharacterBody>("body");
            if ((bool)body)
            {
                body.onSkillActivatedServer += OnSkillActivated;
            }
        }

        private void OnSkillActivated(GenericSkill skill)
        {
            CharacterBody body = skill.characterBody;
            SkillLocator skillLocator = body.GetComponent<SkillLocator>();
            if ((object)skillLocator?.primary == skill && body.master.GetDeployableCount(DeployableSlot.LunarSunBomb) > 0)
            {
                HurlBomb(body);
            }
        }

        private void HurlBomb(CharacterBody body)
        {
            Ray aimRay = GetAimRay(body);

            List<DeployableInfo> list = body.master.deployablesList;
            foreach (DeployableInfo info in list)
            {
                if (info.slot == DeployableSlot.LunarSunBomb)
                {
                    ProjectileSphereTargetFinder targetFinder = info.deployable.gameObject.GetComponent<ProjectileSphereTargetFinder>();
                    if (!(bool)targetFinder)
                        continue;
                    if (targetFinder.hasTarget)
                        continue;

                    targetFinder.SetTarget(getTrackingTarget(aimRay, body));
                    break;
                }
            }
        }

        public HurtBox getTrackingTarget(Ray aimRay, CharacterBody body)
        {
            search.teamMaskFilter = TeamMask.all;
            search.teamMaskFilter.RemoveTeam(body.teamComponent.teamIndex);
            search.filterByLoS = true;
            search.searchOrigin = aimRay.origin;
            search.searchDirection = aimRay.direction;
            search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
            search.maxDistanceFilter = float.PositiveInfinity;
            search.maxAngleFilter = 30;
            search.RefreshCandidates();
            search.FilterOutGameObject(body.gameObject);
            return search.GetResults().FirstOrDefault();
        }

        private Ray GetAimRay(CharacterBody body)
        {
            InputBankTest inputBank = body.GetComponent<InputBankTest>();
            if ((bool)inputBank)
            {
                return new Ray(inputBank.aimOrigin, inputBank.aimDirection);
            }
            return new Ray(body.transform.position, body.transform.forward);
        }

        private void LunarSunBehavior_OnDisable(On.RoR2.LunarSunBehavior.orig_OnDisable orig, LunarSunBehavior self)
        {
            orig(self);
            CharacterBody body = self.GetFieldValue<CharacterBody>("body");
            if ((bool)body)
            {
                body.onSkillActivatedServer -= OnSkillActivated;
            }
        }

        private void LunarSunBehavior_FixedUpdate(On.RoR2.LunarSunBehavior.orig_FixedUpdate orig, LunarSunBehavior self)
        {
            //Grab private variables first, makes the code readable
            CharacterBody body = self.GetFieldValue<CharacterBody>("body");
            GameObject projectilePrefab = self.GetFieldValue<GameObject>("projectilePrefab");
            int stack = self.GetFieldValue<int>("stack");
            float projectileTimer = self.GetFieldValue<float>("projectileTimer");
            float transformTimer = self.GetFieldValue<float>("transformTimer");
            Xoroshiro128Plus transformRng = self.GetFieldValue<Xoroshiro128Plus>("transformRng");
            projectileTimer += Time.fixedDeltaTime;

            if ((bool)projectilePrefab && projectilePrefab != null && MegalomaniaPlugin.ConfigEnableBombs.Value)
                handleBombs(body, ref projectileTimer, stack, projectilePrefab);

            if (MegalomaniaPlugin.ConfigTransformTime.Value >= 0)
                handleTransUpdate(body, ref transformTimer, stack, transformRng);

            self.SetFieldValue("projectileTimer", projectileTimer);
            self.SetFieldValue("transformTimer", transformTimer);
        }

        private int LunarSunBehavior_GetMaxProjectiles(On.RoR2.LunarSunBehavior.orig_GetMaxProjectiles orig, Inventory inventory)
        {
            return (int)(MegalomaniaPlugin.ConfigBombCap.Value + (inventory.GetItemCount(DLC1Content.Items.LunarSun) - 1) * MegalomaniaPlugin.ConfigBombStackingCap.Value);
        }

        private void handleBombs(CharacterBody body, ref float projectileTimer, int stack, GameObject projectilePrefab)
        {
            float denominator = (float)(stack - 1) * (float)MegalomaniaPlugin.ConfigBombCreationStackingMultiplier.Value + 1;
            if (!body.master.IsDeployableLimited(DeployableSlot.LunarSunBomb) &&
                projectileTimer > MegalomaniaPlugin.ConfigBombCreationRate.Value / denominator + MegalomaniaPlugin.ConfigBombCreationStackingAdder.Value * stack)
            {
                projectileTimer = 0f;

                ProjectileSphereTargetFinder targetFinder = projectilePrefab.GetComponent<ProjectileSphereTargetFinder>();
                if (targetFinder)
                {
                    if (MegalomaniaPlugin.ConfigPassiveBombAttack.Value)
                        targetFinder.lookRange = (float)(MegalomaniaPlugin.ConfigBombRange.Value + (MegalomaniaPlugin.ConfigBombStackingRange.Value * (stack - 1)));
                    else
                        targetFinder.lookRange = 0;
                }
                else
                    Log.Error("LunarSunBehavior: Unable to modify projectile Range (ProjectileSphereTargetFinder component not found)");

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = projectilePrefab;
                fireProjectileInfo.crit = body.RollCrit();
                fireProjectileInfo.damage = body.damage * (float)(MegalomaniaPlugin.ConfigBombDamage.Value + MegalomaniaPlugin.ConfigBombStackingDamage.Value * stack);
                fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
                fireProjectileInfo.force = 0f;
                fireProjectileInfo.owner = body.gameObject;
                fireProjectileInfo.position = body.transform.position;
                fireProjectileInfo.rotation = UnityEngine.Quaternion.identity;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                if (MegalomaniaPlugin.ConfigBombStacking.Value)
                {
                    body.statsDirty = true;
                    //DamageInfo damageInfo = new DamageInfo();
                    //damageInfo.damage = (float)ConfigMaxHealthPerStack.Value;
                    //damageInfo.damageType = DamageType.Silent;
                    //body.healthComponent.TakeDamage(damageInfo);
                }
            }
        }

        private void handleTransUpdate(CharacterBody body, ref float transformTimer, int stack, Xoroshiro128Plus transformRng)
        {
            //with acceptance
            transformTimer += Time.fixedDeltaTime;
            double calcTimer = MegalomaniaPlugin.ConfigTransformTime.Value * Math.Pow(MegalomaniaPlugin.ConfigTransformTimeDiminishing.Value, stack) + stack * MegalomaniaPlugin.ConfigTransformTimePerStack.Value;

            calcTimer = Math.Min(calcTimer, MegalomaniaPlugin.ConfigTransformTimeMax.Value);
            calcTimer = Math.Max(calcTimer, MegalomaniaPlugin.ConfigTransformTimeMin.Value);

            //if the timer's not up, stop
            if (transformTimer <= calcTimer)
            {
                return;
            }
            transformTimer = 0f;
            //if something is null, stop
            if (!body.master || !body.inventory)
            {
                return;
            }
            //if exceeds max items already converted, stop
            if (MegalomaniaPlugin.ConfigMaxTransformationsPerStage.Value > 0
                && body.inventory.GetItemCount(MegalomaniaPlugin.transformToken) >= MegalomaniaPlugin.ConfigMaxTransformationsPerStage.Value + (stack - 1) * MegalomaniaPlugin.ConfigMaxTransformationsPerStageStacking.Value)
            {
                return;
            }

            utils.TransformItems(body.inventory, 1, transformRng, body.master);
        }
    }
}
