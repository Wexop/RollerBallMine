using System;
using BepInEx;
using System.IO;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LethalLib.Extras;
using LethalLib.Modules;
using RandomEnemiesSize;
using Unity.Netcode;
using static LethalLib.Modules.Levels;
using UnityEngine;
using NetworkPrefabs = LethalLib.Modules.NetworkPrefabs;


namespace RollerBallMine
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("wexop.random_enemies_size", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("evaisa.lethallib", "0.15.1")]
    [BepInDependency(StaticNetcodeLib.StaticNetcodeLib.Guid, BepInDependency.DependencyFlags.HardDependency)]
    public class RollerBallMinePlugin : BaseUnityPlugin
    {

        const string GUID = "wexop.roller_ball_mine";
        const string NAME = "RollerBallMine";
        const string VERSION = "1.0.1";

        public static RollerBallMinePlugin instance;

        public ConfigEntry<int> maxSpawn;
        public ConfigEntry<int> minSpawn;
        
        public ConfigEntry<float> Speed;
        public ConfigEntry<float> TriggerRadius;
        public ConfigEntry<float> ExplodeTime;
        public ConfigEntry<float> ExplosionRange;
        public ConfigEntry<int> ExplosionDamage;
        public ConfigEntry<float> DetectionRange;
        public ConfigEntry<float> SoundVolume;
        
        public ConfigEntry<bool> DebugMode;

        public GameObject ballObject;

        private void Update()
        {
            if(!DebugMode.Value) return;
            var drop = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Discard", false).ReadValue<float>();
            
            if (drop > 0)
            {
                if (ballObject != null)
                {
                    var o = Instantiate(ballObject,
                        GameNetworkManager.Instance.localPlayerController.transform.position +
                        GameNetworkManager.Instance.localPlayerController.transform.forward * 15, Quaternion.identity);

                    var n = o.GetComponent<NetworkObject>();
                    if(n.IsOwner) n.Spawn();
                }
            }
        }

        void Awake()
        {
            instance = this;
            
            Logger.LogInfo($"RollerBallMine starting....");

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "rollerballmine");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);

            LoadConfigs();
            
            SpawnableMapObjectDef rollerBall =
                bundle.LoadAsset<SpawnableMapObjectDef>("Assets/LethalCompany/Mods/RollerBallMine/RollerBallMine.asset");
            Logger.LogInfo($"{rollerBall.spawnableMapObject.prefabToSpawn.name} FOUND");

            ballObject = rollerBall.spawnableMapObject.prefabToSpawn;
            
            if (Chainloader.PluginInfos.ContainsKey("wexop.random_enemies_size"))
            {
                Debug.Log("RandomEnemiesSize is here !");
                ballObject.AddComponent<MapHazardSizeRandomizer>();
            }
            
            rollerBall.spawnableMapObject.numberToSpawn.keys[0].value = instance.minSpawn.Value;
            rollerBall.spawnableMapObject.numberToSpawn.keys[1].value = instance.maxSpawn.Value;
            
            NetworkPrefabs.RegisterNetworkPrefab(rollerBall.spawnableMapObject.prefabToSpawn);
            Utilities.FixMixerGroups(rollerBall.spawnableMapObject.prefabToSpawn);
            
            MapObjects.RegisterMapObject(rollerBall, LevelTypes.All, _ => rollerBall.spawnableMapObject.numberToSpawn);
            
            
            Logger.LogInfo($"RollerBallMine is ready!");
        }

        public static string ConditionalString(string value)
        {
            var finalString = value.ToLower();
            while (finalString.Contains(" "))
            {
                finalString = finalString.Replace(" ", "");
            }

            return finalString;
        }

        private void LoadConfigs()
        {
            minSpawn = Config.Bind(
                "General", 
                "MinSpawn", 
                0,
                "Min RollerBallMine possible for one game. You need to restart the game.");
            CreateIntConfig(minSpawn);
            
            maxSpawn = Config.Bind(
                "General", 
                "MaxSpawn", 
                7,
                "Max RollerBallMine possible for one game. You need to restart the game.");
            CreateIntConfig(maxSpawn);
            
            Speed = Config.Bind(
                "General", 
                "Speed", 
                5f,
                "RollerBallMine speed. You don't need to restart the game :)");
            CreateFloatConfig(Speed, 0f, 40f);
            
            TriggerRadius = Config.Bind(
                "General", 
                "TriggerRadius", 
                0.5f,
                "RollerBallMine distance with player to explode. You don't need to restart the game :)");
            CreateFloatConfig(TriggerRadius, 0f, 10f);
            
            ExplodeTime = Config.Bind(
                "General", 
                "ExplodeTime", 
                3f,
                "RollerBallMine time to explode if it don't touch any player. You don't need to restart the game :)");
            CreateFloatConfig(ExplodeTime, 0f, 10f);
            
            ExplosionRange = Config.Bind(
                "General", 
                "ExplosionRange", 
                4f,
                "RollerBallMine explosion range. You don't need to restart the game :)");
            CreateFloatConfig(ExplosionRange, 0f, 30f);
            
            ExplosionDamage = Config.Bind(
                "General", 
                "ExplosionDamage", 
                100,
                "RollerBallMine explosion damage to player. You don't need to restart the game :)");
            CreateIntConfig(ExplosionDamage);
            
            DetectionRange = Config.Bind(
                "General", 
                "DetectionRange", 
                10f,
                "RollerBallMine player detection distance. You don't need to restart the game :)");
            CreateFloatConfig(DetectionRange);
            
            SoundVolume = Config.Bind(
                "Sound", 
                "SoundVolume", 
                0.6f,
                "RollerBallMine sounds volume. You don't need to restart the game :)");
            CreateFloatConfig(SoundVolume, 0f, 2f);
            
            //DEV
            
            DebugMode = Config.Bind(
                "DEV", 
                "DebugMode", 
                false,
                "Enable dev mod to spawn balls with [G]. You don't need to restart the game :)");
            CreateBoolConfig(DebugMode);
        }
        
        private void CreateFloatConfig(ConfigEntry<float> configEntry, float min = 0f, float max = 100f)
        {
            var exampleSlider = new FloatSliderConfigItem(configEntry, new FloatSliderOptions
            {
                Min = min,
                Max = max,
                RequiresRestart = false
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        private void CreateIntConfig(ConfigEntry<int> configEntry, int min = 0, int max = 100)
        {
            var exampleSlider = new IntSliderConfigItem(configEntry, new IntSliderOptions()
            {
                Min = min,
                Max = max,
                RequiresRestart = false
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        private void CreateStringConfig(ConfigEntry<string> configEntry, bool requireRestart = false)
        {
            var exampleSlider = new TextInputFieldConfigItem(configEntry, new TextInputFieldOptions()
            {
                RequiresRestart = requireRestart
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        private void CreateBoolConfig(ConfigEntry<bool> configEntry, bool requireRestart = false)
        {
            var exampleSlider = new BoolCheckBoxConfigItem(configEntry, new BoolCheckBoxOptions()
            {
                RequiresRestart = requireRestart
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }


    }
}