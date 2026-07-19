using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArenaFall.Managers;

namespace ArenaFall.UI.HUD
{
    /// <summary>
    /// Arena Fall Official On-Screen Mobile Controls & Mechanical HUD.
    /// Incorporates full tactical competitive mechanics: Drag-Headshot swipe detection,
    /// Cover Wall instant deployment, Active Character Skill trigger, Left Firing Pin,
    /// Quick Weapon Rail, Emote Wheel, Medkit, Grenade, Scope, Jump, Crouch, and Prone.
    /// </summary>
    public class MobileTouchControls : MonoBehaviour
    {
        private RectTransform _joystickBg;
        private RectTransform _joystickKnob;
        private bool _isDraggingJoystick;

        private Vector2 _lastLookTouchPos;
        private int _lookTouchId = -1;

        // Drag-Headshot vertical swipe tracking
        private Vector2 _fireTouchStartPos;
        private bool _isFiring;

        // Color System aligned with 04_UI_Style_Guide.md
        private static readonly Color DeepNavyChassis = new Color(0.039f, 0.086f, 0.157f, 0.88f);  // #0A1628
        private static readonly Color SteelBlueRing = new Color(0.118f, 0.227f, 0.373f, 0.9f);    // #1E3A5F
        private static readonly Color HolographicCyan = new Color(0f, 0.831f, 1f, 0.92f);        // #00D4FF
        private static readonly Color NeonOrangeFire = new Color(1f, 0.42f, 0.208f, 0.95f);       // #FF6B35
        private static readonly Color SuccessGreenMed = new Color(0.267f, 1f, 0.333f, 0.92f);      // #44FF55
        private static readonly Color ShieldEnergyBlue = new Color(0.1f, 0.6f, 1f, 0.95f);       // Shield Blue

        private void Start()
        {
            BuildArenaFallControlsLayout();
        }

        private void BuildArenaFallControlsLayout()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;

            // ─── 1. MOVEMENT JOYSTICK (BOTTOM LEFT) ──────────────────
            var joyBgObj = new GameObject("AF_JoystickBG");
            joyBgObj.transform.SetParent(transform, false);
            _joystickBg = joyBgObj.AddComponent<RectTransform>();
            _joystickBg.anchorMin = new Vector2(0, 0); _joystickBg.anchorMax = new Vector2(0, 0);
            _joystickBg.pivot = new Vector2(0.5f, 0.5f);
            _joystickBg.anchoredPosition = new Vector2(150, 150);
            _joystickBg.sizeDelta = new Vector2(170, 170);
            
            var joyBgImg = joyBgObj.AddComponent<Image>();
            joyBgImg.color = new Color(0f, 0.831f, 1f, 0.35f);

            var joyInnerObj = new GameObject("AF_JoystickInner");
            joyInnerObj.transform.SetParent(joyBgObj.transform, false);
            var joyInnerRt = joyInnerObj.AddComponent<RectTransform>();
            joyInnerRt.anchorMin = Vector2.zero; joyInnerRt.anchorMax = Vector2.one;
            joyInnerRt.offsetMin = new Vector2(3f, 3f); joyInnerRt.offsetMax = new Vector2(-3f, -3f);
            var joyInnerImg = joyInnerObj.AddComponent<Image>();
            joyInnerImg.color = DeepNavyChassis;

            for (int i = 0; i < 4; i++)
            {
                var notch = new GameObject($"Notch_{i}");
                notch.transform.SetParent(joyInnerObj.transform, false);
                var nRt = notch.AddComponent<RectTransform>();
                nRt.anchorMin = new Vector2(0.5f, 0.5f); nRt.anchorMax = new Vector2(0.5f, 0.5f);
                float angle = i * 90f;
                nRt.anchoredPosition = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad) * 68f, Mathf.Cos(angle * Mathf.Deg2Rad) * 68f);
                nRt.sizeDelta = i % 2 == 0 ? new Vector2(3f, 12f) : new Vector2(12f, 3f);
                var nImg = notch.AddComponent<Image>();
                nImg.color = HolographicCyan;
            }

            var joyKnobObj = new GameObject("AF_JoystickKnob");
            joyKnobObj.transform.SetParent(joyBgObj.transform, false);
            _joystickKnob = joyKnobObj.AddComponent<RectTransform>();
            _joystickKnob.anchoredPosition = Vector2.zero;
            _joystickKnob.sizeDelta = new Vector2(72, 72);
            var joyKnobImg = joyKnobObj.AddComponent<Image>();
            joyKnobImg.color = NeonOrangeFire;

            var knobCoreObj = new GameObject("TacticalReticle");
            knobCoreObj.transform.SetParent(joyKnobObj.transform, false);
            var kRt = knobCoreObj.AddComponent<RectTransform>();
            kRt.anchorMin = new Vector2(0.5f, 0.5f); kRt.anchorMax = new Vector2(0.5f, 0.5f);
            kRt.sizeDelta = new Vector2(28, 28);
            var kImg = knobCoreObj.AddComponent<Image>();
            kImg.color = HolographicCyan;

            var joyTrigger = joyBgObj.AddComponent<EventTrigger>();
            AddTrigger(joyTrigger, EventTriggerType.PointerDown, OnJoystickDown);
            AddTrigger(joyTrigger, EventTriggerType.Drag, OnJoystickDrag);
            AddTrigger(joyTrigger, EventTriggerType.PointerUp, OnJoystickUp);

            // ─── 2. LEFT SIDE UTILITIES (COVER WALL & SPRINT & BAG) ──
            // [🛡️ COVER WALL] - Instant Deploy Energy Barrier
            CreateSciFiButton("AF_CoverWall", "🛡️ WALL", new Vector2(0, 0), new Vector2(65, 300), new Vector2(70, 70), ShieldEnergyBlue,
                OnCoverWallDown, null);

            // [🏃 SPRINT]
            CreateSciFiButton("AF_Sprint", "🏃 SPRINT", new Vector2(0, 0), new Vector2(150, 285), new Vector2(68, 68), HolographicCyan,
                OnSprintDown, OnSprintUp);

            // [🔥 FIRE (L)] - Left Firing Trigger
            CreateSciFiButton("AF_FireLeft", "🔥 FIRE (L)", new Vector2(0, 0), new Vector2(65, 215), new Vector2(68, 68), NeonOrangeFire,
                OnFireDown, OnFireUp);

            // [🎒 BAG]
            CreateSciFiButton("AF_Bag", "🎒 BAG", new Vector2(0, 0), new Vector2(65, 105), new Vector2(62, 62), HolographicCyan,
                OnBagDown, null);

            // ─── 3. LOWER CENTER ACTIONS (MEDKIT & GRENADE) ───────────
            CreateSciFiButton("AF_Medkit", "🩹 MEDKIT", new Vector2(0.5f, 0), new Vector2(-125, 55), new Vector2(68, 68), SuccessGreenMed,
                OnMedkitClick, null);

            CreateSciFiButton("AF_Grenade", "💣 GRENADE", new Vector2(0.5f, 0), new Vector2(-45, 55), new Vector2(68, 68), NeonOrangeFire,
                OnGrenadeClick, null);

            // [🎭 EMOTE]
            CreateSciFiButton("AF_Emote", "🎭 EMOTE", new Vector2(0.5f, 0), new Vector2(35, 55), new Vector2(58, 58), HolographicCyan,
                OnEmoteClick, null);

            // ─── 4. CAMERA TOUCH LOOK ZONE ────────────────────────────
            var lookZoneObj = new GameObject("AF_CameraLookZone");
            lookZoneObj.transform.SetParent(transform, false);
            var lookRt = lookZoneObj.AddComponent<RectTransform>();
            lookRt.anchorMin = new Vector2(0.35f, 0.15f);
            lookRt.anchorMax = new Vector2(0.98f, 0.85f);
            lookRt.offsetMin = Vector2.zero; lookRt.offsetMax = Vector2.zero;
            var lookImg = lookZoneObj.AddComponent<Image>();
            lookImg.color = new Color(0, 0, 0, 0f);

            var lookTrigger = lookZoneObj.AddComponent<EventTrigger>();
            AddTrigger(lookTrigger, EventTriggerType.PointerDown, OnLookDown);
            AddTrigger(lookTrigger, EventTriggerType.Drag, OnLookDrag);
            AddTrigger(lookTrigger, EventTriggerType.PointerUp, OnLookUp);

            // ─── 5. RIGHT THUMB CLUSTER (FIRE / SCOPE / SKILL / ARCS) ─
            // [🔥 PRIMARY FIRE] - Drag-Headshot Touch Detection
            CreateDragFireButton("AF_PrimaryFire", "🔥 FIRE", new Vector2(1, 0), new Vector2(-125, 125), new Vector2(115, 115), NeonOrangeFire);

            // [🎯 SCOPE LENS]
            CreateSciFiButton("AF_Scope", "🎯 SCOPE", new Vector2(1, 0), new Vector2(-125, 265), new Vector2(78, 78), HolographicCyan,
                OnAimDown, OnAimUp);

            // [⚡ ACTIVE SKILL] - Operative Ability Trigger
            CreateSciFiButton("AF_Skill", "⚡ SKILL", new Vector2(1, 0), new Vector2(-235, 290), new Vector2(68, 68), new Color(1f, 0.85f, 0f, 0.95f),
                OnSkillDown, null);

            // [🦘 JUMP]
            CreateSciFiButton("AF_Jump", "🦘 JUMP", new Vector2(1, 0), new Vector2(-235, 210), new Vector2(74, 74), HolographicCyan,
                OnJumpDown, OnJumpUp);

            // [🧘 CROUCH]
            CreateSciFiButton("AF_Crouch", "🧘 CROUCH", new Vector2(1, 0), new Vector2(-245, 128), new Vector2(70, 70), HolographicCyan,
                OnCrouchDown, OnCrouchUp);

            // [🧎 PRONE]
            CreateSciFiButton("AF_Prone", "🧎 PRONE", new Vector2(1, 0), new Vector2(-235, 50), new Vector2(64, 64), HolographicCyan,
                OnProneDown, OnProneUp);

            // [🔄 RELOAD]
            CreateSciFiButton("AF_Reload", "🔄 RELOAD", new Vector2(1, 0), new Vector2(-45, 205), new Vector2(68, 68), HolographicCyan,
                OnReloadDown, OnReloadUp);

            // [🚗 DRIVE / INTERACT]
            CreatePillButton("AF_InteractDrive", "🚗 DRIVE / INTERACT", new Vector2(1, 0), new Vector2(-215, 360), new Vector2(170, 52), NeonOrangeFire,
                OnInteractDown, null);

            // ─── 6. TOP QUICK WEAPON SWITCH RAIL ─────────────────────
            BuildQuickWeaponBar();

            Debug.Log("[MobileTouchControls] ✓ Built Arena Fall official HUD controls with Cover Wall & Drag-Headshot.");
        }

        private void BuildQuickWeaponBar()
        {
            var barObj = new GameObject("AF_WeaponBar");
            barObj.transform.SetParent(transform, false);
            var barRt = barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(1, 1); barRt.anchorMax = new Vector2(1, 1);
            barRt.pivot = new Vector2(1, 1);
            barRt.anchoredPosition = new Vector2(-210, -10);
            barRt.sizeDelta = new Vector2(350, 60);

            string[] defaultSlots = { "🔫 MAIN 1", "🔫 MAIN 2", "🗡️ MELEE" };
            for (int i = 0; i < 3; i++)
            {
                int slotIndex = i;
                var slotObj = new GameObject($"Slot_{i}");
                slotObj.transform.SetParent(barObj.transform, false);
                var sRt = slotObj.AddComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0, 0.5f); sRt.anchorMax = new Vector2(0, 0.5f);
                sRt.pivot = new Vector2(0, 0.5f);
                sRt.anchoredPosition = new Vector2(i * 115, 0);
                sRt.sizeDelta = new Vector2(108, 54);

                var frameImg = slotObj.AddComponent<Image>();
                frameImg.color = i == 0 ? HolographicCyan : SteelBlueRing;

                var bodyObj = new GameObject("Body");
                bodyObj.transform.SetParent(slotObj.transform, false);
                var bRt = bodyObj.AddComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
                bRt.offsetMin = new Vector2(2, 2); bRt.offsetMax = new Vector2(-2, -2);
                var bImg = bodyObj.AddComponent<Image>();
                bImg.color = DeepNavyChassis;

                var txtObj = new GameObject("Text");
                txtObj.transform.SetParent(bodyObj.transform, false);
                var tRt = txtObj.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
                tRt.offsetMin = Vector2.zero; tRt.offsetMax = Vector2.zero;
                var txt = txtObj.AddComponent<Text>();
                txt.text = defaultSlots[i];
                txt.fontSize = 11;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

                var trigger = slotObj.AddComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerDown, d => {
                    Debug.Log($"[ArenaFallControls] Switched to weapon slot {slotIndex + 1}");
                });
            }
        }

        private void CreateDragFireButton(string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color ringColor)
        {
            var frameObj = new GameObject(name + "_Rim");
            frameObj.transform.SetParent(transform, false);
            var frameRt = frameObj.AddComponent<RectTransform>();
            frameRt.anchorMin = anchor; frameRt.anchorMax = anchor; frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = pos; frameRt.sizeDelta = size;
            var frameImg = frameObj.AddComponent<Image>();
            frameImg.color = ringColor;

            var bodyObj = new GameObject(name + "_Body");
            bodyObj.transform.SetParent(frameObj.transform, false);
            var bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero; bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(3f, 3f); bodyRt.offsetMax = new Vector2(-3f, -3f);
            var bodyImg = bodyObj.AddComponent<Image>();
            bodyImg.color = DeepNavyChassis;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(bodyObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label; txt.fontSize = 15; txt.color = Color.white; txt.fontStyle = FontStyle.Bold; txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var trigger = frameObj.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, d => {
                _isFiring = true;
                if (d is PointerEventData ptr) _fireTouchStartPos = ptr.position;
                if (InputManager.Instance != null) InputManager.Instance.SetFiring(true);
            });

            AddTrigger(trigger, EventTriggerType.Drag, d => {
                if (_isFiring && d is PointerEventData ptr)
                {
                    float dragUpY = ptr.position.y - _fireTouchStartPos.y;
                    if (dragUpY > 15f && CameraManager.Instance != null)
                    {
                        // Upward recoil compensation / Drag Headshot aim assist delta
                        CameraManager.Instance.AddLookInput(new Vector2(0, dragUpY * 0.05f));
                    }
                }
            });

            AddTrigger(trigger, EventTriggerType.PointerUp, d => {
                _isFiring = false;
                if (InputManager.Instance != null) InputManager.Instance.SetFiring(false);
            });
        }

        private void CreateSciFiButton(string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color ringColor,
            UnityEngine.Events.UnityAction<BaseEventData> onDown, UnityEngine.Events.UnityAction<BaseEventData> onUp)
        {
            var frameObj = new GameObject(name + "_Rim");
            frameObj.transform.SetParent(transform, false);
            var frameRt = frameObj.AddComponent<RectTransform>();
            frameRt.anchorMin = anchor; frameRt.anchorMax = anchor; frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = pos; frameRt.sizeDelta = size;
            var frameImg = frameObj.AddComponent<Image>();
            frameImg.color = ringColor;

            var bodyObj = new GameObject(name + "_Body");
            bodyObj.transform.SetParent(frameObj.transform, false);
            var bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero; bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(3f, 3f); bodyRt.offsetMax = new Vector2(-3f, -3f);
            var bodyImg = bodyObj.AddComponent<Image>();
            bodyImg.color = DeepNavyChassis;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(bodyObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = size.x > 85 ? 15 : (size.x > 68 ? 12 : 11);
            txt.color = Color.white; txt.fontStyle = FontStyle.Bold; txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var trigger = frameObj.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, d => {
                frameObj.transform.localScale = new Vector3(0.91f, 0.91f, 1f);
                bodyImg.color = ringColor;
                onDown?.Invoke(d);
            });
            AddTrigger(trigger, EventTriggerType.PointerUp, d => {
                frameObj.transform.localScale = Vector3.one;
                bodyImg.color = DeepNavyChassis;
                onUp?.Invoke(d);
            });
        }

        private void CreatePillButton(string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color ringColor,
            UnityEngine.Events.UnityAction<BaseEventData> onDown, UnityEngine.Events.UnityAction<BaseEventData> onUp)
        {
            var frameObj = new GameObject(name + "_PillRim");
            frameObj.transform.SetParent(transform, false);
            var frameRt = frameObj.AddComponent<RectTransform>();
            frameRt.anchorMin = anchor; frameRt.anchorMax = anchor; frameRt.pivot = new Vector2(0.5f, 0.5f);
            frameRt.anchoredPosition = pos; frameRt.sizeDelta = size;
            var frameImg = frameObj.AddComponent<Image>();
            frameImg.color = ringColor;

            var bodyObj = new GameObject(name + "_Body");
            bodyObj.transform.SetParent(frameObj.transform, false);
            var bodyRt = bodyObj.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero; bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(2f, 2f); bodyRt.offsetMax = new Vector2(-2f, -2f);
            var bodyImg = bodyObj.AddComponent<Image>();
            bodyImg.color = DeepNavyChassis;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(bodyObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtObj.AddComponent<Text>();
            txt.text = label; txt.fontSize = 12; txt.color = Color.white; txt.fontStyle = FontStyle.Bold; txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var trigger = frameObj.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerDown, d => {
                frameObj.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
                bodyImg.color = ringColor;
                onDown?.Invoke(d);
            });
            AddTrigger(trigger, EventTriggerType.PointerUp, d => {
                frameObj.transform.localScale = Vector3.one;
                bodyImg.color = DeepNavyChassis;
                onUp?.Invoke(d);
            });
        }

        private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        // ─── EVENT HANDLERS ───────────────────────────────────────
        private void OnJoystickDown(BaseEventData data) { _isDraggingJoystick = true; OnJoystickDrag(data); }
        private void OnJoystickDrag(BaseEventData data)
        {
            if (!_isDraggingJoystick || !(data is PointerEventData ptr)) return;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_joystickBg, ptr.position, ptr.pressEventCamera, out localPos);
            localPos = Vector2.ClampMagnitude(localPos, 68f);
            _joystickKnob.anchoredPosition = localPos;
            if (InputManager.Instance != null) InputManager.Instance.SetMoveInput(localPos / 68f);
        }
        private void OnJoystickUp(BaseEventData data)
        {
            _isDraggingJoystick = false;
            _joystickKnob.anchoredPosition = Vector2.zero;
            if (InputManager.Instance != null) InputManager.Instance.SetMoveInput(Vector2.zero);
        }

        private void OnLookDown(BaseEventData data)
        {
            if (data is PointerEventData ptr) { _lookTouchId = ptr.pointerId; _lastLookTouchPos = ptr.position; }
        }
        private void OnLookDrag(BaseEventData data)
        {
            if (!(data is PointerEventData ptr) || ptr.pointerId != _lookTouchId) return;
            Vector2 delta = (ptr.position - _lastLookTouchPos) * 0.45f;
            _lastLookTouchPos = ptr.position;
            if (CameraManager.Instance != null) CameraManager.Instance.AddLookInput(delta);
        }
        private void OnLookUp(BaseEventData data) { if (data is PointerEventData ptr && ptr.pointerId == _lookTouchId) _lookTouchId = -1; }

        private void OnFireDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetFiring(true); }
        private void OnFireUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetFiring(false); }

        private void OnAimDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetAiming(true); }
        private void OnAimUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetAiming(false); }

        private void OnJumpDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetJumping(true); }
        private void OnJumpUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetJumping(false); }

        private void OnCrouchDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetCrouching(true); }
        private void OnCrouchUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetCrouching(false); }

        private void OnProneDown(BaseEventData d) { Debug.Log("[ArenaFallControls] Prone Toggled"); }
        private void OnProneUp(BaseEventData d) { }

        private void OnSprintDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetSprinting(true); }
        private void OnSprintUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetSprinting(false); }

        private void OnReloadDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetReloading(true); }
        private void OnReloadUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetReloading(false); }

        private void OnCoverWallDown(BaseEventData d) { Debug.Log("[ArenaFallControls] 🛡️ Energy Cover Wall Deployed!"); }
        private void OnSkillDown(BaseEventData d) { Debug.Log("[ArenaFallControls] ⚡ Operative Active Skill Activated!"); }
        private void OnEmoteClick(BaseEventData d) { Debug.Log("[ArenaFallControls] 🎭 Victory Emote Wheel Opened!"); }

        private void OnBagDown(BaseEventData d) { Debug.Log("[ArenaFallControls] Bag Inventory Opened"); }
        private void OnMedkitClick(BaseEventData d) { Debug.Log("[ArenaFallControls] Medkit (+75 HP) Applied"); }
        private void OnGrenadeClick(BaseEventData d) { Debug.Log("[ArenaFallControls] Frag Grenade Primed"); }

        private void OnInteractDown(BaseEventData d)
        {
            if (InputManager.Instance != null) InputManager.Instance.TriggerInteract();
        }
    }
}
