using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Globals;
using GTFO.API;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;

namespace ChaosGTFO
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Loader : BasePlugin
    {
        public const string MODNAME = "ChaosGTFO";
        public const string AUTHOR = "Brandonious";
        public const string GUID = "com.Brandonious.ChaosGTFO";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Logger;

        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<ChaosLogic>();

            EventAPI.OnManagersSetup += () => {
                var cLogic = Global.Current.gameObject.AddComponent<ChaosLogic>();
                LevelAPI.OnEnterLevel += cLogic.LevelStarted;
                LevelAPI.OnLevelCleanup += cLogic.LevelCleanup;
            };

            Logger = Log;

            var harmony = new Harmony(MODNAME);
            harmony.PatchAll(typeof(GlowstickGunPatch));

            Log.LogWarning($"Plugin {"ChaosGTFO"} Loaded! Have fun.. :)");
        }
    }
}