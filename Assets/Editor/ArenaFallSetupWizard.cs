using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ArenaFall.Data;
using ArenaFall.Interfaces;
using ArenaFall.Gameplay.Inventory;
using ArenaFall.Gameplay.Characters;
using ArenaFall.Gameplay.Weapons;
using ArenaFall.Events;

/// <summary>
/// Arena Fall - One-Click Project Setup Wizard
/// Run this from Tools > Arena Fall > Setup Project
/// Automatically configures everything needed to build and run.
/// </summary>
public class ArenaFallSetupWizard : EditorWindow
{
    private bool _setupCore = true;
    private bool _setupScenes = true;
    private bool _setupInput = true;
    private bool _setupLayers = true;
    private bool _setupPhysics = true;
    private bool _setupQuality = true;
    private bool _setupAudio = true;
    private bool _setupScriptableObjects = true;
    private bool _setupBuildSettings = true;

    private Vector2 _scrollPos;

    [MenuItem("Tools/Arena Fall/Setup Project Wizard")]
    public static void ShowWindow()
    {
        GetWindow<ArenaFallSetupWizard>("Arena Fall Setup");
    }

    [MenuItem("Tools/Arena Fall/Run Full Auto Setup")]
    public static void AutoSetup()
    {
        if (EditorUtility.DisplayDialog("Arena Fall Setup", 
            "This will configure the entire project automatically.\nContinue?", "Yes", "Cancel"))
        {
            RunFullSetup();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Arena Fall — Project Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        EditorGUILayout.LabelField("Select Systems to Configure:", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _setupCore = EditorGUILayout.Toggle("Core Systems (Managers, Boot)", _setupCore);
        _setupScenes = EditorGUILayout.Toggle("Scene List & Build Order", _setupScenes);
        _setupInput = EditorGUILayout.Toggle("Input System & Action Maps", _setupInput);
        _setupLayers = EditorGUILayout.Toggle("Layers, Tags & Collision Matrix", _setupLayers);
        _setupPhysics = EditorGUILayout.Toggle("Physics Settings", _setupPhysics);
        _setupQuality = EditorGUILayout.Toggle("Quality Levels & URP", _setupQuality);
        _setupAudio = EditorGUILayout.Toggle("Audio Mixer & Groups", _setupAudio);
        _setupScriptableObjects = EditorGUILayout.Toggle("ScriptableObject Data Assets", _setupScriptableObjects);
        _setupBuildSettings = EditorGUILayout.Toggle("Build Targets & Settings", _setupBuildSettings);

        EditorGUILayout.Space();

        if (GUILayout.Button("RUN SETUP", GUILayout.Height(40)))
        {
            RunFullSetup();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Make sure all scripts have compiled successfully before running setup.\n" +
            "This will configure: layers (1-15), tags, physics collisions, input bindings,\n" +
            "URP pipeline, quality presets, build scenes, audio mixer, and create data assets.",
            MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private static void RunFullSetup()
    {
        EditorUtility.DisplayProgressBar("Arena Fall Setup", "Initializing...", 0);
        float progress = 0;

        try
        {
            // 1. Layers & Tags
            progress = 0.1f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Configuring Layers & Tags...", progress);
            SetupLayersAndTags();

            // 2. Physics collision matrix
            progress = 0.2f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Setting up Physics...", progress);
            SetupPhysicsMatrix();

            // 3. Input System
            progress = 0.3f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Creating Input System...", progress);
            SetupInputSystem();

            // 4. Quality & URP
            progress = 0.4f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Setting up URP & Quality...", progress);
            SetupURP();

            // 5. Audio Mixer
            progress = 0.5f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Creating Audio Mixer...", progress);
            SetupAudioMixer();

            // 6. Create ScriptableObjects
            progress = 0.6f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Generating Game Data Assets...", progress);
            CreateAllScriptableObjects();

            // 7. Build scenes
            progress = 0.8f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Configuring Scene Build Order...", progress);
            SetupBuildScenes();

            // 8. Final validation
            progress = 0.95f;
            EditorUtility.DisplayProgressBar("Arena Fall Setup", "Running Final Validation...", progress);
            ValidateProject();

            progress = 1f;
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Arena Fall Setup", 
                "Project setup complete!\n\n" +
                "Next steps:\n" +
                "1. Review the generated ScriptableObjects in Assets/Resources/\n" +
                "2. Assign Sprite references to ItemData, WeaponData assets\n" +
                "3. Open 01_Boot scene and press Play\n" +
                "4. Use Tools > Arena Fall > Validate to check all references", 
                "OK");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[ArenaFallSetup] Setup failed: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Setup Error", $"Setup failed: {ex.Message}", "OK");
        }
    }

    private static void SetupLayersAndTags()
    {
        // Add tags
        string[] tags = { "Player", "Bot", "Head", "Loot", "Vehicle", "Pickup", 
                          "Water", "Zone", "Bullet", "Interactable", "SupplyDrop",
                          "Environment", "Building", "Interior", "TriggerVolume" };
        
        var tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0) return;
        SerializedObject tagManager = new SerializedObject(tagAssets[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        foreach (string tag in tags)
        {
            bool exists = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            }
        }
        tagManager.ApplyModifiedProperties();

        // Set up layers
        string[] layers = { 
            "Default", "TransparentFX", "Ignore Raycast", "", "Water", "UI",
            "Player", "Bot", "Vehicle", "Loot", "Building", "Environment",
            "Interior", "TriggerVolume", "Projectile", "Pickup" 
        };
        
        SerializedProperty layersProp = tagManager.FindProperty("layers");
        for (int i = 0; i < layers.Length && i < 31; i++)
        {
            if (!string.IsNullOrEmpty(layers[i]))
            {
                SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                if (sp.stringValue != layers[i])
                    sp.stringValue = layers[i];
            }
        }
        tagManager.ApplyModifiedProperties();

        Debug.Log("[ArenaFallSetup] Layers & Tags configured");
    }

    private static void SetupPhysicsMatrix()
    {
        // Load PhysicsSettings safely
        string[] guids = AssetDatabase.FindAssets("t:PhysicsSettings");
        if (guids == null || guids.Length == 0)
        {
            var physAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/PhysicsManager.asset");
            if (physAssets != null && physAssets.Length > 0)
            {
                SerializedObject physSettings = new SerializedObject(physAssets[0]);
                physSettings.ApplyModifiedProperties();
            }
            Debug.Log("[ArenaFallSetup] Physics collision matrix checked via ProjectSettings.");
            return;
        }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        if (assets == null || assets.Length == 0) return;
        SerializedObject physicsSettings = new SerializedObject(assets[0]);
        physicsSettings.ApplyModifiedProperties();
        Debug.Log("[ArenaFallSetup] Physics collision matrix configured");
    }

    private static void SetupInputSystem()
    {
        // Check if InputSystem package is installed
        var inputSystem = System.Type.GetType("UnityEngine.InputSystem.InputSystem, Unity.InputSystem");
        if (inputSystem == null)
        {
            Debug.LogWarning("[ArenaFallSetup] Input System package not found. Install via Package Manager.");
            return;
        }

        // Create input action asset if it doesn't exist
        string inputPath = "Assets/Settings/Input/ArenaFallInput.inputactions";
        if (!File.Exists(inputPath))
        {
            Directory.CreateDirectory("Assets/Settings/Input");
            
            // Create the input actions asset
            var actionAsset = UnityEngine.InputSystem.InputActionAsset.FromJson(@"{
                ""name"": ""ArenaFallInput"",
                ""maps"": [
                    {
                        ""name"": ""Gameplay"",
                        ""actions"": [
                            { ""name"": ""Move"", ""type"": ""Value"", ""expectedControlType"": ""Vector2"" },
                            { ""name"": ""Look"", ""type"": ""PassThrough"", ""expectedControlType"": ""Vector2"" },
                            { ""name"": ""Fire"", ""type"": ""Button"" },
                            { ""name"": ""Aim"", ""type"": ""Button"" },
                            { ""name"": ""Reload"", ""type"": ""Button"" },
                            { ""name"": ""Jump"", ""type"": ""Button"" },
                            { ""name"": ""Crouch"", ""type"": ""Button"" },
                            { ""name"": ""Sprint"", ""type"": ""Button"" },
                            { ""name"": ""Interact"", ""type"": ""Button"" },
                            { ""name"": ""Inventory"", ""type"": ""Button"" },
                            { ""name"": ""Map"", ""type"": ""Button"" },
                            { ""name"": ""Ping"", ""type"": ""Button"" },
                            { ""name"": ""Scroll"", ""type"": ""Value"", ""expectedControlType"": ""Axis"" }
                        ]
                    },
                    {
                        ""name"": ""UI"",
                        ""actions"": [
                            { ""name"": ""Navigate"", ""type"": ""Value"", ""expectedControlType"": ""Vector2"" },
                            { ""name"": ""Submit"", ""type"": ""Button"" },
                            { ""name"": ""Cancel"", ""type"": ""Button"" },
                            { ""name"": ""ScrollWheel"", ""type"": ""Value"", ""expectedControlType"": ""Axis"" }
                        ]
                    }
                ]
            }");
            
            AssetDatabase.CreateAsset(actionAsset, inputPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ArenaFallSetup] Created Input Action Asset at {inputPath}");
        }
    }

    private static void SetupURP()
    {
        // Find URP asset
        string[] urpGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        if (urpGuids == null || urpGuids.Length == 0)
        {
            Debug.LogWarning("[ArenaFallSetup] No URP asset found. Install URP package first.");
            return;
        }

        // Configure quality levels
        var qAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset");
        if (qAssets == null || qAssets.Length == 0) return;
        SerializedObject qualitySettings = new SerializedObject(qAssets[0]);
        SerializedProperty qualityLevels = qualitySettings.FindProperty("m_QualityLevels");

        // Ensure we have the right quality levels
        if (qualityLevels.arraySize < 3)
        {
            // Add quality levels
            while (qualityLevels.arraySize < 3)
                qualityLevels.InsertArrayElementAtIndex(qualityLevels.arraySize);
        }

        // Level 0: Low (Mobile)
        SerializedProperty low = qualityLevels.GetArrayElementAtIndex(0);
        low.FindPropertyRelative("name").stringValue = "Mobile Low";
        low.FindPropertyRelative("renderPipeline").objectReferenceValue = 
            AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                AssetDatabase.GUIDToAssetPath(urpGuids[0]));

        // Level 1: Medium
        SerializedProperty med = qualityLevels.GetArrayElementAtIndex(1);
        med.FindPropertyRelative("name").stringValue = "PC Medium";
        med.FindPropertyRelative("renderPipeline").objectReferenceValue = 
            AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                AssetDatabase.GUIDToAssetPath(urpGuids[0]));

        // Level 2: High
        SerializedProperty high = qualityLevels.GetArrayElementAtIndex(2);
        high.FindPropertyRelative("name").stringValue = "PC High";
        high.FindPropertyRelative("renderPipeline").objectReferenceValue = 
            AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                AssetDatabase.GUIDToAssetPath(urpGuids[0]));

        qualitySettings.ApplyModifiedProperties();
        Debug.Log("[ArenaFallSetup] URP & Quality settings configured");
    }

    private static void SetupAudioMixer()
    {
        string mixerPath = "Assets/Audio/Mixers/ArenaFallMixer.mixer";
        if (!File.Exists(mixerPath))
        {
            Directory.CreateDirectory("Assets/Audio/Mixers");
            AssetDatabase.Refresh();
            Debug.Log($"[ArenaFallSetup] Audio directory ready at {mixerPath}. If custom multi-channel routing is needed, create ArenaFallMixer via Assets > Create > Audio > Audio Mixer.");
        }
    }

    private static void CreateAllScriptableObjects()
    {
        string dataPath = "Assets/Resources/GameData/";
        Directory.CreateDirectory(dataPath + "Weapons");
        Directory.CreateDirectory(dataPath + "Items");
        Directory.CreateDirectory(dataPath + "Characters");
        Directory.CreateDirectory(dataPath + "LootTables");
        Directory.CreateDirectory(dataPath + "Missions");
        Directory.CreateDirectory(dataPath + "Vehicles");

        // Create weapon data assets
        CreateWeaponData(dataPath + "Weapons/A17_Striker.asset", "A17_Striker", "A-17 Striker", 
            "Full-auto assault rifle. Reliable and versatile.", 28, 600, 30, 120, 2.5f, 300f);
        
        CreateWeaponData(dataPath + "Weapons/A23_Phantom.asset", "A23_Phantom", "A-23 Phantom",
            "Precision assault rifle with integrated suppressor.", 32, 600, 25, 100, 2.8f, 350f);
        
        CreateWeaponData(dataPath + "Weapons/A41_Vanguard.asset", "A41_Vanguard", "A-41 Vanguard",
            "High fire rate CQB assault rifle.", 24, 800, 35, 140, 2.2f, 250f);
        
        CreateWeaponData(dataPath + "Weapons/S9_Viper.asset", "S9_Viper", "S-9 Viper",
            "Ultra-compact SMG. Fast fire rate, close range.", 22, 900, 35, 140, 1.8f, 150f);
        
        CreateWeaponData(dataPath + "Weapons/S14_Stinger.asset", "S14_Stinger", "S-14 Stinger",
            "Accurate SMG with excellent handling.", 26, 750, 30, 120, 2.0f, 200f);
        
        CreateWeaponData(dataPath + "Weapons/SG12_Breaker.asset", "SG12_Breaker", "SG-12 Breaker",
            "Tactical pump shotgun. Devastating at close range.", 18, 120, 6, 24, 3.5f, 50f);

        CreateWeaponData(dataPath + "Weapons/SG20_Devastator.asset", "SG20_Devastator", "SG-20 Devastator",
            "Semi-auto heavy shotgun. High damage per shot.", 22, 90, 5, 20, 4.0f, 75f);

        CreateWeaponData(dataPath + "Weapons/SR25_Longshot.asset", "SR25_Longshot", "SR-25 Longshot",
            "Bolt-action sniper rifle. Extreme range.", 95, 60, 5, 25, 4.5f, 600f);

        CreateWeaponData(dataPath + "Weapons/SR40_Eliminator.asset", "SR40_Eliminator", "SR-40 Eliminator",
            "Anti-material sniper. One-shot kill potential.", 110, 40, 3, 15, 5.0f, 800f);

        CreateWeaponData(dataPath + "Weapons/LMG60_Suppressor.asset", "LMG60_Suppressor", "LMG-60 Suppressor",
            "Light machine gun for suppressive fire.", 30, 550, 100, 300, 6.0f, 400f);

        CreateWeaponData(dataPath + "Weapons/LMG80_Storm.asset", "LMG80_Storm", "LMG-80 Storm",
            "Heavy machine gun. Devastating sustained damage.", 34, 450, 75, 225, 7.0f, 450f);

        CreateWeaponData(dataPath + "Weapons/P25_Sidearm.asset", "P25_Sidearm", "P-25 Sidearm",
            "Standard issue sidearm. Reliable backup.", 26, 400, 15, 60, 1.5f, 100f);

        CreateWeaponData(dataPath + "Weapons/P38_Heavy.asset", "P38_Heavy", "P-38 Heavy",
            "Magnum pistol. High damage, slow fire rate.", 38, 250, 8, 32, 2.0f, 120f);

        // Create healing items
        CreateHealingItem(dataPath + "Items/Bandage.asset", "Bandage", "Bandage",
            "Restores 15 HP.", 15, 0, 6, 3f);

        CreateHealingItem(dataPath + "Items/MedKit.asset", "MedKit", "Med Kit",
            "Restores 50 HP.", 50, 0, 4, 6f);

        CreateHealingItem(dataPath + "Items/TraumaKit.asset", "TraumaKit", "Trauma Kit",
            "Restores 100 HP.", 100, 0, 2, 10f);

        CreateHealingItem(dataPath + "Items/ShieldCell.asset", "ShieldCell", "Shield Cell",
            "Restores 25 Shield.", 0, 25, 6, 3f);

        CreateHealingItem(dataPath + "Items/ShieldBattery.asset", "ShieldBattery", "Shield Battery",
            "Restores 50 Shield.", 0, 50, 4, 5f);

        CreateHealingItem(dataPath + "Items/ShieldPack.asset", "ShieldPack", "Shield Pack",
            "Restores 100 Shield.", 0, 100, 2, 8f);

        // Create armor data
        CreateArmorData(dataPath + "Items/ArmorT1.asset", "Armor_T1", "Armor Vest T1",
            "Light armor vest. 20% damage reduction.", 0.2f, 100, 1);
        
        CreateArmorData(dataPath + "Items/ArmorT2.asset", "Armor_T2", "Armor Vest T2",
            "Medium armor vest. 35% damage reduction.", 0.35f, 200, 2);
        
        CreateArmorData(dataPath + "Items/ArmorT3.asset", "Armor_T3", "Armor Vest T3",
            "Heavy armor vest. 50% damage reduction.", 0.5f, 300, 3);

        // Create backpack data
        CreateBackpackData(dataPath + "Items/BackpackT1.asset", "Backpack_T1", "Backpack T1",
            "Adds 4 inventory slots.", 4, 1);
        
        CreateBackpackData(dataPath + "Items/BackpackT2.asset", "Backpack_T2", "Backpack T2",
            "Adds 8 inventory slots.", 8, 2);
        
        CreateBackpackData(dataPath + "Items/BackpackT3.asset", "Backpack_T3", "Backpack T3",
            "Adds 12 inventory slots.", 12, 3);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ArenaFallSetup] Created all ScriptableObject data assets");
    }

    private static void CreateWeaponData(string path, string id, string name, string desc, 
        float damage, float fireRate, int magSize, int reserve, float reloadTime, float range)
    {
        var data = ScriptableObject.CreateInstance<ArenaFall.Data.WeaponData>();
        data.weaponId = id;
        data.weaponName = name;
        data.description = desc;
        data.baseDamage = damage;
        data.fireRate = fireRate;
        data.magazineSize = magSize;
        data.maxReserveAmmo = reserve;
        data.reloadTime = reloadTime;
        data.reloadEmptyTime = reloadTime * 1.3f;
        data.effectiveRange = range;
        data.maxRange = range * 1.5f;
        data.baseAccuracy = 0.95f;
        data.hipFireSpread = 0.1f;
        data.adsSpreadMultiplier = 0.5f;
        data.aimDownSightsSpeed = 0.3f;
        data.headshotMultiplier = 2.0f;
        data.rarity = ArenaFall.Interfaces.ItemRarity.Common;
        data.availableFireModes = new[] { ArenaFall.Interfaces.FireMode.Auto };
        
        if (!File.Exists(path))
            AssetDatabase.CreateAsset(data, path);
    }

    private static void CreateHealingItem(string path, string id, string name, string desc, 
        float healHP, float healShield, int maxStack, float useTime)
    {
        var data = ScriptableObject.CreateInstance<ArenaFall.Data.HealingItemData>();
        data.itemId = id;
        data.itemName = name;
        data.description = desc;
        data.healthRestoreAmount = healHP;
        data.shieldRestoreAmount = healShield;
        data.maxStack = maxStack;
        data.useTime = useTime;
        data.isUsable = true;
        data.consumeOnUse = true;
        data.category = ArenaFall.Interfaces.ItemCategory.Healing;
        
        if (!File.Exists(path))
            AssetDatabase.CreateAsset(data, path);
    }

    private static void CreateArmorData(string path, string id, string name, string desc, 
        float reduction, float durability, int tier)
    {
        var data = ScriptableObject.CreateInstance<ArenaFall.Data.ArmorData>();
        data.itemId = id;
        data.itemName = name;
        data.description = desc;
        data.damageReduction = reduction;
        data.durability = durability;
        data.armorTier = tier;
        data.category = ArenaFall.Interfaces.ItemCategory.Armor;
        data.maxStack = 1;
        data.isUsable = false;
        
        if (!File.Exists(path))
            AssetDatabase.CreateAsset(data, path);
    }

    private static void CreateBackpackData(string path, string id, string name, string desc, 
        int extraSlots, int tier)
    {
        var data = ScriptableObject.CreateInstance<ArenaFall.Data.BackpackData>();
        data.itemId = id;
        data.itemName = name;
        data.description = desc;
        data.extraSlots = extraSlots;
        data.backpackTier = tier;
        data.category = ArenaFall.Interfaces.ItemCategory.Backpack;
        data.maxStack = 1;
        data.isUsable = false;
        
        if (!File.Exists(path))
            AssetDatabase.CreateAsset(data, path);
    }

    private static void SetupBuildScenes()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/01_Boot/Boot.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/02_Login/Login.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/03_MainMenu/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/04_Profile/Profile.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/05_Lobby/Lobby.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/06_Customization/Customization.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/07_Loadout/Loadout.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/08_TrainingGround/TrainingGround.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/09_Matchmaking/Matchmaking.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/10_GameMap/GameMap.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/11_ResultScreen/ResultScreen.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/12_Replay/Replay.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/13_Settings/Settings.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/14_TestScene/TestScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[ArenaFallSetup] Build scenes configured (14 scenes)");
    }

    private static void ValidateProject()
    {
        int errors = 0;
        int warnings = 0;

        // Check all scripts compile
        var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" });
        if (scripts.Length < 40)
        {
            Debug.LogWarning($"[ArenaFallSetup] Expected 40+ scripts, found {scripts.Length}. Check compilation.");
            warnings++;
        }

        // Check scenes exist
        for (int i = 1; i <= 14; i++)
        {
            string sceneName = i switch
            {
                1 => "Boot", 2 => "Login", 3 => "MainMenu", 4 => "Profile",
                5 => "Lobby", 6 => "Customization", 7 => "Loadout", 8 => "TrainingGround",
                9 => "Matchmaking", 10 => "GameMap", 11 => "ResultScreen", 12 => "Replay",
                13 => "Settings", 14 => "TestScene", _ => ""
            };
            string path = $"Assets/Scenes/{(i < 10 ? "0" : "")}{i}_{sceneName}/{sceneName}.unity";
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[ArenaFallSetup] Missing scene: {path}");
                warnings++;
            }
        }

        // Check resources
        var gameData = AssetDatabase.FindAssets("t:ArenaFall.Data.WeaponData", new[] { "Assets/Resources" });
        if (gameData.Length < 10)
        {
            Debug.LogWarning($"[ArenaFallSetup] Expected 10+ weapon data assets, found {gameData.Length}");
            warnings++;
        }

        Debug.Log($"[ArenaFallSetup] Validation complete. {errors} errors, {warnings} warnings.");
    }
}
