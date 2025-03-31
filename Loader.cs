using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CutsceneCamera;
using Globals;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace ChaosGTFO
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Loader : BasePlugin
    {
        public const string MODNAME = "CutsceneCamera";
        public const string AUTHOR = "Brandonious";
        public const string GUID = "com.Brandonious.CutsceneCamera";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Logger;

        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CutsceneCameraLogic>();

            EventAPI.OnManagersSetup += () => {
                var ccLogic = Global.Current.gameObject.AddComponent<CutsceneCameraLogic>();
                LevelAPI.OnEnterLevel += ccLogic.LevelStarted;
            };

            Logger = Log;

            var harmony = new Harmony(MODNAME);
            harmony.PatchAll();

            Log.LogWarning($"Plugin {"CutsceneCamera"} Loaded!");
        }
    }
}