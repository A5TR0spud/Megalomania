using EntityStates;
using JetBrains.Annotations;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MegalomaniaPlugin.Skills
{
    internal class EgoSkillDef : SkillDef
    {
        public bool requiresBombs = false;
        public override bool IsReady([NotNull] GenericSkill skillSlot)
        {
            if (!requiresBombs) {
                return base.IsReady(skillSlot);
            }
            int ego = skillSlot.characterBody.master.GetDeployableCount(DeployableSlot.LunarSunBomb);
            return base.IsReady(skillSlot) && ego > 0;
        }

        public override bool CanExecute([NotNull] GenericSkill skillSlot)
        {
            if (!requiresBombs)
            {
                return base.CanExecute(skillSlot);
            }
            int ego = skillSlot.characterBody.master.GetDeployableCount(DeployableSlot.LunarSunBomb);
            return base.CanExecute(skillSlot) && ego > 0;
        }

        public Deployable GetFreeLunarSunBomb(CharacterBody body)
        {
            int ego = body.master.GetDeployableCount(DeployableSlot.LunarSunBomb);
            if (ego > 0)
            {
                List<DeployableInfo> deps = body.master.deployablesList;
                foreach (DeployableInfo info in deps)
                {
                    if (info.slot == DeployableSlot.LunarSunBomb)
                    {
                        ProjectileSphereTargetFinder targetFinder = info.deployable.gameObject.GetComponent<ProjectileSphereTargetFinder>();
                        if (!(bool)targetFinder)
                            continue;
                        if (targetFinder.hasTarget)
                            continue;
                        return info.deployable;
                    }
                }
            }
            return null;
        }
    }
}
