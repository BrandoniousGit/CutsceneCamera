using CharacterDestruction;
using HarmonyLib;
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
        
        //[HarmonyPatch(typeof(CD_CharacterDestructionCollider), nameof(CD_CharacterDestructionCollider.TryDoDestruction))]
        //[HarmonyPrefix]
        //private static bool PreventAnything(CD_CharacterDestructionCollider __instance, ImpactDirection impactDirection, Vector3 fromDir, Vector3 atPos_Local, Vector3 force, CD_DestructionSeverity severity, bool severeArmsDestuctionNotAllowed, out sDestructionEventData destructionEventData)
        //{
        //    destructionEventData = default(sDestructionEventData);
        //    return false;
        //}
    }
}
