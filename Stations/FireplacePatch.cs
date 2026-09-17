using HarmonyLib;
using SmartCraftStorage.Shared;
using UnityEngine;

namespace SmartCraftStorage.Stations
{
    [HarmonyPatch(typeof(Fireplace), "UpdateFireplace")]
    internal static class FireplacePatch
    {
        private static readonly string[] WoodFuelPrefabNames = { "Wood", "FineWood", "RoundLog" };

        private static void Postfix(Fireplace __instance)
        {
            try
            {
                if (!StationConfig.FireplaceAutoRefuel.Value
                    || __instance.m_nview == null || !__instance.m_nview.IsValid()
                    || !__instance.m_nview.IsOwner())
                {
                    return;
                }

                if (__instance.m_fuelItem == null)
                {
                    return;
                }

                var player = Player.m_localPlayer;
                if (player == null)
                {
                    return;
                }

                float currentFuel = __instance.m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
                if (Mathf.CeilToInt(currentFuel) >= __instance.m_maxFuel)
                {
                    return;
                }

                foreach (var container in NearbyContainers.Find(__instance.transform.position, StationConfig.FireplaceRadius.Value, player))
                {
                    if (Mathf.CeilToInt(currentFuel) >= __instance.m_maxFuel)
                    {
                        break;
                    }

                    var chestInventory = container.GetInventory();
                    var fuelItem = FindFuelItem(__instance, chestInventory);
                    if (fuelItem == null)
                    {
                        continue;
                    }

                    if (!NearbyContainers.TryClaimWriteAccess(container))
                    {
                        continue;
                    }

                    chestInventory.RemoveItem(fuelItem, 1);
                    __instance.m_nview.InvokeRPC("RPC_AddFuel");
                    currentFuel += 1f;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        private static ItemDrop.ItemData FindFuelItem(Fireplace fireplace, Inventory inventory)
        {
            string fuelName = fireplace.m_fuelItem.m_itemData.m_shared.m_name;
            bool canUseAnyWood = !StationConfig.FireplaceRegularWoodOnly.Value
                && fireplace.m_fuelItem.gameObject.name == "Wood";

            foreach (var item in inventory.GetAllItems())
            {
                if (canUseAnyWood)
                {
                    foreach (var woodPrefabName in WoodFuelPrefabNames)
                    {
                        if (item.m_dropPrefab != null && item.m_dropPrefab.name == woodPrefabName)
                        {
                            return item;
                        }
                    }
                }
                else if (item.m_shared.m_name == fuelName)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
