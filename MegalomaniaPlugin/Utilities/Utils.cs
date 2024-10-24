using BepInEx;
using MegalomaniaPlugin.Skills;
using RoR2;
using RoR2.Orbs;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.UI.GridLayoutGroup;

namespace MegalomaniaPlugin.Utilities
{
    public class Utils
    {
        //Parsed Rarity:Priority List
        public Dictionary<ItemTier, int> parsedRarityPriorityList;

        //Parsed Item:Priority List
        public Dictionary<ItemIndex, int> parsedItemPriorityList;

        //Selection mode
        public ConversionSelectionType parsedConversionSelectionType;

        //Items to convert to
        public Dictionary<ItemIndex, int> parsedItemConvertToList;

        //Thank you ConfigEgocentrism by Judgy53 for code reference:
        //https://github.com/Judgy53/ConfigEgocentrism/blob/main/ConfigEgocentrism/ConfigEgocentrismPlugin.cs
        public enum ItemTierLookup
        {
            white = ItemTier.Tier1,
            common = ItemTier.Tier1,

            green = ItemTier.Tier2,
            uncommon = ItemTier.Tier2,

            red = ItemTier.Tier3,
            legendary = ItemTier.Tier3,

            blue = ItemTier.Lunar,
            lunar = ItemTier.Lunar,

            yellow = ItemTier.Boss,
            boss = ItemTier.Boss,

            voidwhite = ItemTier.VoidTier1,
            voidcommon = ItemTier.VoidTier1,

            voidgreen = ItemTier.VoidTier2,
            voiduncommon = ItemTier.VoidTier2,

            voidred = ItemTier.VoidTier3,
            voidlegendary = ItemTier.VoidTier3,

            voidyellow = ItemTier.VoidBoss,
            voidboss = ItemTier.VoidBoss
        }

        public enum ConversionSelectionType
        {
            weighted = 0,
            priority = 1
        }

        private Dictionary<string, SkillDef> SkillLookup { get; set; }

        public void initSkillsList()
        {
            SkillLookup = new Dictionary<string, SkillDef>();
            SkillLookup.Add("conceit", ConceitAbility.ConceitSkill);
            SkillLookup.Add("monopolize", MonopolizeAbility.MonopolizeSkill);
            SkillLookup.Add("bomb", BombAbility.BombSkill);
            SkillLookup.Add("twinshot", TwinShotAbility.TwinShotSkill);
            SkillLookup.Add("shell", ShellAbility.ShellSkill);
        }

#nullable enable
        public SkillDef? lookupSkill(string str)
        {
            if (SkillLookup.TryGetValue(str.ToLower().Trim(), out SkillDef sdef))
            {
                return sdef;
            }
            return null;
        }
#nullable restore

        public void CorruptItem(Inventory inventory, ItemIndex toCorrupt, CharacterMaster master)
        {
            int ego = inventory.GetItemCount(DLC1Content.Items.LunarSun);
            if (ego < 1) return;
            int count = inventory.GetItemCount(toCorrupt);
            if (count > 0)
            {
                inventory.RemoveItem(toCorrupt, count);
                inventory.GiveItem(DLC1Content.Items.LunarSun, count);
                CharacterMasterNotificationQueue.SendTransformNotification(master, toCorrupt, DLC1Content.Items.LunarSun.itemIndex, CharacterMasterNotificationQueue.TransformationType.LunarSun);
            }
        }

        public List<ItemIndex> TransformItems(Inventory inventory, int amount, Xoroshiro128Plus transformRng, CharacterMaster master, bool ignoreCap = false, bool notifUI = true)
        {
            List<ItemIndex> toReturn = new List<ItemIndex>();
            if (!NetworkServer.active)
            {
                Log.Warning("[Server] function 'TransformItems' called on client");
                return toReturn;
            }

            if (!inventory || !master)
                return toReturn;

            if (amount < 1)
                return toReturn;

            if (parsedItemConvertToList.Count < 1)
            {
                return toReturn;
            }

            if (transformRng == null)
            {
                ulong seed = Run.instance.seed ^ (ulong)Run.instance.stageClearCount;
                transformRng = new Xoroshiro128Plus(seed);
            }

            Dictionary<ItemIndex, int> weightedInventory = weighInventory(inventory);

            int i = amount * weightedInventory.Count;
            int g = 0;

            while (amount > 0 && weightedInventory.Count > 0 && i > 0)
            {
                //just in case something goes wrong, don't loop forever
                i--;

                //shuffle
                ItemIndex toTransform = ItemIndex.None;
                //modality select item to transform
                switch (parsedConversionSelectionType)
                {
                    case ConversionSelectionType.weighted:
                        toTransform = getWeightedDictKey(weightedInventory, transformRng);
                        break;
                    case ConversionSelectionType.priority:
                        toTransform = getPriorityDictKey(weightedInventory, transformRng);
                        break;
                }

                if (toTransform == ItemIndex.None)
                {
                    Log.Error("Egocentrism tried to convert an item but something went wrong. Did you forget to add an enum or function?\n" +
                        $"parsedConversionSelectionType: '{parsedConversionSelectionType}'");
                    return toReturn;
                }

                List<ItemIndex> toGiveList = getWeightedDictKeyAndBackup(parsedItemConvertToList, transformRng);

                //do the thing
                ItemIndex toGive = ItemIndex.None;
                foreach (ItemIndex corruptor in toGiveList)
                {
                    //don't convert something into itself
                    if (toTransform != corruptor)
                    {
                        toGive = corruptor;
                        break;
                    }
                }

                //no valid targets to be transformed into were found.
                //perhaps egocentrism convert to list only contains 1 item?
                if (toGive == ItemIndex.None)
                {
                    g++;
                    if (g >= weightedInventory.Count)
                    {
                        return toReturn;
                    }
                    continue;
                }

                inventory.RemoveItem(toTransform);
                inventory.GiveItem(toGive);

                if (MegalomaniaPlugin.ConfigRegenerateScrap.Value)
                {
                    if (toTransform == DLC1Content.Items.RegeneratingScrap.itemIndex)
                    {
                        inventory.GiveItem(DLC1Content.Items.RegeneratingScrapConsumed);
                    }
                }

                //balance transformation over time
                if (!ignoreCap)
                    inventory.GiveItem(MegalomaniaPlugin.transformToken, 1 + MegalomaniaPlugin.ConfigMaxTransformationsPerStageStacking.Value);

                toReturn.Add(toTransform);

                //inform owner that ego happened
                if (notifUI)
                {
                    CharacterMasterNotificationQueue.SendTransformNotification(master, toTransform, toGive, CharacterMasterNotificationQueue.TransformationType.LunarSun);
                }

                //remove item from possible selections if it no longer exists, and re-weight it if stack size matters
                if (inventory.GetItemCount(toTransform) < 1)
                {
                    weightedInventory.Remove(toTransform);
                }
                else if (MegalomaniaPlugin.ConfigStackSizeMatters.Value)
                {
                    weightedInventory[toTransform] = weighSingleItem(toTransform, inventory.GetItemCount(toTransform));
                }

                amount--;
            }

            return toReturn;
        }

        public int weighSingleItem(ItemIndex itemIndex, int itemCount)
        {
            //don't convert egocentrism
            if (itemIndex == DLC1Content.Items.LunarSun.itemIndex)
            {
                return -1;
            }
            //don't convert things that don't exist
            ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
            if (!(bool)itemDef)
            {
                return -1;
            }
            //don't convert untiered items
            if (itemDef.tier == ItemTier.NoTier)
            {
                return -1;
            }
            //get tier weight
            int weight = 0;
            if (!parsedRarityPriorityList.TryGetValue(itemDef.tier, out weight))
            {
                weight = 0;
            }
            //don't convert blacklisted items
            int itemWeight = 0;
            if (parsedItemPriorityList.TryGetValue(itemIndex, out itemWeight) && itemWeight == 0)
            {
                return -1;
            }
            weight += itemWeight;
            //discard combination blacklisted items
            if (weight <= 0)
            {
                return -1;
            }
            if (MegalomaniaPlugin.ConfigStackSizeMatters.Value)
            {
                double toAdd = 0;
                toAdd += weight * (double)(itemCount - 1.0) * MegalomaniaPlugin.ConfigStackSizeMultiplier.Value;
                toAdd += (double)(itemCount - 1.0) * MegalomaniaPlugin.ConfigStackSizeAdder.Value;
                weight += (int)toAdd;
            }
            return weight;
        }

        public Dictionary<ItemIndex, int> weighInventory(Inventory inventory)
        {
            List<ItemIndex> inventoryItemsList = new List<ItemIndex>(inventory.itemAcquisitionOrder);

            Dictionary<ItemIndex, int> weightedInventory = new Dictionary<ItemIndex, int>();
            foreach (ItemIndex itemIndex in inventoryItemsList)
            {
                int weight = weighSingleItem(itemIndex, inventory.GetItemCount(itemIndex));
                if (weight <= 0)
                {
                    continue;
                }

                //allow item transform
                weightedInventory.Add(itemIndex, weight);
            }
            return weightedInventory;
        }

        public List<T> getWeightedDictKeyAndBackup<T>(Dictionary<T, int> dict, Xoroshiro128Plus rng)
        {
            Dictionary<T, int> copy = new Dictionary<T, int>();
            foreach (var kvp in dict)
            {
                copy.Add(kvp.Key, kvp.Value);
            }

            List<T> list = new List<T>();
            for (int i = 0; i < dict.Count && i < 2; i++)
            {
                T key = getWeightedDictKey(copy, rng);
                list.Add(key);
                copy.Remove(key);
            }
            list.Reverse();
            return list;
        }

        public T getPriorityDictKey<T>(Dictionary<T, int> dict, Xoroshiro128Plus rng)
        {
            int highestFound = 0;
            List<T> highestTsFound = new List<T>();
            foreach (var v in dict)
            {
                if (v.Value == highestFound)
                {
                    highestTsFound.Add(v.Key);
                    continue;
                }
                if (v.Value > highestFound)
                {
                    highestFound = v.Value;
                    highestTsFound.Clear();
                    highestTsFound.Add(v.Key);
                }
            }
            return highestTsFound[rng.RangeInt(0, highestTsFound.Count)];
        }

        public T getWeightedDictKey<T>(Dictionary<T, int> dict, Xoroshiro128Plus rng)
        {
            int totalWeight = 0;
            foreach (var weight in dict.Values)
            {
                totalWeight += weight;
            }

            int randomNumber = rng.RangeInt(0, totalWeight);
            foreach (var kvp in dict)
            {
                randomNumber -= kvp.Value;
                if (randomNumber < 0)
                {
                    return kvp.Key;
                }
            }

            Log.Error("Couldn't return a random weighted dictionary key! This shouldn't happen if all weights are positive. Returned FirstOrDefault() instead.");
            return dict.FirstOrDefault().Key;
        }

        public float determineStatBoost(bool diminishing, float perStack, float max, float stacksize)
        {
            if (max == 0)
                //no buff
                return 0f;
            else if (diminishing)
                //diminishing returns
                return max - max * (float)Math.Pow(1f - perStack / max, stacksize);
            else if (max > 0)
                //capped linear
                return Math.Min(perStack * stacksize, max);
            else
                //uncapped linear
                return perStack * stacksize;
        }

        public void ParseItemConvertToList()
        {
            parsedItemConvertToList = new Dictionary<ItemIndex, int>();

            string[] itemPriority = MegalomaniaPlugin.ConfigItemsToConvertTo.Value.Split(',');

            foreach (string iP in itemPriority)
            {
                string[] ItePrio = iP.Split(":");
                //if there's an incorrect amount of colons, skip
                if (ItePrio.Length != 2)
                {
                    Log.Warning($"(ConvertTo) Invalid amount of colons: `{iP}`");
                    continue;
                }
                string indexString = ItePrio[0].Trim();
                string priorityString = ItePrio[1].Trim();
                //if either side of the colon is blank, skip
                if (indexString.IsNullOrWhiteSpace() || priorityString.IsNullOrWhiteSpace())
                {
                    Log.Warning($"(ConvertTo) Invalid empty item or priority: `{iP}`");
                    continue;
                }
                int priority;
                //if the priority is not an integer, skip
                if (!int.TryParse(priorityString, out priority))
                {
                    Log.Warning($"(ConvertTo) Invalid priority: `{iP}`");
                    continue;
                }
                //if the item is undefined, skip
                ItemIndex index = ItemCatalog.FindItemIndex(indexString);
                if (index == ItemIndex.None)
                {
                    Log.Warning($"(ConvertTo) Invalid item: `{iP}`");
                    continue;
                }
                //if the rarity is already in the list, skip
                if (parsedItemConvertToList.ContainsKey(index))
                {
                    Log.Warning($"(ConvertTo) Item already in list: `{iP}`");
                    continue;
                }
                parsedItemConvertToList.Add(index, priority);
                Log.Info($"(ConvertTo) Item:Priority added! `{iP}`");
            }
        }

        public void ParseConversionSelectionType()
        {
            string toTest = MegalomaniaPlugin.ConfigConversionSelectionType.Value.Trim().ToLower();
            if (Enum.TryParse(toTest, out ConversionSelectionType conversionType))
            {
                parsedConversionSelectionType = conversionType;
                return;
            }

            Log.Warning($"Invalid conversion selection type: `{toTest}`. Defaulting to weighted.");
            parsedConversionSelectionType = ConversionSelectionType.weighted;
            return;
        }

        public void ParseRarityPriorityList()
        {
            parsedRarityPriorityList = new Dictionary<ItemTier, int>();

            string[] rarityPriority = MegalomaniaPlugin.ConfigRarityPriorityList.Value.Split(',');

            foreach (string rP in rarityPriority)
            {
                string[] Rapier = rP.Split(":");
                //if there's an incorrect amount of colons, skip
                if (Rapier.Length != 2)
                {
                    Log.Warning($"(Rarity:Priority) Invalid amount of colons: `{rP}`");
                    continue;
                }
                string tierString = Rapier[0].Trim().ToLower();
                string priorityString = Rapier[1].Trim();
                //if either side of the colon is blank, skip
                if (tierString.IsNullOrWhiteSpace() || priorityString.IsNullOrWhiteSpace())
                {
                    Log.Warning($"(Rarity:Priority) Invalid empty tier or priority: `{rP}`");
                    continue;
                }
                int priority;
                //if the priority is not an integer, skip
                if (!int.TryParse(priorityString, out priority) || priority < 0)
                {
                    Log.Warning($"(Rarity:Priority) Invalid priority: `{rP}`");
                    continue;
                }
                //if the rarity is undefined, skip
                if (!Enum.TryParse(tierString, out ItemTierLookup tier))
                {
                    Log.Warning($"(Rarity:Priority) Invalid rarity: `{rP}`");
                    continue;
                }
                //if the priority is 0, skip
                if (priority == 0)
                {
                    Log.Info($"(Rarity:Priority) Blacklisting Rarity:Priority! '{rP}'");
                    continue;
                }
                ItemTier rarity = (ItemTier)tier;
                //if the rarity is already in the list, skip
                if (parsedRarityPriorityList.ContainsKey(rarity))
                {
                    Log.Warning($"(Rarity:Priority) Rarity already in list: `{rP}`");
                    continue;
                }
                parsedRarityPriorityList.Add(rarity, priority);
                Log.Info($"(Rarity:Priority) Rarity:Priority added! `{rP}`");
            }
        }

        public void ParseItemPriorityList()
        {
            parsedItemPriorityList = new Dictionary<ItemIndex, int>();

            string[] itemPriority = MegalomaniaPlugin.ConfigItemPriorityList.Value.Split(',');

            foreach (string iP in itemPriority)
            {
                string[] ItePrio = iP.Split(":");
                //if there's an incorrect amount of colons, skip
                if (ItePrio.Length != 2)
                {
                    Log.Warning($"(Item:Priority) Invalid amount of colons: `{iP}`");
                    continue;
                }
                string indexString = ItePrio[0].Trim();
                string priorityString = ItePrio[1].Trim();
                //if either side of the colon is blank, skip
                if (indexString.IsNullOrWhiteSpace() || priorityString.IsNullOrWhiteSpace())
                {
                    Log.Warning($"(Item:Priority) Invalid empty item or priority: `{iP}`");
                    continue;
                }
                int priority;
                //if the priority is not an integer, skip
                if (!int.TryParse(priorityString, out priority))
                {
                    Log.Warning($"(Item:Priority) Invalid priority: `{iP}`");
                    continue;
                }
                //if the item is undefined, skip
                ItemIndex index = ItemCatalog.FindItemIndex(indexString);
                if (index == ItemIndex.None)
                {
                    Log.Warning($"(Item:Priority) Invalid item: `{iP}`");
                    continue;
                }
                //if the rarity is already in the list, skip
                if (parsedItemPriorityList.ContainsKey(index))
                {
                    Log.Warning($"(Item:Priority) Item already in list: `{iP}`");
                    continue;
                }
                parsedItemPriorityList.Add(index, priority);
                Log.Info($"(Item:Priority) Item:Priority added! `{iP}`");
            }
        }
    }
}
