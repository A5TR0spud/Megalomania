using EntityStates;
using JetBrains.Annotations;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace MegalomaniaPlugin
{
    internal class EgoPrimarySkillDef : SkillDef
    {
        public override bool IsReady([NotNull] GenericSkill skillSlot)
        {
            int ego = skillSlot.characterBody.master.GetDeployableCount(DeployableSlot.LunarSunBomb);
            return base.IsReady(skillSlot) && ego > 0;
        }

        public override bool CanExecute([NotNull] GenericSkill skillSlot)
        {
            int ego = skillSlot.characterBody.master.GetDeployableCount(DeployableSlot.LunarSunBomb);
            return base.CanExecute(skillSlot) && ego > 0;
        }
    }
}
