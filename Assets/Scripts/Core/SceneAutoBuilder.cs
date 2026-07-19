using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using ArenaFall.Core;
using ArenaFall.Data;
using ArenaFall.Managers;
using ArenaFall.UI.MainMenu;
using ArenaFall.UI.HUD;
using ArenaFall.Gameplay.Characters;
using ArenaFall.Gameplay.Weapons;
using ArenaFall.Gameplay.Inventory;
using ArenaFall.Gameplay.Zone;
using ArenaFall.Networking;

/// <summary>
/// AUTO-GENERATES all GameObjects and UI layouts when a scene loads.
/// Built strictly following the original Arena Fall Art & UI Style Guides (Clean Glassmorphic Sci-Fi).
/// Palette: Deep Navy (#0A1628), Steel Blue (#1E3A5F), Holographic Cyan (#00D4FF), and Neon Orange (#FF6B35).
/// </summary>
public class SceneAutoBuilder : MonoBehaviour
{
    private static bool _initialized;

    // Arena Fall Official Sci-Fi Theme Color System
    private static readonly Color DeepNavyBg = new Color(0.039f, 0.086f, 0.157f, 0.92f);   // #0A1628
    private static readonly Color SteelBlueCard = new Color(0.118f, 0.227f, 0.373f, 0.88f); // #1E3A5F
    private static readonly Color HolographicCyan = new Color(0f, 0.831f, 1f, 1f);        // #00D4FF
    private static readonly Color NeonOrangeAccent = new Color(1f, 0.42f, 0.208f, 1f);     // #FF6B35
    private static readonly Color ShieldCyanGlow = new Color(0f, 0.831f, 1f, 0.85f);
    private static readonly Color SuccessGreen = new Color(0.267f, 1f, 0.333f, 1f);       // #44FF55

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitOnPlay()
    {
        if (_initialized) return;
        var builderObj = new GameObject("[AUTO] SceneAutoBuilder");
        builderObj.AddComponent<SceneAutoBuilder>();
    }

    private void Awake()
    {
        if (_initialized) return;
        _initialized = true;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        Debug.Log("[SceneAutoBuilder] Active — Clean Glassmorphic Sci-Fi Theme Engaged");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneAutoBuilder] Auto-generating scene '{scene.name}' using Arena Fall Sci-Fi Theme.");

        if (!scene.name.Contains("Boot"))
        {
            EnsureCoreManagersExist();
        }

        CleanupPreviousBuild(scene);

        // Normalize numbered scene names seamlessly
        string sName = scene.name.Replace("01_", "").Replace("02_", "").Replace("03_", "")
                                  .Replace("04_", "").Replace("05_", "").Replace("06_", "")
                                  .Replace("07_", "").Replace("08_", "").Replace("09_", "")
                                  .Replace("10_", "").Replace("11_", "").Replace("12_", "")
                                  .Replace("13_", "").Replace("14_", "");

        switch (sName)
        {
            case "Boot":
                BuildBootScene(scene);
                break;
            case "Login":
                BuildLoginScene(scene);
                break;
            case "MainMenu":
                BuildMainMenuScene(scene);
                break;
            case "Profile":
                BuildProfileScene(scene);
                break;
            case "Lobby":
                BuildLobbyScene(scene);
                break;
            case "Customization":
                BuildCustomizationScene(scene);
                break;
            case "Loadout":
                BuildLoadoutScene(scene);
                break;
            case "TrainingGround":
                BuildTrainingScene(scene);
                break;
            case "Matchmaking":
                BuildMatchmakingScene(scene);
                break;
            case "GameMap":
                BuildGameMapScene(scene);
                break;
            case "ResultScreen":
                BuildResultScene(scene);
                break;
            case "Replay":
                BuildReplayScene(scene);
                break;
            case "Settings":
                BuildSettingsScene(scene);
                break;
            case "TestScene":
                BuildTestScene(scene);
                break;
            default:
                BuildEssentialSystems(scene);
                break;
        }

        Debug.Log($"[SceneAutoBuilder] ✓ Scene '{scene.name}' complete.");
    }

    private void CleanupPreviousBuild(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name.StartsWith("[AUTO]"))
            {
                DestroyImmediate(root);
            }
        }
    }

    // ─── BOOT SCENE ────────────────────────────────────────────
    private void BuildBootScene(Scene scene)
    {
        var bootCamObj = new GameObject("[AUTO] BootCamera");
        var bootCam = bootCamObj.AddComponent<Camera>();
        bootCam.clearFlags = CameraClearFlags.SolidColor;
        bootCam.backgroundColor = new Color(0.039f, 0.086f, 0.157f, 1f);
        bootCamObj.tag = "MainCamera";
        bootCamObj.AddComponent<AudioListener>();
        SceneManager.MoveGameObjectToScene(bootCamObj, scene);

        var bootObj = new GameObject("[AUTO] Boot Systems");
        bootObj.AddComponent<Bootstrapper>()._initializeOnAwake = true;
        
        var gameManager = new GameObject("[AUTO] GameManager");
        gameManager.AddComponent<GameManager>();
        gameManager.AddComponent<SaveManager>();
        gameManager.AddComponent<AudioManager>();
        gameManager.AddComponent<InputManager>();
        gameManager.AddComponent<CameraManager>();
        gameManager.AddComponent<SceneLoader>();
        gameManager.AddComponent<PoolManager>();
        gameManager.AddComponent<GameStateManager>();
        gameManager.AddComponent<SettingsManager>();
        gameManager.AddComponent<MatchManager>();
        gameManager.AddComponent<LootManager>();
        gameManager.AddComponent<ProgressionManager>();
        gameManager.AddComponent<LocalizationManager>();
        gameManager.AddComponent<AnalyticsManager>();
        gameManager.AddComponent<SafeZone>();
        gameManager.AddComponent<BackendClient>();
        gameManager.AddComponent<NetworkManagerSetup>();
        
        var evt = new GameObject("[AUTO] EventSystem");
        evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        CreateSplashScreen(scene);
    }

    private void CreateSplashScreen(Scene scene)
    {
        var canvas = CreateCanvas(scene, "SplashCanvas", 0);
        
        var splashTexture = Resources.Load<Texture2D>("UI/Sprites/splash_screen");
        var bg = CreateImage(canvas.transform, "SplashBG", new Color(0.039f, 0.086f, 0.157f, 1f), 
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        if (splashTexture != null)
        {
            var img = bg.GetComponent<Image>();
            img.sprite = Sprite.Create(splashTexture, new Rect(0, 0, splashTexture.width, splashTexture.height), new Vector2(0.5f, 0.5f));
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
        }
        
        var logoTexture = Resources.Load<Texture2D>("UI/Sprites/arena_fall_logo");
        var logo = CreateImage(bg.transform, "Logo", Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var rt = logo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(480, 210);
        rt.anchoredPosition = Vector2.zero;
        
        if (logoTexture != null)
        {
            var logoImg = logo.GetComponent<Image>();
            logoImg.sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
            logoImg.type = Image.Type.Simple;
            logoImg.preserveAspect = true;
        }
        
        var loadingText = CreateText(bg.transform, "LoadingText", "INITIALIZING ARENA FALL...", 20, HolographicCyan,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        var lrt = loadingText.GetComponent<RectTransform>();
        lrt.anchoredPosition = new Vector2(0, 50);
        
        var loader = bg.AddComponent<AutoSceneLoader>();
        loader.sceneName = "Login";
        loader.delay = 1.8f;
    }

    // ─── MAIN MENU (CLEAN GLASSMORPHIC SCI-FI LAYOUT) ───────────
    private void BuildMainMenuScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        EnsureCoreManagersExist();
        
        var canvas = CreateCanvas(scene, "MainMenuCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        var bgTexture = Resources.Load<Texture2D>("Art/Environment/skybox_concept");
        var logoTexture = Resources.Load<Texture2D>("UI/Sprites/arena_fall_logo");
        
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        var bgOverlay = CreateImage(bg.transform, "BGEffects", new Color(1, 1, 1, 0.45f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        if (bgTexture != null)
        {
            var bgImg = bgOverlay.GetComponent<Image>();
            bgImg.sprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0.5f));
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
        }
        
        CreateImage(bg.transform, "DarkOverlay", new Color(0, 0, 0, 0.55f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        // ─── 1. TOP HEADER (PLAYER PROFILE & CURRENCIES) ────
        var topHeader = CreatePanel(bg.transform, "TopHeaderBar", DeepNavyBg,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1));
        var thRt = topHeader.GetComponent<RectTransform>();
        thRt.sizeDelta = new Vector2(0, 68);
        thRt.anchoredPosition = Vector2.zero;

        // Fetch dynamic profile fields
        string playerName = SaveManager.Instance?.GetPlayerName() ?? BackendClient.Instance?.CachedProfile?.playerName ?? "VANGUARD-01";
        int level = SaveManager.Instance?.CurrentSave?.level ?? BackendClient.Instance?.CachedProfile?.level ?? 1;
        int credits = SaveManager.Instance?.CurrentSave?.credits ?? BackendClient.Instance?.CachedProfile?.credits ?? 0;
        int premium = SaveManager.Instance?.CurrentSave?.premiumCurrency ?? BackendClient.Instance?.CachedProfile?.premiumCurrency ?? 0;
        int wins = SaveManager.Instance?.CurrentSave?.stats?.wins ?? 0;
        string rankTitle = wins >= 10 ? "GRANDMASTER" : (level >= 10 ? "HEROIC" : "RECRUIT");

        // Player Profile Card
        var profileBox = CreatePanel(topHeader.transform, "ProfileBox", SteelBlueCard,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var pRt = profileBox.GetComponent<RectTransform>();
        pRt.sizeDelta = new Vector2(280, 52);
        pRt.anchoredPosition = new Vector2(15, 0);

        var pName = CreateText(profileBox.transform, "PName", playerName.ToUpper(), 15, Color.white,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var pnRt = pName.GetComponent<RectTransform>();
        pnRt.anchoredPosition = new Vector2(10, 8);

        var pLvl = CreateText(profileBox.transform, "PLvl", $"⚡ {rankTitle} | LVL {level}", 11, HolographicCyan,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var plRt = pLvl.GetComponent<RectTransform>();
        plRt.anchoredPosition = new Vector2(10, -10);

        // Currency Counters
        var goldPill = CreatePanel(topHeader.transform, "GoldPill", SteelBlueCard,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
        var gRt = goldPill.GetComponent<RectTransform>();
        gRt.sizeDelta = new Vector2(140, 38);
        gRt.anchoredPosition = new Vector2(-230, 0);
        CreateText(goldPill.transform, "GText", $"🪙 {credits:N0}", 13, NeonOrangeAccent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var diamondPill = CreatePanel(topHeader.transform, "DiamondPill", SteelBlueCard,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
        var dRt = diamondPill.GetComponent<RectTransform>();
        dRt.sizeDelta = new Vector2(130, 38);
        dRt.anchoredPosition = new Vector2(-85, 0);
        CreateText(diamondPill.transform, "DText", $"💎 {premium:N0}", 13, HolographicCyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        CreateMenuButton(topHeader.transform, "SettingsTopBtn", "⚙", 18, ButtonStyle.Tertiary,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-18, 0), new Vector2(42, 38), () => LoadSceneSafely("Settings"));

        // ─── 2. LEFT SIDE NAVIGATION RAIL ──────────────────────────
        var leftDock = CreatePanel(bg.transform, "LeftDockRail", new Color(0, 0, 0, 0),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var ldRt = leftDock.GetComponent<RectTransform>();
        ldRt.anchoredPosition = new Vector2(120, -20);
        ldRt.sizeDelta = new Vector2(190, 480);

        string[] dockLabels = { "🛒 STORE", "🎁 BATTLE PASS", "👤 CHARACTERS", "🔫 ARMORY", "🐾 PETS & MASTERY", "🛡️ GUILD", "🏆 CAREER STATS" };
        for (int i = 0; i < dockLabels.Length; i++)
        {
            string label = dockLabels[i];
            float yPos = 180 - (i * 60);
            CreateMenuButton(leftDock.transform, $"DockBtn_{i}", label, 13, ButtonStyle.Secondary,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, yPos), new Vector2(180, 48), () => {
                    if (label.Contains("STORE") || label.Contains("PASS")) LoadSceneSafely("Customization");
                    else if (label.Contains("ARMORY")) LoadSceneSafely("Loadout");
                    else if (label.Contains("CAREER") || label.Contains("CHARACTERS")) LoadSceneSafely("Profile");
                    else LoadSceneSafely("Customization");
                });
        }

        // ─── 3. CENTER BRANDING LOGO ────────────────────────────────
        var logo = CreateImage(bg.transform, "Logo", Color.white,
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var lrt = logo.GetComponent<RectTransform>();
        lrt.sizeDelta = new Vector2(500, 200);
        if (logoTexture != null)
        {
            var logoImg = logo.GetComponent<Image>();
            logoImg.sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
            logoImg.type = Image.Type.Simple;
            logoImg.preserveAspect = true;
        }

        // ─── 4. BOTTOM RIGHT MATCH ACTION CORNER ───────────────────
        var modeSelector = CreatePanel(bg.transform, "ModeSelectorBox", SteelBlueCard,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
        var msRt = modeSelector.GetComponent<RectTransform>();
        msRt.sizeDelta = new Vector2(290, 52);
        msRt.anchoredPosition = new Vector2(-20, 138);

        int maxMatchPlayers = MatchManager.Instance != null ? MatchManager.Instance.TotalPlayerCount : 60;
        CreateText(modeSelector.transform, "ModeTxt", "BATTLE ROYALE (NEXUS TOWER)", 14, Color.white,
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f));
        CreateText(modeSelector.transform, "SubTxt", $"SOLO / DUO / SQUAD ({maxMatchPlayers} PLAYERS)", 10, HolographicCyan,
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f));

        // PRIMARY CTA START BUTTON
        CreateMenuButton(bg.transform, "StartMatchBtn", "🚀 FIND MATCH", 24, ButtonStyle.Primary,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-20, 30), new Vector2(290, 92), () => {
                if (NetworkManagerSetup.Instance != null) NetworkManagerSetup.Instance.StartHost();
                LoadSceneSafely("GameMap");
            });

        // Practice Shooting Range
        CreateMenuButton(bg.transform, "TrainingBtn", "🎯 RANGE", 14, ButtonStyle.Tertiary,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-320, 30), new Vector2(115, 92), () => LoadSceneSafely("TrainingGround"));
    }

    // ─── GAME MAP (ACTIVE BATTLE ROYALE GAMEPLAY) ────────────────
    private void BuildGameMapScene(Scene scene)
    {
        if (NetworkManagerSetup.Instance != null)
        {
            NetworkManagerSetup.Instance.InitializeNetcode();
        }

        var terrainObj = new GameObject("[AUTO] Terrain");
        var terrain = terrainObj.AddComponent<Terrain>();
        var terrainData = new TerrainData();
        terrainData.size = new Vector3(4000, 200, 4000);
        terrainData.heightmapResolution = 512;
        terrain.terrainData = terrainData;
        
        var terrainCollider = terrainObj.AddComponent<TerrainCollider>();
        terrainCollider.terrainData = terrainData;

        MapPOIBuilder.BuildFullBattleRoyaleMap(terrainObj);
        
        var lightObj = new GameObject("[AUTO] Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.95f, 0.88f, 0.75f);
        light.intensity = 1.25f;
        lightObj.transform.rotation = Quaternion.Euler(42, 125, 0);
        light.shadows = LightShadows.Soft;
        
        var playerObj = new GameObject("[AUTO] Player");
        playerObj.transform.position = new Vector3(2000, 12, 2000);
        
        var cc = playerObj.AddComponent<CharacterController>();
        cc.height = 2; cc.radius = 0.4f; cc.center = new Vector3(0, 1, 0);
        
        playerObj.AddComponent<PlayerCharacterController>();
        var health = playerObj.AddComponent<CharacterHealth>();
        var inventory = playerObj.AddComponent<Inventory>();

        SciFiCharacterAndVehicleBuilder.Build3DSciFiCharacterRig(playerObj.transform, "Player3DRig", true, HolographicCyan);

        var weaponObj = new GameObject("EquippedWeapon");
        weaponObj.transform.SetParent(playerObj.transform);
        var wc = weaponObj.AddComponent<WeaponController>();
        var wData = ScriptableObject.CreateInstance<WeaponData>();
        wData.weaponId = "a17_striker";
        wData.weaponName = "A17 Striker Assault";
        wData.baseDamage = 35f;
        wData.magazineSize = 30;
        wData.maxReserveAmmo = 180;
        wc.Equip(wData);
        
        var camObj = new GameObject("[AUTO] MainCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 70; cam.nearClipPlane = 0.1f; cam.farClipPlane = 5000;
        camObj.tag = "MainCamera"; camObj.AddComponent<AudioListener>();
        
        var camManager = FindObjectOfType<CameraManager>();
        if (camManager != null) camManager.SetTarget(playerObj.transform);
        
        var zoneObj = new GameObject("[AUTO] SafeZone");
        zoneObj.AddComponent<SafeZone>();
        
        playerObj.AddComponent<AudioSource>().spatialBlend = 1;
        
        BuildHUD(scene);
        
        SpawnAIBots();
        SpawnLoot();
        
        var evt = new GameObject("[AUTO] EventSystem");
        evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // ─── HUD (IN-GAME BATTLE ROYALE HUD) ──────────────────────────
    private void BuildHUD(Scene scene)
    {
        var canvas = CreateCanvas(scene, "HUDCanvas", 10);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        int aliveCount = MatchManager.Instance != null ? MatchManager.Instance.AlivePlayerCount : 60;
        string playerId = SaveManager.Instance?.CurrentSave?.playerId ?? "local";
        int killsCount = MatchManager.Instance != null ? MatchManager.Instance.GetPlayerKills(playerId) : 0;

        // Match Stats Box
        var matchInfoBox = CreatePanel(canvas.transform, "AF_MatchInfoBox", DeepNavyBg,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
        var miRt = matchInfoBox.GetComponent<RectTransform>();
        miRt.sizeDelta = new Vector2(240, 50);
        miRt.anchoredPosition = new Vector2(15, -15);

        CreateText(matchInfoBox.transform, "AliveTxt", $"ALIVE: {aliveCount}", 14, Color.white,
            new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f));

        CreateText(matchInfoBox.transform, "KillsTxt", $"KILLS: {killsCount}", 14, HolographicCyan,
            new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f), new Vector2(0.75f, 0.5f));

        // Top-Right Minimap Frame
        var minimap = CreatePanel(canvas.transform, "Minimap", DeepNavyBg,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
        var mrt = minimap.GetComponent<RectTransform>();
        mrt.sizeDelta = new Vector2(175, 175);
        mrt.anchoredPosition = new Vector2(-15, -15);
        
        var mmBorder = CreateImage(minimap.transform, "MinimapBorder", HolographicCyan,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var brt2 = mmBorder.GetComponent<RectTransform>();
        brt2.sizeDelta = new Vector2(173, 173);
        mmBorder.GetComponent<Image>().fillCenter = false;
        
        // HP & Shield Container
        var hpBox = CreatePanel(canvas.transform, "AF_HealthBox", DeepNavyBg,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        var hRt = hpBox.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(270, 44);
        hRt.anchoredPosition = new Vector2(0, 25);

        // EP Shield Gauge (Cyan)
        var epBarBg = CreateImage(hpBox.transform, "EPBarBG", new Color(0.1f, 0.15f, 0.25f, 0.85f),
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f));
        epBarBg.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 6);

        var epFill = CreateImage(epBarBg.transform, "EPFill", HolographicCyan,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        epFill.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 6);
        epFill.GetComponent<Image>().type = Image.Type.Filled;

        // HP Gauge (Green)
        var hpBarBg = CreateImage(hpBox.transform, "HPBarBG", new Color(0.1f, 0.15f, 0.25f, 0.85f),
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f));
        hpBarBg.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 15);

        var hpFill = CreateImage(hpBarBg.transform, "HPFill", SuccessGreen,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        hpFill.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 15);
        hpFill.GetComponent<Image>().type = Image.Type.Filled;

        CreateText(hpBarBg.transform, "HPTxt", "HP  200 / 200", 12, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        // Reticle Crosshair
        var crosshairPanel = CreatePanel(canvas.transform, "Crosshair", new Color(0, 0, 0, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        crosshairPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

        var dot = CreateImage(crosshairPanel.transform, "CenterDot", NeonOrangeAccent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        dot.GetComponent<RectTransform>().sizeDelta = new Vector2(4, 4);

        for (int i = 0; i < 4; i++)
        {
            var line = CreateImage(crosshairPanel.transform, $"Line{i}", HolographicCyan,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var lrt = line.GetComponent<RectTransform>();
            float x = i == 0 ? 8 : (i == 1 ? -8 : 0);
            float y = i == 2 ? 8 : (i == 3 ? -8 : 0);
            lrt.anchoredPosition = new Vector2(x, y);
            lrt.sizeDelta = new Vector2(i < 2 ? 8 : 2, i < 2 ? 2 : 8);
        }

        canvas.AddComponent<HUDController>();
    }

    // ─── RESULT SCREEN (MATCH SUMMARY) ───────────────────────────
    private void BuildResultScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        
        var canvas = CreateCanvas(scene, "ResultCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));

        string pId = SaveManager.Instance?.CurrentSave?.playerId ?? "local";
        int place = MatchManager.Instance != null ? MatchManager.Instance.GetPlayerPlacement(pId) : 1;
        int kills = MatchManager.Instance != null ? MatchManager.Instance.GetPlayerKills(pId) : 0;
        int damage = MatchManager.Instance != null ? MatchManager.Instance.GetPlayerDamage(pId) : 0;
        float survival = MatchManager.Instance != null ? MatchManager.Instance.MatchElapsedTime : 0f;
        int xpEarned = (kills * 150) + (place == 1 ? 500 : 200);

        if (BackendClient.Instance != null)
        {
            BackendClient.Instance.SyncStatsAfterMatch(kills, damage, place, survival);
        }

        string headerText = place == 1 ? "🏆 VICTORY ROYALE 🏆" : $"PLACEMENT #{place}";
        CreateText(bg.transform, "VictoryTitle", headerText, 46, HolographicCyan,
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f));

        CreateText(bg.transform, "SubTitle", $"SURVIVAL TIME: {(int)(survival/60)}m {(int)(survival%60)}s | XP EARNED: +{xpEarned:N0}", 15, Color.white,
            new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f));

        string[] stats = { "KILLS", "DAMAGE", "SURVIVED", "TOTAL XP" };
        string[] values = { kills.ToString(), damage.ToString("N0"), $"{(int)(survival/60)}:{(int)(survival%60):D2}", xpEarned.ToString("N0") };
        for (int i = 0; i < 4; i++)
        {
            var statCard = CreatePanel(bg.transform, $"Card_{i}", SteelBlueCard,
                new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            var sRt = statCard.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(135, 85);
            sRt.anchoredPosition = new Vector2(-210 + (i * 140), 0);

            CreateText(statCard.transform, "Val", values[i], 26, NeonOrangeAccent, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f));
            CreateText(statCard.transform, "Lab", stats[i], 11, Color.gray, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f));
        }

        CreateMenuButton(bg.transform, "PlayAgainBtn", "🚀 PLAY AGAIN", 20, ButtonStyle.Primary,
            new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f),
            new Vector2(-90, 0), new Vector2(200, 50), () => LoadSceneSafely("GameMap"));

        CreateMenuButton(bg.transform, "MenuBtn", "MAIN MENU", 16, ButtonStyle.Secondary,
            new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f),
            new Vector2(120, 0), new Vector2(180, 50), () => LoadSceneSafely("MainMenu"));
    }

    // ─── PROFILE CAREER SCENE (LIVE STATS) ─────────────────────
    private void BuildProfileScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        var canvas = CreateCanvas(scene, "ProfileCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        var save = SaveManager.Instance?.CurrentSave;
        int wins = save?.stats?.wins ?? 0;
        int kills = save?.stats?.kills ?? 0;
        int deaths = save?.stats?.deaths ?? 0;
        int damage = save?.stats?.damageDealt ?? 0;
        float kdr = deaths > 0 ? (float)kills / deaths : kills;

        CreateText(bg.transform, "Title", "CAREER STATS & PROGRESSION", 28, HolographicCyan, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f));
        
        string[] labels = { "WINS", "KILLS", "DAMAGE", "K/D RATIO" };
        string[] values = { wins.ToString("N0"), kills.ToString("N0"), damage.ToString("N0"), kdr.ToString("F2") };
        for (int i = 0; i < 4; i++)
        {
            var card = CreatePanel(bg.transform, $"StatCard_{i}", SteelBlueCard, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 100);
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(-270 + (i * 180), 0);

            CreateText(card.transform, "Val", values[i], 30, NeonOrangeAccent, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f));
            CreateText(card.transform, "Lab", labels[i], 12, Color.gray, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f));
        }

        CreateMenuButton(bg.transform, "BackBtn", "← BACK TO MAIN MENU", 16, ButtonStyle.Tertiary, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0, 0), new Vector2(220, 45), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildLobbyScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        var canvas = CreateCanvas(scene, "LobbyCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        CreateText(bg.transform, "Title", "MATCH LOBBY", 30, HolographicCyan, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f));
        CreateMenuButton(bg.transform, "StartButton", "🚀 START MATCH", 22, ButtonStyle.Primary, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0, 0), new Vector2(260, 55), () => {
            if (NetworkManagerSetup.Instance != null) NetworkManagerSetup.Instance.StartHost();
            LoadSceneSafely("GameMap");
        });
        CreateMenuButton(bg.transform, "BackButton", "← BACK", 16, ButtonStyle.Tertiary, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(100, 36), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildTrainingScene(Scene scene)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "[AUTO] Ground";
        ground.transform.localScale = new Vector3(60, 1, 60);
        ground.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.18f, 0.25f, 0.35f) };
        
        var playerObj = new GameObject("[AUTO] Player");
        playerObj.transform.position = new Vector3(0, 1, 0);
        var cc = playerObj.AddComponent<CharacterController>();
        cc.height = 2; cc.radius = 0.4f; cc.center = new Vector3(0, 1, 0);
        playerObj.AddComponent<PlayerCharacterController>();
        playerObj.AddComponent<CharacterHealth>();
        playerObj.AddComponent<Inventory>();
        
        SciFiCharacterAndVehicleBuilder.Build3DSciFiCharacterRig(playerObj.transform, "PlayerTraining3DRig", true, HolographicCyan);
        
        for (int i = 0; i < 5; i++)
        {
            var dummy = new GameObject($"[AUTO] TargetDummy_{i}");
            dummy.transform.position = new Vector3(10 + (i * 5), 1, 8);
            dummy.AddComponent<CapsuleCollider>().height = 2f;
            dummy.AddComponent<CharacterHealth>();
            SciFiCharacterAndVehicleBuilder.Build3DSciFiCharacterRig(dummy.transform, $"DummyRig_{i}", false, NeonOrangeAccent);
            dummy.AddComponent<RotateAnimation>();
        }
        
        var camObj = new GameObject("[AUTO] MainCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 70; camObj.tag = "MainCamera"; camObj.AddComponent<AudioListener>();
        
        var camManager = FindObjectOfType<CameraManager>();
        if (camManager != null) camManager.SetTarget(playerObj.transform);
        
        BuildHUD(scene);
        
        var evt = new GameObject("[AUTO] EventSystem");
        evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        var canvas = CreateCanvas(scene, "UICanvas", 1);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        CreateMenuButton(canvas.transform, "BackButton", "← MAIN MENU", 16, ButtonStyle.Tertiary,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(15, -15), new Vector2(140, 35), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildSettingsScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        var canvas = CreateCanvas(scene, "SettingsCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        CreateText(bg.transform, "Title", "SETTINGS & CONTROLS", 28, HolographicCyan, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f));
        CreateMenuButton(bg.transform, "BackButton", "← BACK", 18, ButtonStyle.Tertiary, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0, 0), new Vector2(200, 48), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildTestScene(Scene scene) { }

    private void SpawnAIBots()
    {
        int botCount = 59;
        if (BackendClient.Instance != null && BackendClient.Instance.LastMatchBotCount > 0)
        {
            botCount = BackendClient.Instance.LastMatchBotCount;
        }
        SciFiCharacterAndVehicleBuilder.Spawn3DAIBotsAcrossCompounds(botCount);
    }

    private void SpawnLoot()
    {
        SciFiCharacterAndVehicleBuilder.SpawnDrivableVehiclesAcrossMap();
    }

    private void BuildLoginScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        EnsureCoreManagersExist();

        var canvas = CreateCanvas(scene, "LoginCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));

        CreateText(bg.transform, "Title", "ARENA FALL LOGIN", 30, HolographicCyan, new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f));

        CreateMenuButton(bg.transform, "GuestButton", "⚡ PLAY AS GUEST", 18, ButtonStyle.Primary, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0, 0), new Vector2(300, 52), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildCustomizationScene(Scene scene)
    {
        var canvas = CreateCanvas(scene, "CustomCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        CreateText(bg.transform, "Title", "ARMORY & STORE", 28, HolographicCyan, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f));
        CreateMenuButton(canvas.transform, "BackBtn", "← MAIN MENU", 16, ButtonStyle.Primary, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0, 0), new Vector2(200, 45), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildLoadoutScene(Scene scene)
    {
        var canvas = CreateCanvas(scene, "LoadoutCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        CreateText(bg.transform, "Title", "ARMORY & SKINS", 28, HolographicCyan, new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f));
        CreateMenuButton(canvas.transform, "BackBtn", "← MAIN MENU", 16, ButtonStyle.Primary, new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0, 0), new Vector2(200, 45), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildMatchmakingScene(Scene scene)
    {
        var canvas = CreateCanvas(scene, "MatchmakingCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        CreateText(bg.transform, "Title", "MATCHMAKING QUEUE...", 28, HolographicCyan, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f));
        CreateMenuButton(bg.transform, "CancelSearch", "CANCEL", 16, ButtonStyle.Tertiary, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0, 0), new Vector2(180, 45), () => LoadSceneSafely("MainMenu"));
    }

    private void BuildReplayScene(Scene scene) { }

    private void BuildEssentialSystems(Scene scene)
    {
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var evt = new GameObject("[AUTO] EventSystem");
            evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private static void LoadSceneSafely(string sceneTarget)
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(sceneTarget);
        }
        else
        {
            try
            {
                SceneManager.LoadScene(sceneTarget);
            }
            catch
            {
                Debug.LogWarning($"[SceneAutoBuilder] Fallback scene load for '{sceneTarget}'");
            }
        }
    }

    // ─── HELPERS ───────────────────────────────────────────────
    private GameObject CreateCanvas(Scene scene, string name, int sortOrder)
    {
        if (Camera.main == null && FindObjectOfType<Camera>() == null)
        {
            var camObj = new GameObject("[AUTO] UICamera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.039f, 0.086f, 0.157f, 1f);
            camObj.tag = "MainCamera";
            if (FindObjectOfType<AudioListener>() == null) camObj.AddComponent<AudioListener>();
            SceneManager.MoveGameObjectToScene(camObj, scene);
        }

        var obj = new GameObject($"[AUTO] {name}", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = obj.GetComponent<Canvas>();
        canvas.sortingOrder = sortOrder;
        var scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        SceneManager.MoveGameObjectToScene(obj, scene);
        return obj;
    }

    private GameObject CreatePanel(Transform parent, string name, Color color, 
        Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax, Vector2 pivotMax)
    {
        var obj = new GameObject($"[AUTO] {name}", typeof(RectTransform), typeof(CanvasRenderer));
        var image = obj.AddComponent<Image>();
        image.color = color;
        var rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.sizeDelta = Vector2.zero;
        return obj;
    }

    private GameObject CreateImage(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax, Vector2 pivotMax)
    {
        var obj = new GameObject($"[AUTO] {name}", typeof(RectTransform), typeof(CanvasRenderer));
        var image = obj.AddComponent<Image>();
        image.color = color;
        var rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.sizeDelta = Vector2.zero;
        return obj;
    }

    private GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color,
        Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax, Vector2 pivotMax)
    {
        var obj = new GameObject($"[AUTO] {name}", typeof(RectTransform), typeof(CanvasRenderer));
        var tmp = obj.AddComponent<UnityEngine.UI.Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAnchor.MiddleCenter;
        tmp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        var rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.sizeDelta = new Vector2(250, 32);
        return obj;
    }

    private enum ButtonStyle { Primary, Secondary, Tertiary, Accent, Danger }

    private void CreateMenuButton(Transform parent, string name, string label, int fontSize, 
        ButtonStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivotMin, Vector2 pivotMax,
        Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObj = new GameObject($"[AUTO] {name}", typeof(RectTransform), typeof(CanvasRenderer));
        var button = buttonObj.AddComponent<Button>();
        var rt = buttonObj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivotMin;
        rt.anchoredPosition = position; rt.sizeDelta = size;

        var outerImage = buttonObj.AddComponent<Image>();
        
        Color outerBorderColor = HolographicCyan;
        Color innerBodyColor = SteelBlueCard;
        Color textColor = Color.white;
        Color accentBraceColor = HolographicCyan;

        if (style == ButtonStyle.Primary) // Neon Orange Primary CTA
        {
            outerBorderColor = NeonOrangeAccent;
            innerBodyColor = NeonOrangeAccent;
            textColor = Color.white;
            accentBraceColor = Color.white;
        }
        else if (style == ButtonStyle.Secondary)
        {
            outerBorderColor = new Color(0f, 0.831f, 1f, 0.4f);
            innerBodyColor = DeepNavyBg;
            textColor = new Color(0.9f, 0.95f, 1f, 1f);
            accentBraceColor = HolographicCyan;
        }
        else if (style == ButtonStyle.Tertiary)
        {
            outerBorderColor = new Color(0.2f, 0.35f, 0.5f, 0.5f);
            innerBodyColor = new Color(0.04f, 0.08f, 0.14f, 0.9f);
            textColor = new Color(0.7f, 0.82f, 0.95f, 1f);
            accentBraceColor = HolographicCyan;
        }

        outerImage.color = outerBorderColor;

        var innerObj = new GameObject("InnerChassis", typeof(RectTransform), typeof(CanvasRenderer));
        innerObj.transform.SetParent(buttonObj.transform, false);
        var innerRt = innerObj.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f); innerRt.offsetMax = new Vector2(-2f, -2f);
        
        var innerImage = innerObj.AddComponent<Image>();
        innerImage.color = innerBodyColor;

        var leftBrace = new GameObject("LeftBrace", typeof(RectTransform), typeof(CanvasRenderer));
        leftBrace.transform.SetParent(innerObj.transform, false);
        var lRt = leftBrace.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0, 0); lRt.anchorMax = new Vector2(0, 1);
        lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
        lRt.sizeDelta = new Vector2(4, 0); lRt.anchoredPosition = new Vector2(2f, 0);
        var lImg = leftBrace.AddComponent<Image>();
        lImg.color = accentBraceColor;

        var rightBrace = new GameObject("RightBrace", typeof(RectTransform), typeof(CanvasRenderer));
        rightBrace.transform.SetParent(innerObj.transform, false);
        var rRt = rightBrace.GetComponent<RectTransform>();
        rRt.anchorMin = new Vector2(1, 0); rRt.anchorMax = new Vector2(1, 1);
        rRt.offsetMin = Vector2.zero; rRt.offsetMax = Vector2.zero;
        rRt.sizeDelta = new Vector2(4, 0); rRt.anchoredPosition = new Vector2(-2f, 0);
        var rImg = rightBrace.AddComponent<Image>();
        rImg.color = accentBraceColor;

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        var tmp = textObj.AddComponent<UnityEngine.UI.Text>();
        tmp.raycastTarget = false;
        tmp.text = label.ToUpper();
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.fontStyle = FontStyle.Bold;
        tmp.alignment = TextAnchor.MiddleCenter;
        tmp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var trt = textObj.GetComponent<RectTransform>();
        trt.SetParent(innerObj.transform, false);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        if (onClick != null) button.onClick.AddListener(onClick);

        var tactile = buttonObj.AddComponent<TactileButton>();
        tactile.neonStrip = lImg;
        tactile.rightBrace = rImg;
        tactile.neonColor = accentBraceColor;
    }

    private void EnsureCoreManagersExist()
    {
        if (FindObjectOfType<BackendClient>() != null) return;

        var gameManager = new GameObject("[AUTO] GameManager");
        DontDestroyOnLoad(gameManager);
        
        gameManager.AddComponent<GameManager>();
        gameManager.AddComponent<SaveManager>();
        gameManager.AddComponent<AudioManager>();
        gameManager.AddComponent<InputManager>();
        gameManager.AddComponent<CameraManager>();
        gameManager.AddComponent<SceneLoader>();
        gameManager.AddComponent<PoolManager>();
        gameManager.AddComponent<GameStateManager>();
        gameManager.AddComponent<SettingsManager>();
        gameManager.AddComponent<MatchManager>();
        gameManager.AddComponent<LootManager>();
        gameManager.AddComponent<ProgressionManager>();
        gameManager.AddComponent<LocalizationManager>();
        gameManager.AddComponent<AnalyticsManager>();
        gameManager.AddComponent<SafeZone>();
        gameManager.AddComponent<BackendClient>();
        gameManager.AddComponent<NetworkManagerSetup>();
    }
}
