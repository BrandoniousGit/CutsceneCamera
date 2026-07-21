using CharacterDestruction;
using Enemies;
using HarmonyLib;
using SNetwork;
using UnityEngine;

namespace StrikerBossfight.BossfightMain
{
    [HarmonyPatch]
    internal class BossfightPatches
    {
        public static event Action<float>? OnVolumeChangedAction;

        [HarmonyPatch(typeof(CellSettingsApply), nameof(CellSettingsApply.ApplyMusicVolume))]
        [HarmonyPostfix]
        private static void GlobalMusic(float value)
        {
            OnVolumeChangedAction?.Invoke(value);
        }

        [HarmonyPatch(typeof(EB_InCombat), nameof(EB_InCombat.TryScream))]
        [HarmonyPrefix]
        private static bool Postfix_TryScream(EB_InCombat __instance)
        {
            if (__instance.m_ai.m_enemyAgent.EnemyDataID == 150u)
            {
                return false;
            }

            return true;
        }
    }
}
