using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;


namespace PriceUpgradeScalingMod
{
    [BepInPlugin("Zmazor.PriceUpgradeScaling", "PriceUpgradeScaling", "1.2.0")]
    public class PriceUpgradeScaling : BaseUnityPlugin
    {
        internal static PriceUpgradeScaling Instance { get; private set; }
        internal static ManualLogSource Logger => Instance?._logger ?? BepInEx.Logging.Logger.CreateLogSource("PriceUpgradeScaling");
        private ManualLogSource _logger;

        internal Harmony? Harmony { get; set; }

        public static ConfigEntry<float> PriceScaling;
        private static Dictionary<string, int> previousMaxValues = new Dictionary<string, int>();

        private void Awake()
        {
            Instance = this;
            _logger = base.Logger;

            Logger.LogInfo(">>> [ZMAZOR MOD] Awake started");

            PriceScaling = Config.Bind(
                "General",
                "Price Scaling",
                0.8f,
                new ConfigDescription(
                    "How much upgrade prices scale with players. Can be any float. Formula: price = base + (players - 1) * scale"
                )
            );

            Logger.LogInfo($"[ZMAZOR MOD] PriceScaling loaded: {PriceScaling.Value}");

            if (PriceScaling.Value < 0f || PriceScaling.Value > 10f)
                Logger.LogWarning($"[ZMAZOR MOD] Unusual PriceScaling value: {PriceScaling.Value}. Is this intentional?");

            Patch();

            Logger.LogInfo(">>> [ZMAZOR MOD] Awake finished");
        }

        internal void Patch()
        {
            if (Harmony == null)
            {
                Harmony = new Harmony(Info.Metadata.GUID);
                Harmony.PatchAll();
            }
        }

        // Synchronizacja upgrade'ów – patch StatsManager.Update()
        [HarmonyPatch(typeof(StatsManager), "Update")]
        public class SharedUpgradesPatch
        {
            [HarmonyPrefix]
            private static void SyncUpgrades()
            {
                try
                {
                    if (!LevelGenerator.Instance.Generated || SemiFunc.MenuLevel() || !PhotonNetwork.IsMasterClient)
                        return;

                    var stats = StatsManager.instance;
                    var dict = stats.dictionaryOfDictionaries;
                    var keys = dict.Keys.Where(k => k.StartsWith("playerUpgrade")).ToList();
                    var maxValues = new Dictionary<string, int>();

                    foreach (var key in keys)
                    {
                        if (dict.TryGetValue(key, out var valueDict) && valueDict.Count > 0)
                        {
                            maxValues[key] = valueDict.Values.Max();
                        }
                    }

                    bool changed = maxValues.Any(kv =>
                        !previousMaxValues.TryGetValue(kv.Key, out var val) || val != kv.Value);

                    if (!changed)
                        return;

                    foreach (var kv in maxValues)
                    {
                        stats.DictionaryFill(kv.Key, kv.Value);
                    }

                    SemiFunc.StatSyncAll();
                    previousMaxValues = new Dictionary<string, int>(maxValues);

                    Logger.LogInfo("[ZMAZOR MOD] Upgrades synced for all players.");
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"[ZMAZOR MOD] Error during upgrade sync: {ex}");
                }
            }
        }
    }

    [HarmonyPatch(typeof(ItemAttributes), "GetValue")]
    public class UpdatePrice
    {
        [HarmonyPostfix]
        private static void Start_Postfix(ItemAttributes __instance)
        {
            try
            {
                float value = PriceUpgradeScaling.PriceScaling.Value;

                var players = SemiFunc.PlayerGetAll();
                if (players == null || players.Count == 0)
                {
                    PriceUpgradeScaling.Logger.LogWarning("[ZMAZOR MOD] No players found in SemiFunc.PlayerGetAll()");
                    return;
                }

                float multiplier = 1f + (SemiFunc.PlayerGetAll().Count - 1) * value;

                var itemTypeField = AccessTools.Field(__instance.GetType(), "itemType");
                var valueField = AccessTools.Field(__instance.GetType(), "value");

                if (itemTypeField != null && valueField != null)
                {
                    var itemType = (SemiFunc.itemType)itemTypeField.GetValue(__instance);
                    if ((int)itemType == 3) // 3 = Upgrade
                    {
                        int currentValue = (int)valueField.GetValue(__instance);
                        int newValue = (int)Mathf.Round(currentValue * multiplier);
                        newValue = Mathf.Max(0, newValue);
                        valueField.SetValue(__instance, newValue);

                        if (GameManager.Multiplayer() && PhotonNetwork.IsMasterClient)
                        {
                            var photonViewField = AccessTools.Field(__instance.GetType(), "photonView");
                            var photonView = photonViewField?.GetValue(__instance) as PhotonView;
                            photonView?.RPC("GetValueRPC", RpcTarget.Others, newValue);
                        }

                        PriceUpgradeScaling.Logger.LogDebug($"[ZMAZOR MOD] {__instance} UpdatedPrice to {newValue}");
                    }
                }
                else
                {
                    PriceUpgradeScaling.Logger.LogWarning("[ZMAZOR MOD] Failed to find itemType or value fields");
                }
            }
            catch (System.Exception ex)
            {
                PriceUpgradeScaling.Logger.LogError($"[ZMAZOR MOD] Error during price patch: {ex}");
            }
        }
    }
}
