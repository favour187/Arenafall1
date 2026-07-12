using UnityEngine;
using UnityEngine.InputSystem;
using ArenaFall.Core;

namespace ArenaFall.Managers
{
    /// <summary>
    /// Manages input handling, action maps, and input profiles.
    /// Uses Unity's new Input System.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionAsset _inputActionAsset;

        private InputActionMap _gameplayMap;
        private InputActionMap _uiMap;
        private InputActionMap _menuMap;
        private string _currentMap = "Gameplay";

        public static InputManager Instance { get; private set; }

        // Exposed input values
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsJumping { get; private set; }
        public bool IsFiring { get; private set; }
        public bool IsAiming { get; private set; }
        public bool IsReloading { get; private set; }
        public float ScrollInput { get; private set; }
        public float Sensitivity { get; set; } = 5f;

        // Public Mobile & Touch Setters
        public void SetMoveInput(Vector2 input) { MoveInput = input; }
        public void SetLookInput(Vector2 input) { LookInput = input; }
        public void SetFiring(bool firing) { IsFiring = firing; }
        public void SetAiming(bool aiming) { IsAiming = aiming; }
        public void SetJumping(bool jumping) { IsJumping = jumping; }
        public void SetReloading(bool reloading) { IsReloading = reloading; }
        public void SetCrouching(bool crouching) { IsCrouching = crouching; }
        public void TriggerInteract() { TriggerInteractEvent(); }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<InputManager>(this);

            InitializeInput();
        }

        private void InitializeInput()
        {
            if (_inputActionAsset == null)
            {
                Debug.Log("[InputManager] No InputActionAsset assigned. Creating programmatic ActionAsset & enabling direct keyboard/mouse fallback...");
                _inputActionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
            }

            _gameplayMap = _inputActionAsset.FindActionMap("Gameplay");
            _uiMap = _inputActionAsset.FindActionMap("UI");
            _menuMap = _inputActionAsset.FindActionMap("Menu");

            if (_gameplayMap == null)
            {
                _gameplayMap = _inputActionAsset.AddActionMap("Gameplay");
                SetupDefaultGameplayActions(_gameplayMap);
            }

            // Bind all gameplay input events
            BindGameplayInput();
            _gameplayMap.Enable();
        }

        private void Update()
        {
            // Direct Programmatic Keyboard / Mouse & Legacy Input Fallback
            // Ensures movement, jumping, shooting, aiming, and interactions work smoothly without manual action map configuration!
            if (Keyboard.current != null)
            {
                float x = 0; float y = 0;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1;
                
                if (MoveInput.sqrMagnitude < 0.01f || (_gameplayMap == null))
                {
                    MoveInput = new Vector2(x, y).normalized;
                }

                if (Keyboard.current.spaceKey.isPressed) IsJumping = true;
                else if (_gameplayMap == null) IsJumping = false;

                if (Keyboard.current.leftShiftKey.isPressed) IsSprinting = true;
                else if (_gameplayMap == null) IsSprinting = false;

                if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame) IsCrouching = !IsCrouching;
                if (Keyboard.current.rKey.isPressed) IsReloading = true;
                else if (_gameplayMap == null) IsReloading = false;

                if (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                {
                    TriggerInteractEvent();
                }
            }
            else
            {
                if (MoveInput.sqrMagnitude < 0.01f)
                {
                    MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
                }
                if (Input.GetKey(KeyCode.Space)) IsJumping = true;
                if (Input.GetKey(KeyCode.LeftShift)) IsSprinting = true;
                if (Input.GetKeyDown(KeyCode.C)) IsCrouching = !IsCrouching;
                if (Input.GetKeyDown(KeyCode.R)) IsReloading = true;
                if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E)) TriggerInteractEvent();
            }

            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue() * (Sensitivity / 50f);
                if (delta.sqrMagnitude > 0.0001f || _gameplayMap == null) LookInput = delta;
                if (Mouse.current.leftButton.isPressed) IsFiring = true;
                else if (_gameplayMap == null) IsFiring = false;
                if (Mouse.current.rightButton.isPressed) IsAiming = true;
                else if (_gameplayMap == null) IsAiming = false;
            }
            else
            {
                LookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * Sensitivity;
                IsFiring = Input.GetMouseButton(0);
                IsAiming = Input.GetMouseButton(1);
            }
        }

        private void TriggerInteractEvent()
        {
            var playerObj = GameObject.Find("[AUTO] Player") ?? GameObject.Find("Player") ?? GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null && Camera.main != null) playerObj = Camera.main.gameObject;
            if (playerObj == null) return;

            // 1. Check if currently inside a vehicle to exit
            if (playerObj.transform.parent != null && playerObj.transform.parent.GetComponentInParent<Gameplay.Vehicles.VehicleController>() != null)
            {
                var currentVeh = playerObj.transform.parent.GetComponentInParent<Gameplay.Vehicles.VehicleController>();
                currentVeh.ExitVehicle(playerObj.transform);
                if (CameraManager.Instance != null) CameraManager.Instance.SetTarget(playerObj.transform);
                Debug.Log("[InputManager] Exited vehicle.");
                return;
            }

            // 2. Check nearby Vehicles to enter
            var vehicles = FindObjectsOfType<Gameplay.Vehicles.VehicleController>();
            foreach (var veh in vehicles)
            {
                if (Vector3.Distance(playerObj.transform.position, veh.transform.position) < 8f)
                {
                    if (veh.EnterVehicle(playerObj.transform))
                    {
                        if (CameraManager.Instance != null) CameraManager.Instance.SetTarget(veh.transform);
                        Debug.Log($"[InputManager] Entered vehicle: {veh.VehicleName}");
                        return;
                    }
                }
            }

            // 3. Check nearby Items/Weapons to pick up
            var interactables = FindObjectsOfType<MonoBehaviour>();
            foreach (var obj in interactables)
            {
                if (obj is Interfaces.IPickupable pickup && pickup.CanPickup)
                {
                    float dist = Vector3.Distance(playerObj.transform.position, ((MonoBehaviour)pickup).transform.position);
                    if (dist < 5f)
                    {
                        Debug.Log($"[InputManager] Picked up item: {pickup.PickupPrompt}");
                        Destroy(((MonoBehaviour)pickup).gameObject);
                        break;
                    }
                }
            }
        }

        private void SetupDefaultGameplayActions(InputActionMap map)
        {
            map.AddAction("Move", InputActionType.Value, "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d", "<Gamepad>/leftStick");
            map.AddAction("Look", InputActionType.PassThrough, "<Mouse>/delta", "<Gamepad>/rightStick");
            map.AddAction("Fire", InputActionType.Button, "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            map.AddAction("Aim", InputActionType.Button, "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            map.AddAction("Reload", InputActionType.Button, "<Keyboard>/r", "<Gamepad>/buttonEast");
            map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space", "<Gamepad>/buttonSouth");
            map.AddAction("Crouch", InputActionType.Button, "<Keyboard>/c", "<Keyboard>/leftCtrl", "<Gamepad>/buttonNorth");
            map.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            map.AddAction("Interact", InputActionType.Button, "<Keyboard>/f", "<Gamepad>/buttonWest");
            map.AddAction("Inventory", InputActionType.Button, "<Keyboard>/tab", "<Gamepad>/start");
            map.AddAction("Map", InputActionType.Button, "<Keyboard>/m", "<Gamepad>/select");
            map.AddAction("Ping", InputActionType.Button, "<Keyboard>/mouse2", "<Gamepad>/dpadUp");
            map.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll", "<Gamepad>/dpad");
            map.AddAction("Slot1", InputActionType.Button, "<Keyboard>/1");
            map.AddAction("Slot2", InputActionType.Button, "<Keyboard>/2");
            map.AddAction("Slot3", InputActionType.Button, "<Keyboard>/3");
            map.AddAction("Slot4", InputActionType.Button, "<Keyboard>/4");
            map.AddAction("Slot5", InputActionType.Button, "<Keyboard>/5");
            map.AddAction("UseHeal", InputActionType.Button, "<Keyboard>/4", "<Gamepad>/dpadLeft");
        }

        private void BindGameplayInput()
        {
            if (_gameplayMap == null) return;

            _gameplayMap["Move"].performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
            _gameplayMap["Move"].canceled += ctx => MoveInput = Vector2.zero;

            _gameplayMap["Look"].performed += ctx => LookInput = ctx.ReadValue<Vector2>() * (Sensitivity / 50f);
            _gameplayMap["Look"].canceled += ctx => LookInput = Vector2.zero;

            _gameplayMap["Fire"].performed += ctx => IsFiring = true;
            _gameplayMap["Fire"].canceled += ctx => IsFiring = false;

            _gameplayMap["Aim"].performed += ctx => IsAiming = true;
            _gameplayMap["Aim"].canceled += ctx => IsAiming = false;

            _gameplayMap["Sprint"].performed += ctx => IsSprinting = true;
            _gameplayMap["Sprint"].canceled += ctx => IsSprinting = false;

            _gameplayMap["Crouch"].performed += ctx => IsCrouching = !IsCrouching;

            _gameplayMap["Jump"].performed += ctx => IsJumping = true;
            _gameplayMap["Jump"].canceled += ctx => IsJumping = false;

            _gameplayMap["Reload"].performed += ctx => IsReloading = true;
            _gameplayMap["Reload"].canceled += ctx => IsReloading = false;

            _gameplayMap["Scroll"].performed += ctx => ScrollInput = ctx.ReadValue<float>();
            _gameplayMap["Scroll"].canceled += ctx => ScrollInput = 0f;
        }

        /// <summary>
        /// Switch to a specific input action map.
        /// </summary>
        public void SwitchToMap(string mapName)
        {
            _gameplayMap?.Disable();
            _uiMap?.Disable();
            _menuMap?.Disable();

            _currentMap = mapName;

            switch (mapName)
            {
                case "Gameplay":
                    _gameplayMap?.Enable();
                    break;
                case "UI":
                    _uiMap?.Enable();
                    break;
                case "Menu":
                    _menuMap?.Enable();
                    break;
            }
        }

        /// <summary>
        /// Enable or disable all input.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            if (enabled)
                _gameplayMap?.Enable();
            else
                _gameplayMap?.Disable();
        }

        /// <summary>
        /// Update sensitivity value.
        /// </summary>
        public void UpdateSensitivity(float newSensitivity)
        {
            Sensitivity = Mathf.Clamp(newSensitivity, 0.1f, 20f);
        }

        private void OnDestroy()
        {
            if (_gameplayMap != null)
            {
                _gameplayMap["Move"].performed -= ctx => MoveInput = Vector2.zero;
                _gameplayMap["Look"].performed -= ctx => LookInput = Vector2.zero;
            }
        }
    }
}
