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
/// AUTO-GENERATES all GameObjects when a scene loads.
/// Attach to Bootstrapper. No manual setup needed!
/// </summary>
public class SceneAutoBuilder : MonoBehaviour
{
    private static bool _initialized;
    private Dictionary<string, Sprite> _spriteCache = new();

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
        Debug.Log("[SceneAutoBuilder] Active — scenes will auto-generate on load");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneAutoBuilder] Building scene: {scene.name}");

        // Clear any existing auto-generated objects to avoid duplicates
        CleanupPreviousBuild(scene);

        // Build based on scene name
        switch (scene.name)
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
                // Any other scene gets essentials
                BuildEssentialSystems(scene);
                break;
        }

        Debug.Log($"[SceneAutoBuilder] ✓ {scene.name} built successfully");
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
        
        // Spawn core managers
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
        
        // Event system
        var evt = new GameObject("[AUTO] EventSystem");
        evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // Show splash/loading
        CreateSplashScreen(scene);
    }

    private void CreateSplashScreen(Scene scene)
    {
        var canvas = CreateCanvas(scene, "SplashCanvas", 0);
        
        // Try to load the actual splash screen image from your AI art
        var splashTexture = Resources.Load<Texture2D>("UI/Sprites/splash_screen");
        Color bgColor = splashTexture != null ? Color.white : new Color(0.039f, 0.086f, 0.157f, 1);
        
        var bg = CreateImage(canvas.transform, "SplashBG", bgColor, 
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        if (splashTexture != null)
        {
            var img = bg.GetComponent<Image>();
            img.sprite = Sprite.Create(splashTexture, new Rect(0, 0, splashTexture.width, splashTexture.height), new Vector2(0.5f, 0.5f));
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            Debug.Log("[SceneAutoBuilder] Loaded splash screen art!");
        }
        
        // Try to load the actual logo from your AI art
        var logoTexture = Resources.Load<Texture2D>("UI/Sprites/arena_fall_logo");
        var logo = CreateImage(bg.transform, "Logo", Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var rt = logo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 180);
        rt.anchoredPosition = new Vector2(0, 0);
        
        if (logoTexture != null)
        {
            var logoImg = logo.GetComponent<Image>();
            logoImg.sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
            logoImg.type = Image.Type.Simple;
            logoImg.preserveAspect = true;
            Debug.Log("[SceneAutoBuilder] Loaded logo art!");
        }
        
        // Loading text
        var loadingText = CreateText(bg.transform, "LoadingText", "INITIALIZING...", 24, Color.gray,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        var lrt = loadingText.GetComponent<RectTransform>();
        lrt.anchoredPosition = new Vector2(0, 40);
        
        // Auto-transition to MainMenu after brief delay
        var loader = bg.AddComponent<AutoSceneLoader>();
        loader.sceneName = "MainMenu";
        loader.delay = 2f;
    }

    // ─── MAIN MENU ─────────────────────────────────────────────
    private void BuildMainMenuScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        
        var canvas = CreateCanvas(scene, "MainMenuCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Try to load your AI-generated menu background
        var bgTexture = Resources.Load<Texture2D>("Art/Environment/skybox_concept");
        var logoTexture = Resources.Load<Texture2D>("UI/Sprites/arena_fall_logo");
        var iconAtlas = Resources.Load<Texture2D>("UI/Icons/ui_icons_atlas");
        
        // Dark background with AI art overlay
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        // Background image overlay — your AI art if available
        var bgOverlay = CreateImage(bg.transform, "BGEffects", new Color(1, 1, 1, 0.4f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        if (bgTexture != null)
        {
            var bgImg = bgOverlay.GetComponent<Image>();
            bgImg.sprite = Sprite.Create(bgTexture, new Rect(0, 0, bgTexture.width, bgTexture.height), new Vector2(0.5f, 0.5f));
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
            Debug.Log("[SceneAutoBuilder] MainMenu: Loaded background art!");
        }
        // Dark overlay for readability
        var darkOverlay = CreateImage(bg.transform, "DarkOverlay", new Color(0, 0, 0, 0.6f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        // Logo — your AI generated logo
        var logo = CreateImage(bg.transform, "Logo", Color.white,
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var lrt = logo.GetComponent<RectTransform>();
        lrt.sizeDelta = new Vector2(500, 200);
        if (logoTexture != null)
        {
            var logoImg = logo.GetComponent<Image>();
            logoImg.sprite = Sprite.Create(logoTexture, new Rect(0, 0, logoTexture.width, logoTexture.height), new Vector2(0.5f, 0.5f));
            logoImg.type = Image.Type.Simple;
            logoImg.preserveAspect = true;
            Debug.Log("[SceneAutoBuilder] MainMenu: Loaded logo art!");
        }
        
        // Player info bar at top
        var infoBar = CreatePanel(bg.transform, "PlayerInfoBar", new Color(0.02f, 0.04f, 0.08f, 0.8f),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1));
        var irt = infoBar.GetComponent<RectTransform>();
        irt.sizeDelta = new Vector2(0, 50);
        irt.anchoredPosition = new Vector2(0, 0);
        
        var playerName = CreateText(infoBar.transform, "PlayerName", "VANGUARD-01", 18, Color.white,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var nrt = playerName.GetComponent<RectTransform>();
        nrt.anchoredPosition = new Vector2(20, 0);
        
        var levelText = CreateText(infoBar.transform, "LevelText", "LVL 1", 14, new Color(0, 0.83f, 1, 1),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var lvrt = levelText.GetComponent<RectTransform>();
        lvrt.anchoredPosition = new Vector2(200, 0);
        
        var creditsText = CreateText(infoBar.transform, "CreditsText", "1,000", 14, new Color(1, 0.42f, 0.21f, 1),
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
        var crt = creditsText.GetComponent<RectTransform>();
        crt.anchoredPosition = new Vector2(-20, 0);
        
        // === MODERN BATTLE ROYALE MENU LAYOUT ===
        // Left side: Main navigation buttons (vertical stack)
        var leftPanel = CreatePanel(bg.transform, "NavPanel", new Color(0, 0, 0, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var lpanelR = leftPanel.GetComponent<RectTransform>();
        lpanelR.anchoredPosition = new Vector2(-200, -30);
        lpanelR.sizeDelta = new Vector2(280, 360);
        
        // Play button — LARGE, primary
        CreateMenuButton(lpanelR.transform, "PlayButton", "PLAY", 28, ButtonStyle.Primary, 
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -10), new Vector2(260, 60), () => {
                SceneManager.LoadScene("Lobby");
            });
        
        // Mode buttons
        CreateMenuButton(lpanelR.transform, "SoloButton", "SOLO", 20, ButtonStyle.Secondary,
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -80), new Vector2(260, 50), () => {
                SceneManager.LoadScene("GameMap");
            });
        
        CreateMenuButton(lpanelR.transform, "DuosButton", "DUOS", 20, ButtonStyle.Secondary,
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -140), new Vector2(260, 50), () => {
                if (BackendClient.Instance != null) BackendClient.Instance.JoinMatchmakingQueue("Battle Royale (Duos)", null);
                SceneManager.LoadScene("Matchmaking");
            });
        
        CreateMenuButton(lpanelR.transform, "SquadsButton", "SQUADS", 20, ButtonStyle.Secondary,
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -200), new Vector2(260, 50), () => {
                if (BackendClient.Instance != null) BackendClient.Instance.JoinMatchmakingQueue("Battle Royale (Squads)", null);
                SceneManager.LoadScene("Matchmaking");
            });
        
        // Right side: Secondary actions
        var rightPanel = CreatePanel(bg.transform, "ActionPanel", new Color(0, 0, 0, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var rpanelR = rightPanel.GetComponent<RectTransform>();
        rpanelR.anchoredPosition = new Vector2(200, -30);
        rpanelR.sizeDelta = new Vector2(280, 360);
        
        CreateMenuButton(rpanelR.transform, "TrainingButton", "TRAINING", 18, ButtonStyle.Tertiary,
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -10), new Vector2(260, 50), () => {
                SceneManager.LoadScene("TrainingGround");
            });
        
        CreateMenuButton(rpanelR.transform, "CustomizeButton", "CUSTOMIZE", 18, ButtonStyle.Tertiary,
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -70), new Vector2(260, 50), () => {
                SceneManager.LoadScene("Customization");
            });
        
        CreateMenuButton(rpanelR.transform, "LoadoutButton", "LOADOUT", 18, ButtonStyle.Tertiary,
            new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -130), new Vector2(260, 50), () => {
                SceneManager.LoadScene("Loadout");
            });
        
        // Bottom bar
        var bottomBar = CreatePanel(bg.transform, "BottomBar", new Color(0.02f, 0.04f, 0.08f, 0.9f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 0));
        var brt = bottomBar.GetComponent<RectTransform>();
        brt.sizeDelta = new Vector2(0, 60);
        
        CreateMenuButton(bottomBar.transform, "ShopButton", "SHOP", 16, ButtonStyle.Tertiary,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-100, 0), new Vector2(140, 40), () => {
                SceneManager.LoadScene("Customization");
            });
        
        CreateMenuButton(bottomBar.transform, "BattlePassButton", "BATTLE PASS", 16, ButtonStyle.Accent,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(100, 0), new Vector2(180, 40), () => {
                SceneManager.LoadScene("Profile");
            });
        
        CreateMenuButton(bottomBar.transform, "SettingsButton", "⚙", 20, ButtonStyle.Tertiary,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-20, 0), new Vector2(50, 40), () => {
                SceneManager.LoadScene("Settings");
            });
        
        CreateMenuButton(bottomBar.transform, "QuitButton", "✕", 18, ButtonStyle.Danger,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-80, 0), new Vector2(40, 40), () => {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            });
    }

    // ─── GAME MAP (Battle Royale) ──────────────────────────────
    private void BuildGameMapScene(Scene scene)
    {
        // 1. Initialize NetworkManager & UnityTransport for Online Multiplayer / Dedicated Headless Server
        if (NetworkManagerSetup.Instance != null)
        {
            NetworkManagerSetup.Instance.InitializeNetcode();
        }

        // 2. Create Terrain
        var terrainObj = new GameObject("[AUTO] Terrain");
        var terrain = terrainObj.AddComponent<Terrain>();
        var terrainData = new TerrainData();
        terrainData.size = new Vector3(4000, 200, 4000);
        terrainData.heightmapResolution = 512;
        terrain.terrainData = terrainData;
        
        var terrainCollider = terrainObj.AddComponent<TerrainCollider>();
        terrainCollider.terrainData = terrainData;

        // 3. Construct 6 Compound POIs & Infrastructure using AI Architectural Art & 3D Geometry
        MapPOIBuilder.BuildFullBattleRoyaleMap(terrainObj);
        
        // 4. Directional Light
        var lightObj = new GameObject("[AUTO] Directional Light");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.9f, 0.85f, 0.75f);
        light.intensity = 1.2f;
        lightObj.transform.rotation = Quaternion.Euler(40, 120, 0);
        light.shadows = LightShadows.Soft;
        
        // 5. Player Spawn at Nexus Tower Central Hub
        var playerObj = new GameObject("[AUTO] Player");
        playerObj.transform.position = new Vector3(2000, 12, 2000);
        
        var cc = playerObj.AddComponent<CharacterController>();
        cc.height = 2;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 1, 0);
        
        var controller = playerObj.AddComponent<PlayerCharacterController>();
        var health = playerObj.AddComponent<CharacterHealth>();
        var inventory = playerObj.AddComponent<Inventory>();

        // Build 3D Character Rig for Player with glowing cyan visor & armor vest
        SciFiCharacterAndVehicleBuilder.Build3DSciFiCharacterRig(playerObj.transform, "Player3DRig", true, new Color(0f, 0.83f, 1f));

        // Equip starting weapon (`pc90_plasma_cannon` or `a17_striker`)
        var weaponObj = new GameObject("EquippedWeapon");
        weaponObj.transform.SetParent(playerObj.transform);
        var wc = weaponObj.AddComponent<WeaponController>();
        var wData = ScriptableObject.CreateInstance<WeaponData>();
        wData.weaponId = "pc90_plasma_cannon";
        wData.weaponName = "PC-90 Plasma Cannon";
        wData.baseDamage = 38f;
        wData.magazineSize = 30;
        wData.maxReserveAmmo = 180;
        wc.Equip(wData);
        
        // 6. Camera
        var camObj = new GameObject("[AUTO] MainCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 70;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 5000;
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();
        
        var camManager = FindObjectOfType<CameraManager>();
        if (camManager != null)
        {
            camManager.SetTarget(playerObj.transform);
        }
        
        // 7. Safe Zone
        var zoneObj = new GameObject("[AUTO] SafeZone");
        zoneObj.AddComponent<SafeZone>();
        
        var audioSource = playerObj.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1;
        
        // 8. HUD
        BuildHUD(scene);
        
        // 9. Spawn 3D Rigged AI Bots & Drivable Vehicles
        SpawnAIBots();
        SpawnLoot();
        
        // 10. Event System
        var evt = new GameObject("[AUTO] EventSystem");
        evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // ─── HUD ──────────────────────────────────────────────────
    private void BuildHUD(Scene scene)
    {
        var canvas = CreateCanvas(scene, "HUDCanvas", 10);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        // === TOP BAR ===
        var topBar = CreatePanel(canvas.transform, "TopBar", new Color(0, 0, 0, 0.5f),
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 1));
        var tbrt = topBar.GetComponent<RectTransform>();
        tbrt.sizeDelta = new Vector2(0, 50);
        
        // Player count
        var pCount = CreateText(topBar.transform, "PlayerCount", "ALIVE: 60", 14, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        
        // Minimap (top right) — using your AI HUD art
        var minimap = CreatePanel(canvas.transform, "Minimap", new Color(0, 0, 0, 0.6f),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
        var mrt = minimap.GetComponent<RectTransform>();
        mrt.sizeDelta = new Vector2(180, 180);
        mrt.anchoredPosition = new Vector2(-15, -15);
        
        // Try loading the modern BR HUD background for minimap
        var hudTex = Resources.Load<Texture2D>("UI/Sprites/modern_br_hud");
        var hudElementsTex = Resources.Load<Texture2D>("UI/Sprites/hud_elements");
        
        // Minimap border — from your AI HUD elements
        var mmBorder = CreateImage(minimap.transform, "MinimapBorder", new Color(0, 0.83f, 1, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var brt2 = mmBorder.GetComponent<RectTransform>();
        brt2.sizeDelta = new Vector2(178, 178);
        if (hudElementsTex != null)
        {
            var borderImg = mmBorder.GetComponent<Image>();
            borderImg.sprite = Sprite.Create(hudElementsTex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            borderImg.type = Image.Type.Sliced;
            Debug.Log("[SceneAutoBuilder] HUD: Loaded minimap border art!");
        }
        else
        {
            mmBorder.GetComponent<Image>().fillCenter = false;
            mmBorder.GetComponent<Image>().type = Image.Type.Sliced;
        }
        
        // Compass bar (top center area)
        var compass = CreatePanel(canvas.transform, "CompassBar", new Color(0, 0, 0, 0.4f),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        var crt2 = compass.GetComponent<RectTransform>();
        crt2.sizeDelta = new Vector2(400, 20);
        crt2.anchoredPosition = new Vector2(0, -60);
        
        // Compass text labels
        string[] dirs = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        for (int i = 0; i < 8; i++)
        {
            float xPos = -175 + (i * 50);
            var dirText = CreateText(compass.transform, dirs[i], dirs[i], 10, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var drt = dirText.GetComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(xPos, 0);
        }
        
        // === BOTTOM CENTER ===
        // Health panel
        var healthPanel = CreatePanel(canvas.transform, "HealthPanel", new Color(0, 0, 0, 0),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        var hprt = healthPanel.GetComponent<RectTransform>();
        hprt.sizeDelta = new Vector2(350, 100);
        hprt.anchoredPosition = new Vector2(0, 30);
        
        // Health bar
        var healthBarBg = CreateImage(healthPanel.transform, "HealthBarBG", new Color(0.2f, 0.2f, 0.2f, 0.6f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var hbrt = healthBarBg.GetComponent<RectTransform>();
        hbrt.sizeDelta = new Vector2(200, 14);
        hbrt.anchoredPosition = new Vector2(0, 15);
        
        var healthBar = CreateImage(healthBarBg.transform, "HealthBarFill", new Color(0.27f, 0.85f, 0.27f, 1),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var hfrt = healthBar.GetComponent<RectTransform>();
        hfrt.sizeDelta = new Vector2(200, 14);
        hfrt.GetComponent<Image>().type = Image.Type.Filled;
        hfrt.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        
        var hpText = CreateText(healthBarBg.transform, "HPText", "100", 12, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        
        // Shield bar
        var shieldBarBg = CreateImage(healthPanel.transform, "ShieldBarBG", new Color(0.2f, 0.2f, 0.2f, 0.6f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var sbrt = shieldBarBg.GetComponent<RectTransform>();
        sbrt.sizeDelta = new Vector2(200, 10);
        sbrt.anchoredPosition = new Vector2(0, 0);
        
        var shieldBar = CreateImage(shieldBarBg.transform, "ShieldBarFill", new Color(0, 0.83f, 1, 1),
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var sfrt = shieldBar.GetComponent<RectTransform>();
        sfrt.sizeDelta = new Vector2(200, 10);
        sfrt.GetComponent<Image>().type = Image.Type.Filled;
        sfrt.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        
        // Weapon display — shows your AI weapon art!
        var weaponPanel = CreatePanel(healthPanel.transform, "WeaponDisplay", new Color(0, 0, 0, 0.5f),
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        var wprt = weaponPanel.GetComponent<RectTransform>();
        wprt.sizeDelta = new Vector2(320, 70);
        wprt.anchoredPosition = new Vector2(0, -50);
        
        // Weapon icon — using your AI assault rifle art
        var weaponIcon = CreateImage(weaponPanel.transform, "WeaponIcon", Color.white,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var wirt = weaponIcon.GetComponent<RectTransform>();
        wirt.sizeDelta = new Vector2(80, 50);
        wirt.anchoredPosition = new Vector2(10, 0);
        var weaponArtTex = Resources.Load<Texture2D>("Art/Weapons/AssaultRifles/a17_striker");
        if (weaponArtTex != null)
        {
            var wImg = weaponIcon.GetComponent<Image>();
            wImg.sprite = Sprite.Create(weaponArtTex, new Rect(0, 0, weaponArtTex.width, weaponArtTex.height), new Vector2(0.5f, 0.5f));
            wImg.type = Image.Type.Simple;
            wImg.preserveAspect = true;
        }
        
        var weaponName = CreateText(weaponPanel.transform, "WeaponName", "PC-90 PLASMA CANNON", 16, Color.white,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var wnrt = weaponName.GetComponent<RectTransform>();
        wnrt.anchoredPosition = new Vector2(100, 10);
        
        var ammoText = CreateText(weaponPanel.transform, "AmmoText", "30 / 120", 22, Color.white,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
        var art2 = ammoText.GetComponent<RectTransform>();
        art2.anchoredPosition = new Vector2(100, -15);
        
        // === RIGHT SIDE ===
        // Kill feed
        var killFeed = CreatePanel(canvas.transform, "KillFeed", new Color(0, 0, 0, 0),
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
        var kfrt = killFeed.GetComponent<RectTransform>();
        kfrt.sizeDelta = new Vector2(300, 200);
        kfrt.anchoredPosition = new Vector2(-210, -80);
        
        // Crosshair (center) — using your AI crosshair asset
        var crosshairPanel = CreatePanel(canvas.transform, "Crosshair", new Color(0, 0, 0, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var crt3 = crosshairPanel.GetComponent<RectTransform>();
        crt3.sizeDelta = new Vector2(48, 48);
        
        // Try loading your AI crosshair sprite
        var crosshairTex = Resources.Load<Texture2D>("UI/Sprites/crosshairs");
        if (crosshairTex != null)
        {
            var crossImg = crosshairPanel.AddComponent<Image>();
            crossImg.sprite = Sprite.Create(crosshairTex, new Rect(0, 0, 48, 48), new Vector2(0.5f, 0.5f));
            crossImg.type = Image.Type.Simple;
            crossImg.color = new Color(0, 0.83f, 1, 0.9f);
            Debug.Log("[SceneAutoBuilder] HUD: Loaded crosshair art!");
        }
        else
        {
            // Fallback: procedural crosshair
            // Center dot
            var dot = CreateImage(crosshairPanel.transform, "CenterDot", new Color(0, 0.83f, 1, 0.8f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var drt2 = dot.GetComponent<RectTransform>();
            drt2.sizeDelta = new Vector2(3, 3);
            
            // Cross lines
            for (int i = 0; i < 4; i++)
            {
                var line = CreateImage(crosshairPanel.transform, $"Line{i}", new Color(0, 0.83f, 1, 0.7f),
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                var lrt = line.GetComponent<RectTransform>();
                float x = i < 2 ? 5 : 0;
                float y = i >= 2 ? 5 : 0;
                float w = i < 2 ? 10 : 2;
                float h = i < 2 ? 2 : 10;
                if (i == 1) x = -8;
                if (i == 3) y = -8;
                lrt.anchoredPosition = new Vector2(x, y);
                lrt.sizeDelta = new Vector2(w, h);
            }
        }
        
        // Interaction prompt (center bottom)
        var interact = CreatePanel(canvas.transform, "InteractPrompt", new Color(0, 0, 0, 0.6f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var irt2 = interact.GetComponent<RectTransform>();
        irt2.sizeDelta = new Vector2(300, 40);
        irt2.anchoredPosition = new Vector2(0, -80);
        interact.SetActive(false);
        
        var interactText = CreateText(interact.transform, "InteractText", "Press [F] to pick up", 14, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        
        // Add HUD Controller component to canvas
        var hudController = canvas.AddComponent<HUDController>();
    }

    // ─── LOBBY ─────────────────────────────────────────────────
    private void BuildLobbyScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        
        var canvas = CreateCanvas(scene, "LobbyCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Try loading terrain or nexus tower as lobby background
        var lobbyBgTex = Resources.Load<Texture2D>("Art/Environment/terrain_concept");
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        if (lobbyBgTex != null)
        {
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = Sprite.Create(lobbyBgTex, new Rect(0, 0, lobbyBgTex.width, lobbyBgTex.height), new Vector2(0.5f, 0.5f));
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
        }
        var lobbyOverlay = CreateImage(bg.transform, "Overlay", new Color(0, 0, 0, 0.5f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        // Lobby title
        var title = CreateText(bg.transform, "Title", "MATCH LOBBY", 32, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        var trt = title.GetComponent<RectTransform>();
        trt.anchoredPosition = new Vector2(0, -30);
        
        // Player list panel
        var playerList = CreatePanel(bg.transform, "PlayerList", new Color(0, 0, 0, 0.3f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var plrt = playerList.GetComponent<RectTransform>();
        plrt.sizeDelta = new Vector2(400, 300);
        plrt.anchoredPosition = new Vector2(0, 30);
        
        // Player entries
        for (int i = 1; i <= 4; i++)
        {
            var entry = CreatePanel(playerList.transform, $"Player{i}", new Color(0.1f, 0.15f, 0.25f, 0.5f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            var entryRt = entry.GetComponent<RectTransform>();
            entryRt.sizeDelta = new Vector2(360, 50);
            entryRt.anchoredPosition = new Vector2(0, -10 - ((i - 1) * 55));
            
            CreateText(entry.transform, "Name", i == 1 ? "YOU" : $"PLAYER {i}", 16, 
                i == 1 ? new Color(0, 0.83f, 1, 1) : Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
            
            if (i == 1)
            {
                var ready = CreateText(entry.transform, "Status", "READY", 12, new Color(0.27f, 0.85f, 0.27f, 1),
                    new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                var rrt = ready.GetComponent<RectTransform>();
                rrt.anchoredPosition = new Vector2(-15, 0);
            }
        }
        
        // Start button with Matchmaking & Netcode initialization
        CreateMenuButton(bg.transform, "StartButton", "START MATCH", 22, ButtonStyle.Primary,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 40), new Vector2(280, 55), () => {
                if (BackendClient.Instance != null)
                {
                    BackendClient.Instance.JoinMatchmakingQueue("Battle Royale (Solo)", (success, status) => {
                        Debug.Log($"[Matchmaking] Status: {status?.status ?? "Queued"}");
                    });
                }
                if (NetworkManagerSetup.Instance != null)
                {
                    NetworkManagerSetup.Instance.StartHost();
                }
                SceneManager.LoadScene("GameMap");
            });
        
        // Back
        CreateMenuButton(bg.transform, "BackButton", "← BACK", 16, ButtonStyle.Tertiary,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(15, -15), new Vector2(100, 35), () => {
                SceneManager.LoadScene("MainMenu");
            });
    }

    // ─── TRAINING ──────────────────────────────────────────────
    private void BuildTrainingScene(Scene scene)
    {
        // Ground plane
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "[AUTO] Ground";
        ground.transform.localScale = new Vector3(60, 1, 60);
        ground.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ground.GetComponent<MeshRenderer>().material.color = new Color(0.2f, 0.25f, 0.32f);
        
        // Player
        var playerObj = new GameObject("[AUTO] Player");
        playerObj.transform.position = new Vector3(0, 1, 0);
        var cc = playerObj.AddComponent<CharacterController>();
        cc.height = 2;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 1, 0);
        playerObj.AddComponent<PlayerCharacterController>();
        playerObj.AddComponent<CharacterHealth>();
        playerObj.AddComponent<Inventory>();
        
        // Build 3D Character Rig for Player in Training
        SciFiCharacterAndVehicleBuilder.Build3DSciFiCharacterRig(playerObj.transform, "PlayerTraining3DRig", true, new Color(0f, 0.83f, 1f));
        
        // Target Dummies with 3D Rigs
        for (int i = 0; i < 5; i++)
        {
            var dummy = new GameObject($"[AUTO] TargetDummy_{i}");
            dummy.transform.position = new Vector3(10 + (i * 5), 1, 8);
            dummy.AddComponent<CapsuleCollider>().height = 2f;
            
            var targetHealth = dummy.AddComponent<CharacterHealth>();
            Color dummyColor = i % 2 == 0 ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.9f, 0.5f, 0.1f);
            SciFiCharacterAndVehicleBuilder.Build3DSciFiCharacterRig(dummy.transform, $"DummyRig_{i}", false, dummyColor);
            
            dummy.AddComponent<RotateAnimation>();
        }
        
        // Camera
        var camObj = new GameObject("[AUTO] MainCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 70;
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();
        
        var camManager = FindObjectOfType<CameraManager>();
        if (camManager != null) camManager.SetTarget(playerObj.transform);
        
        // HUD
        BuildHUD(scene);
        
        // Event system
        var evt = new GameObject("[AUTO] EventSystem");
        evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        
        // Back button
        var canvas = CreateCanvas(scene, "UICanvas", 1);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        CreateMenuButton(canvas.transform, "BackButton", "← MAIN MENU", 16, ButtonStyle.Tertiary,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(15, -15), new Vector2(140, 35), () => {
                SceneManager.LoadScene("MainMenu");
            });
    }

    // ─── RESULT SCREEN ──────────────────────────────────────────
    private void BuildResultScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        
        var canvas = CreateCanvas(scene, "ResultCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Result screen background with AI art
        var resultBgTex = Resources.Load<Texture2D>("Art/Environment/skybox_concept");
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        if (resultBgTex != null)
        {
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = Sprite.Create(resultBgTex, new Rect(0, 0, resultBgTex.width, resultBgTex.height), new Vector2(0.5f, 0.5f));
            bgImg.type = Image.Type.Simple;
            bgImg.preserveAspect = false;
        }
        var resultOverlay = CreateImage(bg.transform, "Overlay", new Color(0, 0, 0, 0.7f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        // Result header
        var header = CreateText(bg.transform, "Header", "MATCH RESULTS", 28, new Color(1, 0.42f, 0.21f, 1),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchoredPosition = new Vector2(0, -40);
        
        int kills = MatchManager.Instance != null ? 7 : 5;
        int damage = MatchManager.Instance != null ? 1420 : 1240;
        int place = MatchManager.Instance != null ? 1 : 1;
        float survival = MatchManager.Instance != null ? MatchManager.Instance.MatchElapsedTime : 750f;

        if (BackendClient.Instance != null)
        {
            BackendClient.Instance.SyncStatsAfterMatch(kills, damage, place, survival);
        }

        // Placement
        var placement = CreateText(bg.transform, "Placement", $"#{place}", 64, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var prt = placement.GetComponent<RectTransform>();
        prt.anchoredPosition = new Vector2(0, 60);
        
        var placementLabel = CreateText(bg.transform, "PlacementLabel", "PLACEMENT", 14, Color.gray,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var plrt2 = placementLabel.GetComponent<RectTransform>();
        plrt2.anchoredPosition = new Vector2(0, 20);
        
        // Stats grid
        string[] stats = { "KILLS", "DAMAGE", "SURVIVED", "SCORE" };
        string[] values = { kills.ToString(), damage.ToString("N0"), $"{(int)(survival/60)}:{(int)(survival%60):D2}", ((kills * 100) + (place == 1 ? 500 : 200)).ToString("N0") };
        for (int i = 0; i < 4; i++)
        {
            var statPanel = CreatePanel(bg.transform, $"Stat_{i}", new Color(0.1f, 0.15f, 0.25f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var srt = statPanel.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(120, 80);
            srt.anchoredPosition = new Vector2(-190 + (i * 125), -30);
            
            CreateText(statPanel.transform, "Value", values[i], 28, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            
            CreateText(statPanel.transform, "Label", stats[i], 10, Color.gray,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
        }
        
        // XP earned
        var xpText = CreateText(bg.transform, "XPEarned", $"+ {(kills * 150) + 350} XP", 20, new Color(0.27f, 0.85f, 0.27f, 1),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var xrt = xpText.GetComponent<RectTransform>();
        xrt.anchoredPosition = new Vector2(0, -130);
        
        // Buttons
        CreateMenuButton(bg.transform, "LobbyButton", "PLAY AGAIN", 20, ButtonStyle.Primary,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(-80, 40), new Vector2(200, 50), () => {
                SceneManager.LoadScene("GameMap");
            });
        
        CreateMenuButton(bg.transform, "MenuButton", "MAIN MENU", 16, ButtonStyle.Secondary,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(110, 40), new Vector2(200, 50), () => {
                SceneManager.LoadScene("MainMenu");
            });
    }

    // ─── SETTINGS ────────────────────────────────────────────────
    private void BuildSettingsScene(Scene scene)
    {
        BuildEssentialSystems(scene);
        
        var canvas = CreateCanvas(scene, "SettingsCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        
        var settingsBgTex = Resources.Load<Texture2D>("Art/Environment/terrain_concept");
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        if (settingsBgTex != null)
        {
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = Sprite.Create(settingsBgTex, new Rect(0, 0, settingsBgTex.width, settingsBgTex.height), new Vector2(0.5f, 0.5f));
            bgImg.type = Image.Type.Simple;
        }
        var settingsOverlay = CreateImage(bg.transform, "Overlay", new Color(0, 0, 0, 0.6f),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        
        var title = CreateText(bg.transform, "Title", "SETTINGS", 28, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
        var trt2 = title.GetComponent<RectTransform>();
        trt2.anchoredPosition = new Vector2(0, -30);
        
        // Audio section
        CreateSettingsSlider(bg.transform, "MasterVolume", "MASTER VOLUME", 0.8f, 0, new Vector2(0, -80));
        CreateSettingsSlider(bg.transform, "MusicVolume", "MUSIC VOLUME", 0.7f, 1, new Vector2(0, -130));
        CreateSettingsSlider(bg.transform, "SFXVolume", "SFX VOLUME", 0.8f, 2, new Vector2(0, -180));
        
        // Sensitivity
        CreateSettingsSlider(bg.transform, "Sensitivity", "LOOK SENSITIVITY", 0.5f, 3, new Vector2(0, -240));
        
        // Back
        CreateMenuButton(bg.transform, "BackButton", "← BACK", 18, ButtonStyle.Tertiary,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 40), new Vector2(200, 50), () => {
                SceneManager.LoadScene("MainMenu");
            });
    }

    // ─── TEST SCENE ──────────────────────────────────────────────
    private void BuildTestScene(Scene scene)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "[AUTO] Ground";
        ground.transform.localScale = new Vector3(20, 1, 20);
        ground.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        
        // Player spawn
        var playerObj = new GameObject("[AUTO] Player");
        playerObj.transform.position = new Vector3(0, 1, 0);
        var cc = playerObj.AddComponent<CharacterController>();
        cc.height = 2;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 1, 0);
        playerObj.AddComponent<PlayerCharacterController>();
        
        // Test cubes
        for (int i = 0; i < 5; i++)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"[AUTO] TestCube_{i}";
            cube.transform.position = new Vector3(-3 + (i * 1.5f), 0.5f, 3);
            cube.GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.red, Color.blue, i / 4f);
        }
        
        // Camera
        var camObj = new GameObject("[AUTO] MainCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 70;
        camObj.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0, 3, -5);
        camObj.transform.LookAt(Vector3.zero);
        
        var camManager = FindObjectOfType<CameraManager>();
        if (camManager != null) camManager.SetTarget(playerObj.transform);
        
        // Event system
        var evt = new GameObject("[AUTO] EventSystem");
        evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
        evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // ─── AI BOTS ──────────────────────────────────────────────
    private void SpawnAIBots()
    {
        int botCount = 59; // Default 59 bots + 1 human = 60 player lobby
        if (BackendClient.Instance != null && BackendClient.Instance.LastMatchBotCount > 0)
        {
            botCount = BackendClient.Instance.LastMatchBotCount;
        }
        SciFiCharacterAndVehicleBuilder.Spawn3DAIBotsAcrossCompounds(botCount);
    }

    // ─── LOOT & VEHICLES ──────────────────────────────────────
    private void SpawnLoot()
    {
        SciFiCharacterAndVehicleBuilder.SpawnDrivableVehiclesAcrossMap();
    }

    // ─── LOGIN & REGISTRATION ──────────────────────────────────
    private void BuildLoginScene(Scene scene)
    {
        BuildEssentialSystems(scene);

        if (BackendClient.Instance != null && BackendClient.Instance.IsAuthenticated)
        {
            Debug.Log("[SceneAutoBuilder] Login: Player already authenticated, navigating to MainMenu.");
            SceneManager.LoadScene("MainMenu");
            return;
        }

        var canvas = CreateCanvas(scene, "LoginCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var bgTex = Resources.Load<Texture2D>("Art/Environment/skybox_concept");
        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));
        if (bgTex != null)
        {
            var bgImg = bg.GetComponent<Image>();
            bgImg.sprite = Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            bgImg.type = Image.Type.Simple;
        }
        CreateImage(bg.transform, "DarkOverlay", new Color(0, 0, 0, 0.75f), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));

        var title = CreateText(bg.transform, "Title", "ARENA FALL LOGIN", 32, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f));

        var statusText = CreateText(bg.transform, "Status", "Connect with your Arena Fall account to play online", 14, Color.gray,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f));

        var panel = CreatePanel(bg.transform, "LoginBox", new Color(0.1f, 0.15f, 0.25f, 0.8f),
            new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.45f));
        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 240);

        CreateText(panel.transform, "EmailLabel", "EMAIL: user@arenafall.com", 14, Color.white,
            new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f));
        CreateText(panel.transform, "PassLabel", "PASSWORD: **********", 14, Color.white,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f));

        CreateMenuButton(panel.transform, "LoginButton", "LOGIN", 18, ButtonStyle.Primary,
            new Vector2(0.28f, 0.4f), new Vector2(0.28f, 0.4f), new Vector2(0.28f, 0.4f), new Vector2(0.28f, 0.4f),
            new Vector2(0, 0), new Vector2(140, 42), () => {
                if (BackendClient.Instance != null)
                {
                    statusText.GetComponent<TextMeshProUGUI>().text = "Authenticating with authoritative backend...";
                    BackendClient.Instance.Login("user@arenafall.com", "password123", (success, msg) => {
                        if (success) SceneManager.LoadScene("MainMenu");
                        else statusText.GetComponent<TextMeshProUGUI>().text = msg;
                    });
                }
                else SceneManager.LoadScene("MainMenu");
            });

        CreateMenuButton(panel.transform, "RegisterButton", "REGISTER", 18, ButtonStyle.Secondary,
            new Vector2(0.72f, 0.4f), new Vector2(0.72f, 0.4f), new Vector2(0.72f, 0.4f), new Vector2(0.72f, 0.4f),
            new Vector2(0, 0), new Vector2(140, 42), () => {
                if (BackendClient.Instance != null)
                {
                    BackendClient.Instance.Register("user@arenafall.com", "Vanguard_Soldier", "password123", (success, msg) => {
                        if (success) SceneManager.LoadScene("MainMenu");
                        else statusText.GetComponent<TextMeshProUGUI>().text = msg;
                    });
                }
                else SceneManager.LoadScene("MainMenu");
            });

        CreateMenuButton(panel.transform, "GuestButton", "⚡ PLAY AS GUEST (INSTANT MOBILE / IOS)", 15, ButtonStyle.Accent,
            new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f),
            new Vector2(0, 0), new Vector2(320, 42), () => {
                if (BackendClient.Instance != null)
                {
                    statusText.GetComponent<TextMeshProUGUI>().text = "Starting instant mobile guest session...";
                    BackendClient.Instance.LoginAsGuest((success, msg) => {
                        if (success) SceneManager.LoadScene("MainMenu");
                        else statusText.GetComponent<TextMeshProUGUI>().text = msg;
                    });
                }
                else SceneManager.LoadScene("MainMenu");
            });
    }

    // ─── PROFILE CAREER DASHBOARD ──────────────────────────────
    private void BuildProfileScene(Scene scene)
    {
        BuildEssentialSystems(scene);

        var canvas = CreateCanvas(scene, "ProfileCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));

        CreateText(bg.transform, "Title", "CAREER STATS & PROGRESSION", 28, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f));

        string playerName = BackendClient.Instance?.CachedProfile?.playerName ?? "VANGUARD-01";
        int level = BackendClient.Instance?.CachedProfile?.level ?? 15;
        int credits = BackendClient.Instance?.CachedProfile?.credits ?? 2450;

        CreateText(bg.transform, "PlayerInfo", $"SOLDIER: {playerName} | LEVEL: {level} | CREDITS: {credits}", 20, Color.white,
            new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f));

        string[] labels = { "WINS", "TOTAL KILLS", "DAMAGE DEALT", "K/D RATIO" };
        string[] values = { "12", "148", "38,420", "3.24" };
        for (int i = 0; i < 4; i++)
        {
            var card = CreatePanel(bg.transform, $"StatCard_{i}", new Color(0.1f, 0.15f, 0.25f, 0.6f),
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f));
            card.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 100);
            card.GetComponent<RectTransform>().anchoredPosition = new Vector2(-270 + (i * 180), 0);

            CreateText(card.transform, "Val", values[i], 32, new Color(1, 0.42f, 0.21f, 1), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f));
            CreateText(card.transform, "Lab", labels[i], 12, Color.gray, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f));
        }

        CreateMenuButton(bg.transform, "BackBtn", "← BACK TO MAIN MENU", 16, ButtonStyle.Tertiary,
            new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f), new Vector2(0.5f, 0.15f),
            new Vector2(0, 0), new Vector2(220, 45), () => SceneManager.LoadScene("MainMenu"));
    }

    // ─── 3D CUSTOMIZATION & SKIN ROOM ──────────────────────────
    private void BuildCustomizationScene(Scene scene)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ground.name = "Podium";
        ground.transform.position = new Vector3(0, 0, 3.5f);
        ground.transform.localScale = new Vector3(3, 0.2f, 3);
        ground.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.1f, 0.15f, 0.25f) };

        // 3D Character Rig Inspection
        var charObj = new GameObject("InspectionCharacter");
        charObj.transform.position = new Vector3(0, 0.1f, 3.5f);
        charObj.transform.rotation = Quaternion.Euler(0, 180, 0);
        SciFiCharacterAndVehicleBuilder.Build3DSciFiCharacterRig(charObj.transform, "InspectRig", true, new Color(0f, 0.83f, 1f));

        // Inspection Camera
        var camObj = new GameObject("InspectionCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 50;
        camObj.transform.position = new Vector3(0, 1.6f, 0);
        camObj.AddComponent<AudioListener>();

        // Light
        var lightObj = new GameObject("PodiumLight");
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(0f, 0.83f, 1f);
        light.intensity = 3f;
        lightObj.transform.position = new Vector3(0, 4, 2);
        lightObj.transform.LookAt(charObj.transform.position + new Vector3(0, 1.2f, 0));

        var canvas = CreateCanvas(scene, "CustomCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CreateText(canvas.transform, "Title", "3D ARMOR & COSMETIC INSPECTION", 26, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f));

        CreateMenuButton(canvas.transform, "ToggleArmor", "TOGGLE TACTICAL VEST", 16, ButtonStyle.Secondary,
            new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.5f),
            new Vector2(0, 40), new Vector2(200, 45), () => {
                var vest = charObj.transform.Find("InspectRig/Torso/ArmorVest");
                if (vest != null) vest.gameObject.SetActive(!vest.gameObject.activeSelf);
            });

        CreateMenuButton(canvas.transform, "ToggleBackpack", "CYCLE BACKPACK TIER", 16, ButtonStyle.Secondary,
            new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.5f), new Vector2(0.2f, 0.5f),
            new Vector2(0, -20), new Vector2(200, 45), () => {
                var bp = charObj.transform.Find("InspectRig/Torso/Backpack");
                if (bp != null) bp.localScale = bp.localScale.x > 0.8f ? new Vector3(0.6f, 0.7f, 0.4f) : new Vector3(0.9f, 1.0f, 0.65f);
            });

        CreateMenuButton(canvas.transform, "RotateRig", "ROTATE PODIUM 90°", 16, ButtonStyle.Tertiary,
            new Vector2(0.8f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0.8f, 0.5f),
            new Vector2(0, 10), new Vector2(200, 45), () => {
                charObj.transform.Rotate(Vector3.up, 90f);
            });

        CreateMenuButton(canvas.transform, "SaveExitBtn", "SAVE & EXIT TO MENU", 18, ButtonStyle.Primary,
            new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
            new Vector2(0, 0), new Vector2(220, 50), () => SceneManager.LoadScene("MainMenu"));
    }

    // ─── 3D LOADOUT & WEAPON ARMORY ROOM ───────────────────────
    private void BuildLoadoutScene(Scene scene)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "ArmoryFloor";
        ground.transform.localScale = new Vector3(2, 1, 2);
        ground.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.12f, 0.16f, 0.22f) };

        var armoryObj = new GameObject("ArmoryRacks");
        string[] weapons = { "sr25_longshot", "a17_striker", "sg20_devastator", "s9_viper" };
        for (int i = 0; i < weapons.Length; i++)
        {
            float x = -3f + (i * 2f);
            var rackObj = new GameObject($"Rack_{weapons[i]}");
            rackObj.transform.SetParent(armoryObj.transform);
            rackObj.transform.position = new Vector3(x, 0, 4f);
            var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stand.transform.SetParent(rackObj.transform);
            stand.transform.localPosition = new Vector3(0, 1f, 0);
            stand.transform.localScale = new Vector3(1.2f, 2f, 0.6f);
            stand.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.2f, 0.28f, 0.4f) };
            
            var wBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wBox.transform.SetParent(rackObj.transform);
            wBox.transform.localPosition = new Vector3(0, 2.2f, 0);
            wBox.transform.localScale = new Vector3(1f, 0.3f, 0.2f);
            wBox.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.6f, 0f) };
        }

        var camObj = new GameObject("ArmoryCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.fieldOfView = 60;
        camObj.transform.position = new Vector3(0, 2.5f, -1f);
        camObj.AddComponent<AudioListener>();

        var canvas = CreateCanvas(scene, "LoadoutCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CreateText(canvas.transform, "Title", "TACTICAL WEAPON LOADOUT ARMORY", 28, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.9f));

        CreateMenuButton(canvas.transform, "EquipSR", "EQUIP SR25 LONGSHOT (PRIMARY)", 16, ButtonStyle.Primary,
            new Vector2(0.3f, 0.2f), new Vector2(0.3f, 0.2f), new Vector2(0.3f, 0.2f), new Vector2(0.3f, 0.2f),
            new Vector2(0, 0), new Vector2(260, 45), () => { Debug.Log("Equipped SR25 Longshot"); });

        CreateMenuButton(canvas.transform, "BackMenu", "← RETURN TO MAIN MENU", 16, ButtonStyle.Tertiary,
            new Vector2(0.7f, 0.2f), new Vector2(0.7f, 0.2f), new Vector2(0.7f, 0.2f), new Vector2(0.7f, 0.2f),
            new Vector2(0, 0), new Vector2(220, 45), () => SceneManager.LoadScene("MainMenu"));
    }

    // ─── MATCHMAKING SEARCH SCREEN ─────────────────────────────
    private void BuildMatchmakingScene(Scene scene)
    {
        BuildEssentialSystems(scene);

        var canvas = CreateCanvas(scene, "MatchmakingCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var bg = CreateImage(canvas.transform, "Background", new Color(0.039f, 0.086f, 0.157f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));

        CreateText(bg.transform, "Title", "SEARCHING FOR BATTLE ROYALE MATCH...", 28, new Color(0, 0.83f, 1, 1),
            new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f));

        var statusLabel = CreateText(bg.transform, "QueueStatus", "Estimated Wait Time: 15s | Region: Africa/Lagos (Authoritative)", 16, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        CreateMenuButton(bg.transform, "ConnectNow", "⚡ FORCE CONNECT NOW (DEV/TEST)", 18, ButtonStyle.Accent,
            new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f),
            new Vector2(0, 0), new Vector2(280, 50), () => {
                if (NetworkManagerSetup.Instance != null) NetworkManagerSetup.Instance.StartClient("127.0.0.1", 7777);
                SceneManager.LoadScene("GameMap");
            });

        CreateMenuButton(bg.transform, "CancelSearch", "CANCEL SEARCH", 16, ButtonStyle.Danger,
            new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f),
            new Vector2(0, 0), new Vector2(180, 45), () => SceneManager.LoadScene("MainMenu"));
    }

    // ─── REPLAY THEATER ────────────────────────────────────────
    private void BuildReplayScene(Scene scene)
    {
        BuildEssentialSystems(scene);

        var canvas = CreateCanvas(scene, "ReplayCanvas", 0);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var bg = CreateImage(canvas.transform, "Background", new Color(0.02f, 0.04f, 0.08f, 1),
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(1, 1), new Vector2(1, 1));

        CreateText(bg.transform, "Title", "🎥 MATCH REPLAY THEATER & SPECTATOR CAM", 28, new Color(1, 0.42f, 0.21f, 1),
            new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f));

        CreateText(bg.transform, "Desc", "Viewing recorded telemetry trajectory across Nexus Tower and Industrial Factory.", 16, Color.gray,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        CreateMenuButton(bg.transform, "BackBtn", "← RETURN TO MAIN MENU", 16, ButtonStyle.Tertiary,
            new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f),
            new Vector2(0, 0), new Vector2(220, 45), () => SceneManager.LoadScene("MainMenu"));
    }

    // ─── ESSENTIAL SYSTEMS ──────────────────────────────────────
    private void BuildEssentialSystems(Scene scene)
    {
        // Check if key systems exist, if not create them
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var evt = new GameObject("[AUTO] EventSystem");
            evt.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evt.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
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
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
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
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = Vector2.zero;
        
        // AUTO-LOAD YOUR AI ART: Try to find a matching sprite in Resources
        TryLoadArtSprite(image, name);
        
        return obj;
    }
    
    /// <summary>
    /// Attempts to load your AI-generated art from Resources by matching the image name.
    /// Looks in: Resources/Art/, Resources/UI/Sprites/, Resources/UI/Icons/
    /// Falls back to the solid color if no art is found.
    /// </summary>
    private void TryLoadArtSprite(Image image, string assetName)
    {
        // Map common UI names to your actual AI art filenames
        string artPath = assetName switch
        {
            "Logo" => "UI/Sprites/arena_fall_logo",
            "SplashBG" => "UI/Sprites/splash_screen",
            "MinimapBorder" => "UI/Sprites/modern_br_hud",
            "BGEffects" or "Background" => "Art/Environment/skybox_concept",
            "Crosshair" => "UI/Sprites/crosshairs",
            _ => null
        };
        
        if (artPath == null) return;
        
        var tex = Resources.Load<Texture2D>(artPath);
        if (tex != null)
        {
            image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Debug.Log($"[SceneAutoBuilder] Loaded art: {artPath}");
        }
    }

    private GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color,
        Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax, Vector2 pivotMax)
    {
        var obj = new GameObject($"[AUTO] {name}", typeof(RectTransform), typeof(CanvasRenderer));
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        
        var fontAsset = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
        }
        
        var rt = obj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(200, 30);
        return obj;
    }

    private enum ButtonStyle { Primary, Secondary, Tertiary, Accent, Danger }

    private void CreateMenuButton(Transform parent, string name, string label, int fontSize, 
        ButtonStyle style, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivotMin, Vector2 pivotMax,
        Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObj = new GameObject($"[AUTO] {name}", typeof(RectTransform), typeof(CanvasRenderer));
        var image = buttonObj.AddComponent<Image>();
        var button = buttonObj.AddComponent<Button>();
        var rt = buttonObj.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivotMin;
        rt.anchoredPosition = position; rt.sizeDelta = size;

        // Try loading modern beveled button sprite
        var uiBtnTex = Resources.Load<Texture2D>("UI/Sprites/ui_buttons");
        if (uiBtnTex != null)
        {
            image.sprite = Sprite.Create(uiBtnTex, new Rect(0, 0, uiBtnTex.width, uiBtnTex.height), new Vector2(0.5f, 0.5f), 100f, 1, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
            image.type = Image.Type.Sliced;
        }

        Color baseBg = new Color(0.06f, 0.12f, 0.22f, 0.94f);
        Color neonColor = new Color(0f, 0.83f, 1f, 1f);
        switch (style)
        {
            case ButtonStyle.Primary:
                baseBg = new Color(0.18f, 0.08f, 0.05f, 0.95f);
                neonColor = new Color(1f, 0.42f, 0.21f, 1f);
                break;
            case ButtonStyle.Accent:
                baseBg = new Color(0.04f, 0.18f, 0.26f, 0.95f);
                neonColor = new Color(0f, 0.83f, 1f, 1f);
                break;
            case ButtonStyle.Danger:
                baseBg = new Color(0.24f, 0.05f, 0.08f, 0.95f);
                neonColor = new Color(1f, 0.14f, 0.27f, 1f);
                break;
        }
        image.color = baseBg;

        // Glowing Neon Side Bar (Left Edge)
        var neonObj = new GameObject("SideNeonStrip", typeof(RectTransform), typeof(CanvasRenderer));
        neonObj.transform.SetParent(buttonObj.transform, false);
        var nRt = neonObj.GetComponent<RectTransform>();
        nRt.anchorMin = new Vector2(0, 0); nRt.anchorMax = new Vector2(0, 1);
        nRt.offsetMin = Vector2.zero; nRt.offsetMax = Vector2.zero;
        nRt.sizeDelta = new Vector2(5, 0); nRt.anchoredPosition = new Vector2(2.5f, 0);
        var nImg = neonObj.AddComponent<Image>();
        nImg.color = neonColor;

        // Text
        var textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = fontSize; tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        if (style == ButtonStyle.Primary || style == ButtonStyle.Accent) tmp.fontStyle = FontStyles.Bold;
        var trt = textObj.GetComponent<RectTransform>();
        trt.SetParent(buttonObj.transform, false);
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.sizeDelta = new Vector2(-12, 0); trt.anchoredPosition = new Vector2(4, 0);

        if (onClick != null) button.onClick.AddListener(onClick);

        // Modern Tactile Scaling & Glow Animations
        var trigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        var enter = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { buttonObj.transform.localScale = new Vector3(1.04f, 1.04f, 1f); nImg.color = Color.white; });
        trigger.triggers.Add(enter);

        var exit = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => { buttonObj.transform.localScale = Vector3.one; nImg.color = neonColor; });
        trigger.triggers.Add(exit);

        var down = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown };
        down.callback.AddListener(_ => buttonObj.transform.localScale = new Vector3(0.96f, 0.96f, 1f));
        trigger.triggers.Add(down);

        var up = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp };
        up.callback.AddListener(_ => buttonObj.transform.localScale = new Vector3(1.02f, 1.02f, 1f));
        trigger.triggers.Add(up);
    }

    private void CreateSettingsSlider(Transform parent, string name, string label, float defaultValue, int index, Vector2 position)
    {
        var panel = CreatePanel(parent, name + "Panel", new Color(0, 0, 0, 0),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        var prt = panel.GetComponent<RectTransform>();
        prt.sizeDelta = new Vector2(500, 40);
        prt.anchoredPosition = position;

        CreateText(panel.transform, "Label", label, 16, Color.white,
            new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));

        var sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(CanvasRenderer));
        var slider = sliderObj.AddComponent<Slider>();
        var srt = sliderObj.GetComponent<RectTransform>();
        srt.SetParent(panel.transform, false);
        srt.anchorMin = new Vector2(1, 0.5f);
        srt.anchorMax = new Vector2(1, 0.5f);
        srt.pivot = new Vector2(1, 0.5f);
        srt.anchoredPosition = new Vector2(-10, 0);
        srt.sizeDelta = new Vector2(250, 20);

        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = defaultValue;
        slider.direction = Slider.Direction.LeftToRight;

        // Background
        var bgImage = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer));
        var bgImgComp = bgImage.AddComponent<Image>();
        bgImgComp.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
        var brt = bgImage.GetComponent<RectTransform>();
        brt.SetParent(sliderObj.transform, false);
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.sizeDelta = new Vector2(0, 6);
        slider.targetGraphic = bgImgComp;

        // Fill
        var fillImage = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer));
        var fillImgComp = fillImage.AddComponent<Image>();
        fillImgComp.color = new Color(0, 0.83f, 1, 1);
        var frt = fillImage.GetComponent<RectTransform>();
        frt.SetParent(sliderObj.transform, false);
        frt.anchorMin = new Vector2(0, 0);
        frt.anchorMax = new Vector2(0, 1);
        frt.sizeDelta = new Vector2(0, 6);
        slider.fillRect = frt;

        // Handle
        var handleImage = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer));
        var handleImgComp = handleImage.AddComponent<Image>();
        handleImgComp.color = new Color(1, 0.42f, 0.21f, 1);
        var hrt = handleImage.GetComponent<RectTransform>();
        hrt.SetParent(sliderObj.transform, false);
        hrt.sizeDelta = new Vector2(16, 16);
        slider.handleRect = hrt;
    }

    private Color RandomColor()
    {
        Color[] colors = {
            new Color(1, 0.42f, 0.21f), // Orange
            new Color(0, 0.83f, 1),     // Cyan
            new Color(0.27f, 0.85f, 0.27f), // Green
            new Color(0.8f, 0.3f, 0.8f), // Purple
            new Color(1, 0.7f, 0)       // Gold
        };
        return colors[Random.Range(0, colors.Length)];
    }
}

/// <summary>
/// Auto-loader to transition scenes after a delay
/// </summary>
public class AutoSceneLoader : MonoBehaviour
{
    public string sceneName = "MainMenu";
    public float delay = 2f;
    private float _timer;

    private void Start()
    {
        _timer = delay;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}

/// <summary>
/// Simple rotation animation for target dummies
/// </summary>
public class RotateAnimation : MonoBehaviour
{
    private void Update()
    {
        transform.Rotate(Vector3.up, 30 * Time.deltaTime);
    }
}

/// <summary>
/// Floating animation for loot items so they're visible
/// </summary>
public class LootFloatAnimation : MonoBehaviour
{
    private Vector3 _startPos;
    private float _offset;

    private void Start()
    {
        _startPos = transform.position;
        _offset = Random.Range(0, 6.28f);
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * 1.5f + _offset) * 0.3f;
        transform.position = new Vector3(_startPos.x, _startPos.y + y, _startPos.z);
        transform.Rotate(Vector3.up, 45 * Time.deltaTime);
    }
}

/// <summary>
/// Keeps a quad facing the camera — your AI art always visible!
/// </summary>
public class BillboardToCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main != null)
            transform.LookAt(Camera.main.transform);
    }
}
