using RoR2.Projectile;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API.Utils;
using System.Linq;
using MegalomaniaPlugin.Utilities;
using R2API;
using UnityEngine.Networking;
using System.Collections;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using Random = UnityEngine.Random;

namespace MegalomaniaPlugin.Items
{
    public class MegalomaniaEgoBehavior
    {
        private readonly BullseyeSearch search = new BullseyeSearch();
        public static ModdedProcType LunarSunBombProc = ProcTypeAPI.ReserveProcType();
        private static GameObject projectilePrefab;
        private static float baseOrbitRadius = 2f;
        private static float orbitRadiusPerStack = 0.25f;
        private static float maxInclinationDegrees = 0f;
        private static bool limitSpeedReduction = false;
        private static bool preferOuter = false;
        private static bool inclinationScalesWithDistance = false;
        private static bool capRadius = false;
        private static float capRadiusValue = 0f;

        public void init()
        {
            if (MegalomaniaPlugin.ConfigBombFocused.Value != Utils.BombDensity.normal)
            {
                if (MegalomaniaPlugin.ConfigBombFocused.Value == Utils.BombDensity.oort_cloud)
                {
                    baseOrbitRadius = 2.5f;
                    orbitRadiusPerStack = 0.3f;
                    maxInclinationDegrees = 90f;
                    limitSpeedReduction = true;
                    preferOuter = true;
                    capRadius = true;
                    capRadiusValue = 30f;
                }
                else if (MegalomaniaPlugin.ConfigBombFocused.Value == Utils.BombDensity.asteroid_belt)
                {
                    baseOrbitRadius = 1.9f;
                    orbitRadiusPerStack = 0.1f;
                    maxInclinationDegrees = 15f;
                    inclinationScalesWithDistance = true;
                    capRadius = true;
                    capRadiusValue = 5f;
                }

                On.RoR2.LunarSunBehavior.InitializeOrbiter += LunarSunBehavior_InitializeOrbiter;
            }


            On.RoR2.LunarSunBehavior.FixedUpdate += LunarSunBehavior_FixedUpdate;
            On.RoR2.LunarSunBehavior.GetMaxProjectiles += LunarSunBehavior_GetMaxProjectiles;

            if (MegalomaniaPlugin.ConfigPrimaryEnhancement.Value)
            {
                On.RoR2.LunarSunBehavior.OnEnable += LunarSunBehavior_OnEnable;
                On.RoR2.LunarSunBehavior.OnDisable += LunarSunBehavior_OnDisable;
            }

            On.RoR2.GlobalEventManager.ProcessHitEnemy += GlobalEventManager_ProcessHitEnemy;
        }

        private void LunarSunBehavior_InitializeOrbiter(On.RoR2.LunarSunBehavior.orig_InitializeOrbiter orig, LunarSunBehavior self, ProjectileOwnerOrbiter orbiter, LunarSunProjectileController controller)
        {
            float ttrpg = orbitRadiusPerStack * (float)self.stack;
            if (capRadius)
            {
                float t = capRadiusValue - baseOrbitRadius;
                ttrpg = Mathf.Min(ttrpg, t);
            }
            float stackingBonusRandomOrbitAdder = Random.Range(orbitRadiusPerStack, ttrpg);
            if (preferOuter) //roll with advantage
            {
                float other = Random.Range(orbitRadiusPerStack, ttrpg);
                if (other > stackingBonusRandomOrbitAdder) stackingBonusRandomOrbitAdder = other;
            }
            float orbitRadius = self.body.radius + baseOrbitRadius + stackingBonusRandomOrbitAdder;

            float num2 = 0;
            if (limitSpeedReduction)
            {
                num2 = orbitRadius;
            }
            else
            {
                num2 = orbitRadius / 2f;
                num2 *= num2;
            }
            float distanceScalar = Mathf.Pow(0.9f, num2);
            float maxIncline = maxInclinationDegrees;
            if (inclinationScalesWithDistance)
            {
                maxIncline *= 1f - distanceScalar;
            }
            float degreesPerSecond = 180f * distanceScalar;
            Vector3 inclinationVector = Quaternion.AngleAxis(Random.Range(0f, maxIncline), Vector3.forward) * Vector3.up;
            Vector3 rotatedIncVec = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * inclinationVector;
            //It seems like planeNormal is always changed to be on only 1 axis. This sucks, and I hate it.
            //I've tried several methods of finding a planeNormal but none actually rotate it properly.
            //If I want this fixed, I'd have to hook into ProjectileOwnerOrbity, which uses networking. I hate networking.
            //So, for now, it will remain borked but passable.
            Vector3 planeNormal = rotatedIncVec;
            float initialDegreesFromOwnerForward = Random.Range(0f, 360f);
            orbiter.Initialize(planeNormal, orbitRadius, degreesPerSecond, initialDegreesFromOwnerForward);
            self.onDisabled += DestroyOrbiter;
            void DestroyOrbiter(LunarSunBehavior lunarSunBehavior)
            {
                if ((bool)controller)
                {
                    controller.Detonate();
                }
            }
        }

        private void GlobalEventManager_ProcessHitEnemy(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            handleEgoProc(damageInfo, victim);
        }

        private static void handleEgoProc(DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo == null)
                return;
            if (victim == null || !(bool)victim)
                return;

            //copied from base
            if (damageInfo.procCoefficient == 0f || damageInfo.rejected || !NetworkServer.active)
            {
                return;
            }
            if (damageInfo.attacker == null || !damageInfo.attacker || !(damageInfo.procCoefficient > 0f))
            {
                return;
            }

            ProcChainMask procChainMask = damageInfo.procChainMask;

            //further tests for on hit bomb attack
            //this is such bad... this is not good code

            CharacterMaster master = null;
            int egoCount = 0;

            if (MegalomaniaPlugin.ConfigOnHitBombAttack.Value == Utils.OnHitBombAttackType.none
                || procChainMask.HasModdedProc(LunarSunBombProc))
                return;
            CharacterBody attackerBody = null;
            GameObject a1 = damageInfo.attacker;

            if (a1 != null && (bool)a1)
                attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            else
                return;
            if (attackerBody == null || !(bool)attackerBody)
                return;
            master = attackerBody.master;
            if (master == null || !(bool)master)
                return;
            Inventory inventory = attackerBody.inventory;
            if (inventory == null || !(bool)inventory)
                inventory = master.inventory;
            if ((bool)inventory)
                egoCount = inventory.GetItemCount(DLC1Content.Items.LunarSun);
            else
                return;
            if (egoCount < 1)
                return;


            CharacterBody victimBody = (victim ? victim.GetComponent<CharacterBody>() : null);
            HurtBox target = null;
            if (victimBody != null && (bool)victimBody)
                target = victimBody.mainHurtBox;
            if (target == null || !(bool)target)
                return;
            //letsago
            bool doTarget = false;
            if (MegalomaniaPlugin.ConfigOnHitBombAttack.Value == Utils.OnHitBombAttackType.proc
                && Util.CheckRoll(100f * damageInfo.procCoefficient, master.luck, master))
            {
                procChainMask.AddModdedProc(LunarSunBombProc);
                doTarget = true;
            }
            else if (MegalomaniaPlugin.ConfigOnHitBombAttack.Value == Utils.OnHitBombAttackType.create
                && Util.CheckRoll(30f * damageInfo.procCoefficient, master.luck, master))
            {
                procChainMask.AddModdedProc(LunarSunBombProc);
                if (!master.IsDeployableLimited(DeployableSlot.LunarSunBomb))
                {
                    FireProjectileInfo bombInfo = createBombInfo(attackerBody, egoCount);
                    bombInfo.procChainMask = procChainMask;
                    setupBombGen(egoCount, false);
                    ProjectileManager.instance.FireProjectile(bombInfo);
                }
                doTarget = true;
            }

            if (doTarget)
            {
                List<DeployableInfo> list = attackerBody.master.deployablesList;
                if (list == null)
                    return;
                foreach (DeployableInfo info in list)
                {
                    if (info.slot == DeployableSlot.LunarSunBomb)
                    {
                        ProjectileSphereTargetFinder targetFinder = info.deployable.gameObject.GetComponent<ProjectileSphereTargetFinder>();
                        if (targetFinder == null)
                            continue;
                        if (!(bool)targetFinder)
                            continue;
                        if (targetFinder.hasTarget)
                            continue;
                        targetFinder.SetTarget(target);
                        targetFinder.onlySearchIfNoTarget = true;
                        targetFinder.testLoS = false;
                        break;
                    }
                }
            }
        }

        private static void setupBombGen(int stack, bool isFromTime)
        {
            ProjectileSphereTargetFinder targetFinder = projectilePrefab.GetComponent<ProjectileSphereTargetFinder>();
            if (targetFinder)
            {
                targetFinder.lookRange = (float)(MegalomaniaPlugin.ConfigBombRange.Value + MegalomaniaPlugin.ConfigBombStackingRange.Value * (stack - 1));
                
                if (!MegalomaniaPlugin.ConfigPassiveBombAttack.Value)
                {
                    //passive attack is disabled...
                    if (isFromTime)
                    {
                        targetFinder.lookRange = 0.0f; //only check no radius
                    }
                }
            }
            else
                Log.Error("LunarSunBehavior: Unable to modify projectile Range (ProjectileSphereTargetFinder component not found)");
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

        private static void LunarSunBehavior_FixedUpdate(On.RoR2.LunarSunBehavior.orig_FixedUpdate orig, LunarSunBehavior self)
        {
            //Grab private variables first, makes the code readable
            CharacterBody body = self.GetFieldValue<CharacterBody>("body");
            GameObject projectilePrefab1 = self.GetFieldValue<GameObject>("projectilePrefab");
            if ((bool)projectilePrefab1) projectilePrefab = projectilePrefab1;
            int stack = self.GetFieldValue<int>("stack");
            float projectileTimer = self.GetFieldValue<float>("projectileTimer");
            float transformTimer = self.GetFieldValue<float>("transformTimer");
            Xoroshiro128Plus transformRng = self.GetFieldValue<Xoroshiro128Plus>("transformRng");
            projectileTimer += Time.fixedDeltaTime;

            if (projectilePrefab != null && (bool)projectilePrefab && MegalomaniaPlugin.ConfigEnableBombs.Value)
                handleBombs(body, ref projectileTimer, stack, projectilePrefab);

            if (MegalomaniaPlugin.ConfigTransformTime.Value >= 0)
                handleTransUpdate(body, ref transformTimer, stack, transformRng);

            self.SetFieldValue("projectileTimer", projectileTimer);
            self.SetFieldValue("transformTimer", transformTimer);
        }

        private static int LunarSunBehavior_GetMaxProjectiles(On.RoR2.LunarSunBehavior.orig_GetMaxProjectiles orig, Inventory inventory)
        {
            return (int)(MegalomaniaPlugin.ConfigBombCap.Value + (inventory.GetItemCount(DLC1Content.Items.LunarSun) - 1) * MegalomaniaPlugin.ConfigBombStackingCap.Value);
        }

        private static void handleBombs(CharacterBody body, ref float projectileTimer, int stack, GameObject projectilePrefab)
        {
            if (body == null || !(bool)body)
                return;
            if (body.master == null || !(bool)body.master)
                return;
            if (projectilePrefab == null || !(bool)projectilePrefab)
                return;

            float denominator = (stack - 1) * (float)MegalomaniaPlugin.ConfigBombCreationStackingMultiplier.Value + 1;
            if (!body.master.IsDeployableLimited(DeployableSlot.LunarSunBomb) &&
                projectileTimer > MegalomaniaPlugin.ConfigBombCreationRate.Value / denominator + MegalomaniaPlugin.ConfigBombCreationStackingAdder.Value * stack)
            {
                projectileTimer = 0f;

                setupBombGen(stack, true);

                FireProjectileInfo bombInfo = createBombInfo(body, stack);
                ProjectileManager.instance.FireProjectile(bombInfo);
            }
        }

        private static FireProjectileInfo createBombInfo(CharacterBody owner, int? stack = null)
        {
            if (stack == null || stack < 1)
            {
                stack = owner.inventory.GetItemCount(DLC1Content.Items.LunarSun);
            }
            if (stack < 1)
            {
                stack = 1;
            }
            FireProjectileInfo fireProjectileInfo = default;
            fireProjectileInfo.projectilePrefab = projectilePrefab;
            fireProjectileInfo.crit = owner.RollCrit();
            fireProjectileInfo.damage = owner.damage * (float)(MegalomaniaPlugin.ConfigBombDamage.Value + MegalomaniaPlugin.ConfigBombStackingDamage.Value * stack);
            fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
            fireProjectileInfo.force = 0f;
            fireProjectileInfo.owner = owner.gameObject;
            fireProjectileInfo.position = owner.transform.position;
            fireProjectileInfo.rotation = UnityEngine.Quaternion.identity;
            if (MegalomaniaPlugin.ConfigBombStacking.Value)
            {
                owner.statsDirty = true;
                //DamageInfo damageInfo = new DamageInfo();
                //damageInfo.damage = (float)ConfigMaxHealthPerStack.Value;
                //damageInfo.damageType = DamageType.Silent;
                //body.healthComponent.TakeDamage(damageInfo);
            }

            ProcChainMask procChainMask = default(ProcChainMask);
            procChainMask.AddModdedProc(LunarSunBombProc);

            fireProjectileInfo.procChainMask = procChainMask;
            return fireProjectileInfo;
        }

        private static void handleTransUpdate(CharacterBody body, ref float transformTimer, int stack, Xoroshiro128Plus transformRng)
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

            Utils.TransformItems(body.inventory, 1, transformRng, body.master);
        }
    }
}
