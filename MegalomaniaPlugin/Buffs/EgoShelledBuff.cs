using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MegalomaniaPlugin.Buffs
{
    public class EgoShelledBuff
    {
        public static BuffDef EgoShellBuff;

        public static void initEgoShellBuff() {
            EgoShellBuff = ScriptableObject.CreateInstance<BuffDef>();
            //EgoShellBuff = RoR2Content.Buffs.LunarShell;

            //simply duplicate vanilla lunarshell icon and booleans
            EgoShellBuff.isDebuff = false;
            EgoShellBuff.isCooldown = false;
            EgoShellBuff.canStack = false;
            EgoShellBuff.ignoreGrowthNectar = false;
            EgoShellBuff.isHidden = false;
            EgoShellBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("Textures/BuffIcons/texBuffLunarShellIcon");
            EgoShellBuff.buffColor = new Color(0.38039216f, 0.6392157f, 0.9372549f, 255);
            EgoShellBuff.name = "megalomaniaShellBuff";
            //EgoShellBuff.hideFlags = HideFlags.None;

            ContentAddition.AddBuffDef(EgoShellBuff);

            On.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays;
        }

        private static void CharacterModel_UpdateOverlays(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig(self);

            Material lunarGolemShieldMaterial = self.GetFieldValue<Material>("lunarGolemShieldMaterial");
            int activeOverlayCount = self.GetFieldValue<int>("activeOverlayCount");
            Material[] currentOverlays = self.GetFieldValue<Material[]>("currentOverlays");
            int maxOverlays = self.GetFieldValue<int>("maxOverlays");

            CharacterBody body = self.body;

            if ((bool)body)
            {
                if (activeOverlayCount < maxOverlays && body.HasBuff(EgoShellBuff))
                {
                    currentOverlays[activeOverlayCount++] = lunarGolemShieldMaterial;
                }

                self.SetFieldValue("activeOverlayCount", activeOverlayCount);
                self.SetFieldValue("currentOverlays", currentOverlays);
            }
        }
    }
}
