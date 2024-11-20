using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System;
using System.Collections.Generic;
using System.Text;

namespace MegalomaniaPlugin.Items
{
    public class EgoSpeechDriver
    {
        public static void init()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        }

        private static void HealthComponent_TakeDamageProcess(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);

            if (!damageInfo.rejected)
            {
                Chat.UserChatMessage msg = new Chat.UserChatMessage
                {
                    sender = self.body.gameObject,
                    text = "lol"
                };

                Chat.SendBroadcastChat(msg);
                Chat.AddMessage(msg);
                Chat.SendBroadcastChat(msg);
            }
        }
    }
}
