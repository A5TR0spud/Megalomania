using MegalomaniaPlugin.Utilities;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MegalomaniaPlugin.Buffs
{
    public class EgoShelledBuff
    {
        public static BuffDef EgoShellBuff;

        public static void initEgoShellBuff() {
            EgoShellBuff = ScriptableObject.CreateInstance<BuffDef>();

            EgoShellBuff.isDebuff = false;
            EgoShellBuff.isCooldown = false;
            EgoShellBuff.canStack = false;
            EgoShellBuff.ignoreGrowthNectar = true;
            EgoShellBuff.isHidden = false;

            // UnityEngine.Object sprite = LegacyResourcesAPI.Load<UnityEngine.Object>("Textures/BuffIcons/texBuffLunarShellIcon");
            // Log.Debug(sprite);
            // EgoShellBuff.iconSprite = Sprite.Create(new Rect(0, 0, sprite.width, sprite.height), new Vector2(0.5f, 0.5f), 100, sprite);

            EgoShellBuff.iconSprite = MegalomaniaPlugin.megalomaniaAssetBundle.LoadAsset<Sprite>("texEgoShellBuff");

            EgoShellBuff.buffColor = new Color(0.38039216f, 0.6392157f, 0.9372549f, 1f);
            EgoShellBuff.name = "megalomaniaShellBuff";

            ContentAddition.AddBuffDef(EgoShellBuff);

            On.RoR2.CharacterModel.UpdateOverlays += CharacterModel_UpdateOverlays;
            On.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            //On.RoR2.HealthComponent.SendDamageDealt += HealthComponent_SendDamageDealt;
            IL.RoR2.HealthComponent.TakeDamageProcess += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    //0
                    x => x.MatchLdarg(0), //this
                    //1
                    x => x.MatchCall<HealthComponent>("get_fullHealth"),
                    //2
                    x => x.MatchLdcR4(0.1f),
                    //3
                    x => x.MatchMul(),
                    //4
                    x => x.MatchStloc(7), //set num4
                    //5
                    x => x.MatchLdarg(0) //end if
                    //6: insert here
                    );
                c.Index += 6; //insert there
                //c.Emit(OpCodes.Ldarg_0); //this
                c.Emit(OpCodes.Ldfld, typeof(HealthComponent).GetField("body")); //.body
                c.Emit(OpCodes.Ldloc_S, (byte)7); //the local damage variable (num4)
                c.Emit(OpCodes.Ldarg_0); //this
                c.EmitDelegate<Func<CharacterBody, float, HealthComponent, float>>((cb, damage, healthComponent) =>
                {
                    if (cb && cb.HasBuff(EgoShellBuff))
                    {
                        damage -= healthComponent.barrier;
                        if (damage > healthComponent.fullCombinedHealth * 0.1f)
                        {
                            damage = healthComponent.fullCombinedHealth * 0.1f;
                        }
                        damage += healthComponent.barrier;
                    }
                    return damage;
                }); //adds the new damage to the stack
                c.Emit(OpCodes.Stloc_S, (byte)7); //set the local damage variable (num4)
                c.Emit(OpCodes.Ldarg_0);

                //Log.Debug(il.ToString());
            };

            RecalculateStatsAPI.GetStatCoefficients += (sender, args) =>
            {
                if (sender)
                {
                    if (sender.HasBuff(EgoShellBuff))
                    {
                        args.moveSpeedReductionMultAdd += 1;
                    }
                }
            };
        }

        /*private static void HealthComponent_SendDamageDealt(On.RoR2.HealthComponent.orig_SendDamageDealt orig, DamageReport damageReport)
        {
            HealthComponent healthComponent = damageReport.victim;
            if (healthComponent.body && healthComponent.body.HasBuff(EgoShellBuff))
            {
                float damage = damageReport.damageInfo.damage;
                damage -= healthComponent.barrier;
                if (damage > healthComponent.fullCombinedHealth * 0.1f)
                {
                    damage = healthComponent.fullCombinedHealth * 0.1f;
                }
                damage += healthComponent.barrier;
                damage = 1;
                damageReport.damageInfo.damage = damage;
                damageReport.damageDealt = damage;
            }
            orig(damageReport);
        }*/


        private static float HealthComponent_Heal(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
        {
            if (self.body && self.body.HasBuff(EgoShellBuff))
            {
                amount *= 0.5f;
            }
            return orig(self, amount, procChainMask, nonRegen);
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
