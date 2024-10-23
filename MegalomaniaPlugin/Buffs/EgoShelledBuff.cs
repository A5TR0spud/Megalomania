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

            EgoShellBuff.buffColor = new Color(0.458823529412f, 0.890196078431f, 0.960784313725f, 1f);
            //new Color(0.38039216f, 0.6392157f, 0.9372549f, 1f);
            EgoShellBuff.name = "megalomaniaShellBuff";

            ContentAddition.AddBuffDef(EgoShellBuff);

            On.RoR2.HealthComponent.Heal += HealthComponent_Heal;
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
            IL.RoR2.HealthComponent.TakeDamageProcess += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    //0
                    x => x.MatchLdarg(0),   //this
                    //1
                    x => x.MatchCallOrCallvirt<HealthComponent>("get_fullHealth"),
                    //2
                    x => x.MatchLdcR4(0.1f),
                    //3
                    x => x.MatchMul(),
                    //4
                    x => x.MatchStloc(7),   //set num4
                    //5
                    x => x.MatchLdarg(0)
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
            IL.RoR2.CharacterModel.UpdateOverlayStates += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    //0
                    x => x.MatchLdarg(0),
                    //1
                    x => x.MatchLdfld(typeof(CharacterModel).GetField("body")),
                    //2
                    x => x.MatchLdsfld(typeof(RoR2Content.Buffs).GetField("LunarShell")),
                    //3
                    x => x.MatchCallOrCallvirt<CharacterBody>("HasBuff")
                    //4
                    );
                c.Index += 4; //insert there
                //stack already contains boolean from HasBuff(LunarShell)
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(CharacterModel).GetField("body"));
                c.EmitDelegate<Func< bool, CharacterBody, bool>>((hasLunarShell, cb) =>
                {
                    return hasLunarShell || cb.HasBuff(EgoShellBuff);
                });
            };
            IL.RoR2.CharacterModel.UpdateOverlays += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(
                    //0
                    x => x.MatchLdsfld(typeof(CharacterModel).GetField("lunarGolemShieldMaterial")),
                    //1
                    x => x.MatchLdarg(0),
                    //2
                    x => x.MatchLdfld(typeof(CharacterModel).GetField("body")),
                    //3
                    x => x.MatchLdsfld(typeof(RoR2Content.Buffs).GetField("LunarShell")),
                    //4
                    x => x.MatchCallOrCallvirt<CharacterBody>("HasBuff")
                    //5
                    );
                c.Index += 5; //insert there
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, typeof(CharacterModel).GetField("body"));
                c.EmitDelegate<Func<bool, CharacterBody, bool>>((hasLunarShell, cb) =>
                {
                    return hasLunarShell || cb.HasBuff(EgoShellBuff);
                });
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

        private static void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (buffDef == EgoShellBuff)
                reloadOverlays(self);
        }

        private static void CharacterBody_OnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (buffDef == EgoShellBuff)
                reloadOverlays(self);
        }

        private static void reloadOverlays(CharacterBody body)
        {
            ModelLocator component = body.modelLocator;
            if (!component)
            {
                return;
            }
            Transform modelTransform = component.modelTransform;
            if ((bool)modelTransform)
            {
                CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
                if ((bool)component2)
                {
                    component2.UpdateOverlays();
                }
            }
        }

        private static float HealthComponent_Heal(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
        {
            if (self.body && self.body.HasBuff(EgoShellBuff))
            {
                amount *= 0.5f;
            }
            return orig(self, amount, procChainMask, nonRegen);
        }
    }
}
