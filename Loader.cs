using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using StrikerBossfight.CutsceneCamera;
using Globals;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using BossfightLevel.BossfightMain;

namespace StrikerBossfight
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Loader : BasePlugin
    {
        public const string MODNAME = "StrikerBossfight";
        public const string AUTHOR = "Brandonious";
        public const string GUID = "com.Brandonious.StrikerBossfight";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Logger;

        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CutsceneCameraLogic>();
            ClassInjector.RegisterTypeInIl2Cpp<BossfightCore>();
            ClassInjector.RegisterTypeInIl2Cpp<AnimationEventReceiver>();
            ClassInjector.RegisterTypeInIl2Cpp<SunAttack>();
            ClassInjector.RegisterTypeInIl2Cpp<PlumeAttack>();
            ClassInjector.RegisterTypeInIl2Cpp<Plume>();
            ClassInjector.RegisterTypeInIl2Cpp<FireballAttack>();
            ClassInjector.RegisterTypeInIl2Cpp<Fireball>();
            ClassInjector.RegisterTypeInIl2Cpp<DespawnEffect>();

            EventAPI.OnManagersSetup += () => {
                var ccLogic = Global.Current.gameObject.AddComponent<CutsceneCameraLogic>();
                LevelAPI.OnEnterLevel += ccLogic.LevelStarted;
                
                var testLogic = Global.Current.gameObject.AddComponent<BossfightCore>();
                LevelAPI.OnEnterLevel += testLogic.LevelStarted;
                LevelAPI.OnLevelCleanup += testLogic.LevelQuit;
            };

            Logger = Log;

            var harmony = new Harmony(MODNAME);
            harmony.PatchAll();

            Log.LogWarning($"Plugin {"StrikerBossfight"} Loaded!");
        }
    }
}