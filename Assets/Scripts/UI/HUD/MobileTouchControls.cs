using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArenaFall.Managers;

namespace ArenaFall.UI.HUD
{
    /// <summary>
    /// Virtual On-Screen Touch Controls for Mobile (Android & iOS).
    /// Generates virtual joysticks, camera rotation zones, and large touch action buttons
    /// so mobile gamers can run, aim, fire, jump, and drive vehicles seamlessly.
    /// </summary>
    public class MobileTouchControls : MonoBehaviour
    {
        private RectTransform _joystickBg;
        private RectTransform _joystickKnob;
        private Vector2 _joystickStartPos;
        private bool _isDraggingJoystick;

        private Vector2 _lastLookTouchPos;
        private int _lookTouchId = -1;

        private void Start()
        {
            BuildMobileTouchLayout();
        }

        private void BuildMobileTouchLayout()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;

            // ─── 1. VIRTUAL MOVEMENT JOYSTICK (BOTTOM LEFT) ────────
            var joyBgObj = new GameObject("TouchJoystickBG");
            joyBgObj.transform.SetParent(transform, false);
            _joystickBg = joyBgObj.AddComponent<RectTransform>();
            _joystickBg.anchorMin = new Vector2(0, 0);
            _joystickBg.anchorMax = new Vector2(0, 0);
            _joystickBg.pivot = new Vector2(0.5f, 0.5f);
            _joystickBg.anchoredPosition = new Vector2(140, 140);
            _joystickBg.sizeDelta = new Vector2(160, 160);
            var joyBgImg = joyBgObj.AddComponent<Image>();
            joyBgImg.color = new Color(0, 0.83f, 1, 0.35f);

            var joyKnobObj = new GameObject("TouchJoystickKnob");
            joyKnobObj.transform.SetParent(joyBgObj.transform, false);
            _joystickKnob = joyKnobObj.AddComponent<RectTransform>();
            _joystickKnob.anchoredPosition = Vector2.zero;
            _joystickKnob.sizeDelta = new Vector2(70, 70);
            var joyKnobImg = joyKnobObj.AddComponent<Image>();
            joyKnobImg.color = new Color(1, 0.42f, 0.21f, 0.85f);

            _joystickStartPos = _joystickBg.anchoredPosition;

            // Attach EventTriggers to Joystick
            var joyTrigger = joyBgObj.AddComponent<EventTrigger>();
            AddTrigger(joyTrigger, EventTriggerType.PointerDown, OnJoystickDown);
            AddTrigger(joyTrigger, EventTriggerType.Drag, OnJoystickDrag);
            AddTrigger(joyTrigger, EventTriggerType.PointerUp, OnJoystickUp);

            // ─── 2. CAMERA LOOK TOUCH ZONE (RIGHT HALF SCREEN) ─────
            var lookZoneObj = new GameObject("CameraLookTouchZone");
            lookZoneObj.transform.SetParent(transform, false);
            var lookRt = lookZoneObj.AddComponent<RectTransform>();
            lookRt.anchorMin = new Vector2(0.4f, 0.2f);
            lookRt.anchorMax = new Vector2(1f, 0.85f);
            lookRt.offsetMin = Vector2.zero;
            lookRt.offsetMax = Vector2.zero;
            var lookImg = lookZoneObj.AddComponent<Image>();
            lookImg.color = new Color(0, 0, 0, 0f); // Invisible touch interceptor

            var lookTrigger = lookZoneObj.AddComponent<EventTrigger>();
            AddTrigger(lookTrigger, EventTriggerType.PointerDown, OnLookDown);
            AddTrigger(lookTrigger, EventTriggerType.Drag, OnLookDrag);
            AddTrigger(lookTrigger, EventTriggerType.PointerUp, OnLookUp);

            // ─── 3. LARGE TOUCH ACTION BUTTONS (BOTTOM RIGHT) ──────
            // [🔥 FIRE BUTTON] - Large primary thumb button
            CreateTouchButton("TouchFireBtn", "🔥 FIRE", new Vector2(1, 0), new Vector2(-110, 110), new Vector2(95, 95), new Color(1f, 0.22f, 0.27f, 0.85f),
                OnFireDown, OnFireUp);

            // [🎯 AIM BUTTON]
            CreateTouchButton("TouchAimBtn", "🎯 AIM", new Vector2(1, 0), new Vector2(-220, 95), new Vector2(80, 80), new Color(0f, 0.83f, 1f, 0.85f),
                OnAimDown, OnAimUp);

            // [🦘 JUMP BUTTON]
            CreateTouchButton("TouchJumpBtn", "🦘 JUMP", new Vector2(1, 0), new Vector2(-110, 220), new Vector2(80, 80), new Color(0.2f, 0.7f, 0.3f, 0.85f),
                OnJumpDown, OnJumpUp);

            // [🔄 RELOAD BUTTON]
            CreateTouchButton("TouchReloadBtn", "🔄 RELOAD", new Vector2(1, 0), new Vector2(-220, 195), new Vector2(75, 75), new Color(0.9f, 0.6f, 0.1f, 0.85f),
                OnReloadDown, OnReloadUp);

            // [✋ INTERACT / DRIVE BUTTON] - Center right for easy vehicle entering & item pickup
            CreateTouchButton("TouchInteractBtn", "✋ INTERACT / DRIVE", new Vector2(1, 0), new Vector2(-330, 130), new Vector2(120, 65), new Color(1f, 0.42f, 0.21f, 0.9f),
                OnInteractDown, null);

            Debug.Log("[MobileTouchControls] ✓ Built on-screen virtual joystick & touch buttons for iOS/Android.");
        }

        private void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trigger.triggers.Add(entry);
        }

        private void CreateTouchButton(string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color,
            UnityEngine.Events.UnityAction<BaseEventData> onDown, UnityEngine.Events.UnityAction<BaseEventData> onUp)
        {
            var btnObj = new GameObject(name);
            btnObj.transform.SetParent(transform, false);
            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = btnObj.AddComponent<Image>();
            img.color = color;

            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            var txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = label; txt.fontSize = size.x > 90 ? 16 : 13; txt.color = Color.white; txt.alignment = TMPro.TextAlignmentOptions.Center;

            var trigger = btnObj.AddComponent<EventTrigger>();
            if (onDown != null) AddTrigger(trigger, EventTriggerType.PointerDown, onDown);
            if (onUp != null) AddTrigger(trigger, EventTriggerType.PointerUp, onUp);
        }

        // ─── JOYSTICK HANDLERS ─────────────────────────────────────
        private void OnJoystickDown(BaseEventData data) { _isDraggingJoystick = true; OnJoystickDrag(data); }
        private void OnJoystickDrag(BaseEventData data)
        {
            if (!_isDraggingJoystick || !(data is PointerEventData ptr)) return;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_joystickBg, ptr.position, ptr.pressEventCamera, out localPos);
            localPos = Vector2.ClampMagnitude(localPos, 65f);
            _joystickKnob.anchoredPosition = localPos;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetMoveInput(localPos / 65f);
            }
        }
        private void OnJoystickUp(BaseEventData data)
        {
            _isDraggingJoystick = false;
            _joystickKnob.anchoredPosition = Vector2.zero;
            if (InputManager.Instance != null) InputManager.Instance.SetMoveInput(Vector2.zero);
        }

        // ─── CAMERA LOOK HANDLERS ──────────────────────────────────
        private void OnLookDown(BaseEventData data)
        {
            if (data is PointerEventData ptr) { _lookTouchId = ptr.pointerId; _lastLookTouchPos = ptr.position; }
        }
        private void OnLookDrag(BaseEventData data)
        {
            if (!(data is PointerEventData ptr) || ptr.pointerId != _lookTouchId) return;
            Vector2 delta = (ptr.position - _lastLookTouchPos) * 0.4f;
            _lastLookTouchPos = ptr.position;
            if (CameraManager.Instance != null) CameraManager.Instance.AddLookInput(delta);
        }
        private void OnLookUp(BaseEventData data) { if (data is PointerEventData ptr && ptr.pointerId == _lookTouchId) _lookTouchId = -1; }

        // ─── ACTION BUTTON HANDLERS ────────────────────────────────
        private void OnFireDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetFiring(true); }
        private void OnFireUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetFiring(false); }

        private void OnAimDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetAiming(true); }
        private void OnAimUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetAiming(false); }

        private void OnJumpDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetJumping(true); }
        private void OnJumpUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetJumping(false); }

        private void OnReloadDown(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetReloading(true); }
        private void OnReloadUp(BaseEventData d) { if (InputManager.Instance != null) InputManager.Instance.SetReloading(false); }

        private void OnInteractDown(BaseEventData d)
        {
            if (InputManager.Instance != null) InputManager.Instance.TriggerInteract();
        }
    }
}
