using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace ChestCrafting
{
    // Client-side: lets crafting stations and building use items from nearby chests.
    // Only the local player's inventory is ever extended; nothing runs on the server.
    [BepInPlugin(Guid, "ChestCrafting", Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "realfin.ChestCrafting";
        public const string Version = "1.0.0";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> Range;
        internal static ConfigEntry<bool> ForBuilding;
        internal static ConfigEntry<bool> ShowAvailable;

        private void Awake()
        {
            Enabled = Config.Bind("General", "Enabled", true, "Use items from nearby chests when crafting and building.");
            Range = Config.Bind("General", "Range", 20f, new ConfigDescription("How far (in meters) from you a chest may be.", new AcceptableValueRange<float>(1f, 50f)));
            ForBuilding = Config.Bind("General", "ForBuilding", true, "Also use chest items when building with the hammer, hoe or cultivator.");
            ShowAvailable = Config.Bind("General", "ShowAvailable", true, "Show requirements as available/needed, e.g. 47/3.");
            new Harmony(Guid).PatchAll();
        }
    }

    internal static class Chests
    {
        // Every loaded container; destroyed ones are pruned lazily.
        internal static readonly HashSet<Container> All = new HashSet<Container>();

        private static readonly AccessTools.FieldRef<Container, ZNetView> NView = AccessTools.FieldRefAccess<Container, ZNetView>("m_nview");
        private static readonly Func<Container, long, bool> CanOpen =
            AccessTools.MethodDelegate<Func<Container, long, bool>>(AccessTools.Method(typeof(Container), "CheckAccess"));

        // >0 while inside a crafting/building check; Guard stops our own calls being extended again.
        internal static int Depth;
        internal static bool Guard;

        private static int _cachedFrame = -1;
        private static readonly List<Container> _nearby = new List<Container>();

        internal static bool Applies(Inventory inv)
        {
            if (Depth <= 0 || Guard || !Plugin.Enabled.Value) return false;
            Player p = Player.m_localPlayer;
            return p != null && inv == p.GetInventory();
        }

        // Usable chests within range, nearest first. Cached for the current frame.
        internal static List<Container> Nearby()
        {
            if (_cachedFrame == Time.frameCount) return _nearby;
            _cachedFrame = Time.frameCount;
            _nearby.Clear();
            All.RemoveWhere(c => c == null);

            Player player = Player.m_localPlayer;
            if (player == null) return _nearby;
            Vector3 pos = player.transform.position;
            float range = Plugin.Range.Value;
            long me = player.GetPlayerID();

            foreach (Container c in All)
            {
                if (Vector3.Distance(pos, c.transform.position) > range) continue;
                ZNetView nview = NView(c);
                if (nview == null || !nview.IsValid() || c.GetInventory() == null) continue;
                if (c.GetComponent<TombStone>() != null) continue;              // never eat someone's gravestone
                if (!CanOpen(c, me)) continue;                                   // private chests of others
                if (!PrivateArea.CheckAccess(c.transform.position, 0f, false)) continue; // warded areas you can't use
                // Someone else has it open right now: leave it alone.
                if (!nview.IsOwner() && nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1) continue;
                _nearby.Add(c);
            }
            _nearby.Sort((a, b) =>
                Vector3.Distance(pos, a.transform.position).CompareTo(Vector3.Distance(pos, b.transform.position)));
            return _nearby;
        }

        internal static void Invalidate() => _cachedFrame = -1;

        // Take items from chests; returns how many are still missing.
        internal static int Take(string name, int amount, int quality, bool worldLevelBased)
        {
            foreach (Container c in Nearby())
            {
                if (amount <= 0) break;
                Inventory inv = c.GetInventory();
                int have = inv.CountItems(name, quality, worldLevelBased);
                if (have <= 0) continue;

                // Only the owner's changes are saved to the world. Opening a chest does the same claim.
                ZNetView nview = NView(c);
                if (!nview.IsOwner()) nview.ClaimOwnership();
                if (!nview.IsOwner()) continue;

                int take = Math.Min(have, amount);
                inv.RemoveItem(name, take, quality, worldLevelBased); // fires Changed -> Container saves (we own it)
                amount -= take;
            }
            Invalidate();
            return amount;
        }
    }

    [HarmonyPatch(typeof(Container), "Awake")]
    internal static class TrackContainers
    {
        private static void Postfix(Container __instance) => Chests.All.Add(__instance);
    }

    // Places where the game checks, shows or consumes requirements: extend the inventory only inside these.
    [HarmonyPatch]
    internal static class Contexts
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(InventoryGui), "UpdateRecipe");
            yield return AccessTools.Method(typeof(InventoryGui), "UpdateRecipeList");
            yield return AccessTools.Method(typeof(InventoryGui), "DoCrafting");
            yield return AccessTools.Method(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Recipe), typeof(bool), typeof(int), typeof(int) });
            yield return AccessTools.Method(typeof(Player), nameof(Player.ConsumeResources));
        }

        private static void Prefix() => Chests.Depth++;
        private static Exception Finalizer(Exception __exception) { Chests.Depth--; return __exception; }
    }

    [HarmonyPatch]
    internal static class BuildContexts
    {
        private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Player), nameof(Player.HaveRequirements), new[] { typeof(Piece), typeof(Player.RequirementMode) });
            yield return AccessTools.Method(typeof(Hud), "SetupPieceInfo");
        }

        private static void Prefix(ref bool __state)
        {
            __state = Plugin.ForBuilding.Value;
            if (__state) Chests.Depth++;
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            if (__state) Chests.Depth--;
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems))]
    internal static class CountItemsPatch
    {
        private static void Postfix(Inventory __instance, string name, int quality, bool matchWorldLevel, ref int __result)
        {
            if (!Chests.Applies(__instance) || name == null) return;
            foreach (Container c in Chests.Nearby())
                __result += c.GetInventory().CountItems(name, quality, matchWorldLevel);
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveItem), new[] { typeof(string), typeof(bool) })]
    internal static class HaveItemPatch
    {
        private static void Postfix(Inventory __instance, string name, bool matchWorldLevel, ref bool __result)
        {
            if (__result || !Chests.Applies(__instance)) return;
            foreach (Container c in Chests.Nearby())
                if (c.GetInventory().HaveItem(name, matchWorldLevel)) { __result = true; return; }
        }
    }

    // Remove from your own inventory first, then from the nearest chests.
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(string), typeof(int), typeof(int), typeof(bool) })]
    internal static class RemoveItemPatch
    {
        private static bool Prefix(Inventory __instance, string name, int amount, int itemQuality, bool worldLevelBased)
        {
            if (!Chests.Applies(__instance)) return true;
            Chests.Guard = true;
            try
            {
                int own = __instance.CountItems(name, itemQuality, worldLevelBased);
                int fromSelf = Math.Min(own, amount);
                if (fromSelf > 0) __instance.RemoveItem(name, fromSelf, itemQuality, worldLevelBased);
                int missing = Chests.Take(name, amount - fromSelf, itemQuality, worldLevelBased);
                if (missing > 0) Debug.LogWarning($"[ChestCrafting] Could not find {missing}x {name} to remove");
            }
            finally { Chests.Guard = false; }
            return false;
        }
    }

    // Show "available/needed" next to each ingredient.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
    internal static class ShowAvailablePatch
    {
        private static void Prefix(bool craft, ref bool __state)
        {
            __state = craft || Plugin.ForBuilding.Value;
            if (__state) Chests.Depth++;
        }

        private static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, int quality, int craftMultiplier, bool __result, bool __state)
        {
            if (!__state) return;
            if (!__result || !Plugin.ShowAvailable.Value || req.m_resItem == null || player == null) return;
            int have = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
            int need = req.GetAmount(quality) * craftMultiplier;
            TMP_Text text = elementRoot.transform.Find("res_amount")?.GetComponent<TMP_Text>();
            if (text != null) text.text = $"{have}/{need}";
        }

        private static Exception Finalizer(Exception __exception, bool __state)
        {
            if (__state) Chests.Depth--;
            return __exception;
        }
    }
}
