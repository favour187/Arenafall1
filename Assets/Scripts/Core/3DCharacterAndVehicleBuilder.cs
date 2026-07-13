using System.Collections.Generic;
using UnityEngine;
using ArenaFall.Gameplay.Characters;
using ArenaFall.Gameplay.Vehicles;
using ArenaFall.Gameplay.Weapons;

namespace ArenaFall.Core
{
    /// <summary>
    /// Constructs 3D Compound Sci-Fi Character Rigs & Drivable Physics Vehicles.
    /// Replaces 2D billboards with articulated armor, backpacks, glowing visors, and drivable rigs.
    /// </summary>
    public class 3DCharacterAndVehicleBuilder : MonoBehaviour
    {
        private static Dictionary<string, Texture2D> _artCache = new();

        public static void PreloadArtAssets()
        {
            string[] names = { "armor_vests", "backpack_tiers", "ranger_atv", "hovercraft", "cyclone_bike", "transport_truck", "armored_mech_walker", "cyber_commando_skin", "ai_bot", "male_character_front", "female_character_front" };
            foreach (var n in names)
            {
                var tex = Resources.Load<Texture2D>($"Art/Characters/{n}") ?? Resources.Load<Texture2D>($"Art/Vehicles/{n}") ?? Resources.Load<Texture2D>($"Art/Characters/Male/{n}") ?? Resources.Load<Texture2D>($"Art/Characters/Female/{n}") ?? Resources.Load<Texture2D>($"Art/Characters/NPCs/{n}");
                if (tex != null) _artCache[n] = tex;
            }
        }

        // ─── 1. 3D SCI-FI CHARACTER RIG BUILDER ─────────────────────
        public static GameObject Build3DSciFiCharacterRig(Transform parent, string characterName, bool isPlayer, Color teamColor)
        {
            var root = new GameObject($"[3D_RIG] {characterName}");
            root.transform.SetParent(parent);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            var litShader = Shader.Find("Universal Render Pipeline/Lit");

            // 1. Torso / Combat Suit (20% taller for heroic proportions)
            var torso = CreateRigSegment("Torso", root.transform, new Vector3(0, 1.2f, 0), new Vector3(0.6f, 0.7f, 0.35f), teamColor, litShader);

            // 2. Tactical Armor Vest overlay using armor_vests.png
            var armorVest = CreateRigSegment("ArmorVest", torso.transform, new Vector3(0, 0, 0), new Vector3(1.05f, 0.85f, 1.08f), new Color(0.2f, 0.22f, 0.26f), litShader);
            if (_artCache.TryGetValue("armor_vests", out var vestTex) && vestTex != null)
            {
                armorVest.GetComponent<MeshRenderer>().material.mainTexture = vestTex;
            }

            // 3. Combat Backpack (Tier 2/3) using backpack_tiers.png
            var backpack = CreateRigSegment("Backpack", torso.transform, new Vector3(0, 0.1f, -0.6f), new Vector3(0.7f, 0.8f, 0.5f), new Color(0.15f, 0.18f, 0.24f), litShader);
            if (_artCache.TryGetValue("backpack_tiers", out var bpTex) && bpTex != null)
            {
                backpack.GetComponent<MeshRenderer>().material.mainTexture = bpTex;
            }

            // 4. Helmet / Head
            var head = CreateRigSegment("Head", root.transform, new Vector3(0, 1.75f, 0), new Vector3(0.4f, 0.4f, 0.45f), new Color(0.15f, 0.18f, 0.22f), litShader);

            // 5. Glowing Cyan Holographic Visor (#00D4FF)
            var visor = CreateRigSegment("HoloVisor", head.transform, new Vector3(0, 0.05f, 0.25f), new Vector3(0.85f, 0.35f, 0.2f), new Color(0f, 0.83f, 1f), litShader);
            visor.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", new Color(0f, 0.83f, 1f) * 2f);

            // 6. Limbs (Shoulders, Arms, Legs)
            CreateRigSegment("LeftShoulder", root.transform, new Vector3(-0.45f, 1.35f, 0), new Vector3(0.25f, 0.6f, 0.25f), teamColor, litShader);
            CreateRigSegment("RightShoulder", root.transform, new Vector3(0.45f, 1.35f, 0), new Vector3(0.25f, 0.6f, 0.25f), teamColor, litShader);
            CreateRigSegment("LeftLeg", root.transform, new Vector3(-0.2f, 0.45f, 0), new Vector3(0.26f, 0.9f, 0.26f), new Color(0.12f, 0.14f, 0.18f), litShader);
            CreateRigSegment("RightLeg", root.transform, new Vector3(0.2f, 0.45f, 0), new Vector3(0.26f, 0.9f, 0.26f), new Color(0.12f, 0.14f, 0.18f), litShader);

            // 7. Weapon Socket (Right Hand Grip Point)
            var weaponSocket = new GameObject("WeaponSocket");
            weaponSocket.transform.SetParent(root.transform);
            weaponSocket.transform.localPosition = new Vector3(0.45f, 1.15f, 0.45f);
            weaponSocket.transform.localRotation = Quaternion.Euler(0, 0, 0);

            // 8. Shield Bubble Sphere (Disabled initially, enabled on hit)
            var shieldSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldSphere.name = "EnergyShieldBubble";
            shieldSphere.transform.SetParent(root.transform);
            shieldSphere.transform.localPosition = new Vector3(0, 1.1f, 0);
            shieldSphere.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
            var sRenderer = shieldSphere.GetComponent<MeshRenderer>();
            var sMat = new Material(litShader);
            sMat.color = new Color(0f, 0.83f, 1f, 0.25f);
            sMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            sMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            sMat.SetInt("_ZWrite", 0);
            sMat.DisableKeyword("_ALPHATEST_ON");
            sMat.EnableKeyword("_ALPHABLEND_ON");
            sMat.renderQueue = 3000;
            sRenderer.material = sMat;
            shieldSphere.SetActive(false);

            return root;
        }

        private static GameObject CreateRigSegment(string name, Transform parent, Vector3 localPos, Vector3 size, Color color, Shader shader)
        {
            var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = name;
            seg.transform.SetParent(parent);
            seg.transform.localPosition = localPos;
            seg.transform.localScale = size;
            var renderer = seg.GetComponent<MeshRenderer>();
            var mat = new Material(shader);
            mat.color = color;
            renderer.material = mat;
            return seg;
        }

        // ─── 2. DRIVABLE VEHICLE SPAWNER ────────────────────────────
        public static void SpawnDrivableVehiclesAcrossMap()
        {
            Debug.Log("[3DCharacterAndVehicleBuilder] Spawning Drivable Physics Vehicles across Compound Garages...");
            PreloadArtAssets();

            // Spawn Ranger ATV at Solar Fields
            SpawnVehicle("Ranger ATV", "ranger_atv", new Vector3(850, 2, 850), new Vector3(2.4f, 1.6f, 3.8f), new Color(0.2f, 0.3f, 0.15f));

            // Spawn Hovercraft at Hydro Station
            SpawnVehicle("Sci-Fi Hovercraft", "hovercraft", new Vector3(3150, 2, 850), new Vector3(3.2f, 1.4f, 5.2f), new Color(0.1f, 0.25f, 0.4f));

            // Spawn Cyclone Bike at Nexus Tower
            SpawnVehicle("Cyclone Recon Bike", "cyclone_bike", new Vector3(2050, 2, 1920), new Vector3(1.2f, 1.5f, 2.8f), new Color(0.8f, 0.4f, 0.1f));

            // Spawn Transport Truck at Industrial Factory
            SpawnVehicle("Heavy Armored Truck", "transport_truck", new Vector3(3250, 2, 3150), new Vector3(3.4f, 2.8f, 6.5f), new Color(0.22f, 0.24f, 0.28f));

            // Spawn 5th Heavy Drivable Vehicle: Armored Mech Walker at Orbital Relay Station
            SpawnVehicle("Armored Mech Walker", "armored_mech_walker", new Vector3(2000, 2, 3150), new Vector3(2.8f, 3.6f, 3.2f), new Color(0.12f, 0.2f, 0.35f));

            Debug.Log("[3DCharacterAndVehicleBuilder] ✓ 5 Drivable Vehicles (including Armored Mech Walker) active.");
        }

        private static GameObject SpawnVehicle(string vehicleName, string textureKey, Vector3 position, Vector3 size, Color chassisColor)
        {
            var vehicleObj = new GameObject($"[VEHICLE] {vehicleName}");
            vehicleObj.transform.position = position;

            // Chassis
            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(vehicleObj.transform);
            chassis.transform.localPosition = new Vector3(0, size.y * 0.5f, 0);
            chassis.transform.localScale = size;

            var renderer = chassis.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (_artCache.TryGetValue(textureKey, out var tex) && tex != null)
            {
                mat.mainTexture = tex;
            }
            else
            {
                mat.color = chassisColor;
            }
            renderer.material = mat;

            // 4 Wheels / Hover Pods
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -size.x * 0.55f : size.x * 0.55f;
                float z = (i < 2) ? size.z * 0.35f : -size.z * 0.35f;
                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = $"Wheel_{i}";
                wheel.transform.SetParent(vehicleObj.transform);
                wheel.transform.localPosition = new Vector3(x, size.y * 0.25f, z);
                wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
                wheel.transform.localScale = new Vector3(size.y * 0.5f, 0.4f, size.y * 0.5f);
                wheel.GetComponent<MeshRenderer>().material.color = Color.black;
            }

            // Glowing Headlights
            var leftLight = new GameObject("LeftHeadlight").AddComponent<Light>();
            leftLight.transform.SetParent(vehicleObj.transform);
            leftLight.transform.localPosition = new Vector3(-size.x * 0.35f, size.y * 0.6f, size.z * 0.5f);
            leftLight.color = new Color(0f, 0.83f, 1f);
            leftLight.range = 30f;
            leftLight.intensity = 2f;

            var rightLight = new GameObject("RightHeadlight").AddComponent<Light>();
            rightLight.transform.SetParent(vehicleObj.transform);
            rightLight.transform.localPosition = new Vector3(size.x * 0.35f, size.y * 0.6f, size.z * 0.5f);
            rightLight.color = new Color(0f, 0.83f, 1f);
            rightLight.range = 30f;
            rightLight.intensity = 2f;

            // Attach VehicleController & Rigidbody
            var rb = vehicleObj.AddComponent<Rigidbody>();
            rb.mass = 1500f;
            rb.drag = 0.5f;
            rb.angularDrag = 2f;

            var vc = vehicleObj.AddComponent<VehicleController>();
            // If VehicleController properties exist they can be tweaked or handled cleanly

            return vehicleObj;
        }

        // ─── 3. AI BOTS WITH 3D SCI-FI RIGS ─────────────────────────
        public static void Spawn3DAIBotsAcrossCompounds(int count = 59)
        {
            Debug.Log($"[3DCharacterAndVehicleBuilder] Spawning {count} 3D AI Bots across all 7 Compound POIs to reach 60-player lobby fill...");
            PreloadArtAssets();

            Vector3[] compoundCenters = {
                new Vector3(2000, 2, 2000), // Nexus Tower
                new Vector3(3200, 2, 3200), // Industrial Factory
                new Vector3(3200, 2, 800),  // Hydro Station
                new Vector3(800, 2, 3200),  // Frost Depots
                new Vector3(800, 2, 800),   // Solar Fields
                new Vector3(600, 2, 2000),  // Crash Site
                new Vector3(2000, 2, 3200)  // Orbital Relay Station
            };

            for (int i = 0; i < count; i++)
            {
                Vector3 center = compoundCenters[i % compoundCenters.Length];
                Vector3 spawnPos = center + new Vector3(Random.Range(-55f, 55f), 1f, Random.Range(-55f, 55f));

                var botObj = new GameObject($"[AI_BOT] Elite_Patrol_{i}");
                botObj.transform.position = spawnPos;

                var cc = botObj.AddComponent<CharacterController>();
                cc.height = 2f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0, 1f, 0);

                botObj.AddComponent<CharacterHealth>();

                // Build 3D Character Rig for Bot
                Color botColor = (i % 2 == 0) ? new Color(0.7f, 0.2f, 0.2f) : new Color(0.8f, 0.4f, 0.1f);
                Build3DSciFiCharacterRig(botObj.transform, "Bot3DRig", false, botColor);

                // Attach AIController so bots actively patrol, aim, and engage
                botObj.AddComponent<Gameplay.AI.AIController>();
            }
            Debug.Log($"[3DCharacterAndVehicleBuilder] ✓ {count} 3D AI Bots active across all 7 compounds.");
        }
    }
}
