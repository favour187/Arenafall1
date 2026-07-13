using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ArenaFall.Core;
using ArenaFall.Data;
using ArenaFall.Events;
using ArenaFall.Interfaces;
using ArenaFall.Gameplay.Characters;

namespace ArenaFall.Gameplay.Inventory
{
    /// <summary>
    /// Manages character inventory - weapons, items, ammo, and equipment.
    /// Supports slot-based inventory with stackable items.
    /// </summary>
    public class Inventory : MonoBehaviour, IInventory
    {
        [Header("Configuration")]
        [SerializeField] private int _maxWeaponSlots = 2;
        [SerializeField] private int _maxItemSlots = 6;
        [SerializeField] private int _maxBackpackSlots = 6;
        [SerializeField] private bool _useBackpack = false;

        [Header("Starting Items")]
        [SerializeField] private ItemData _startingWeapon;

        // Inventory data
        private List<InventorySlot> _weaponSlots = new();
        private List<InventorySlot> _itemSlots = new();
        private int _currentWeaponIndex = -1;
        private int _totalSlots;
        private bool _initialized;

        // Events
        public System.Action OnInventoryChanged;

        // Properties
        public IReadOnlyList<IItem> Items => _itemSlots.Select(s => s.Item).ToList();
        public int TotalSlots => _totalSlots;
        public int UsedSlots => _itemSlots.Count(s => s.HasItem);
        public int FreeSlots => _totalSlots - UsedSlots;
        public bool IsFull => FreeSlots <= 0;
        public int MaxWeaponSlots => _maxWeaponSlots;
        public int CurrentWeaponIndex => _currentWeaponIndex;

        private void Awake()
        {
            InitializeSlots();
        }

        private void Start()
        {
            if (_startingWeapon != null)
            {
                GiveStartingWeapon();
            }
        }

        private void InitializeSlots()
        {
            int itemSlotCount = _useBackpack ? _maxItemSlots + _maxBackpackSlots : _maxItemSlots;
            _totalSlots = itemSlotCount;
            
            _weaponSlots.Clear();
            _itemSlots.Clear();

            for (int i = 0; i < _maxWeaponSlots; i++)
            {
                _weaponSlots.Add(new InventorySlot(SlotType.Weapon, i));
            }

            for (int i = 0; i < itemSlotCount; i++)
            {
                _itemSlots.Add(new InventorySlot(SlotType.Item, i));
            }
        }

        private void GiveStartingWeapon()
        {
            var weaponItem = CreateItemFromData(_startingWeapon);
            if (weaponItem != null)
            {
                AddItem(weaponItem);
            }
        }

        /// <summary>
        /// Add an item to the inventory.
        /// </summary>
        public bool AddItem(IItem item)
        {
            if (item == null) return false;

            // Handle stackable items
            if (item.Data.canStack)
            {
                var existingSlot = _itemSlots.Find(s => 
                    s.HasItem && s.Item.ItemId == item.ItemId && s.Item.StackCount < s.Item.MaxStack);
                
                if (existingSlot != null)
                {
                    int added = existingSlot.Item.AddToStack(item.StackCount);
                    item.RemoveFromStack(item.StackCount - added);
                    OnInventoryChanged?.Invoke();
                    EventBus.Raise(new InventoryChangedEvent
                    {
                        PlayerId = gameObject.GetInstanceID().ToString(),
                        ItemId = item.ItemId,
                        ChangeType = InventoryChangeType.Added,
                        Amount = added
                    });
                    return true;
                }
            }

            // Find empty slot
            if (item.Category == ItemCategory.Weapon)
            {
                var emptyWeaponSlot = _weaponSlots.Find(s => !s.HasItem);
                if (emptyWeaponSlot != null)
                {
                    emptyWeaponSlot.SetItem(item);
                    _currentWeaponIndex = emptyWeaponSlot.Index;
                    OnInventoryChanged?.Invoke();
                    EventBus.Raise(new InventoryChangedEvent
                    {
                        PlayerId = gameObject.GetInstanceID().ToString(),
                        ItemId = item.ItemId,
                        ChangeType = InventoryChangeType.Added,
                        Amount = 1
                    });
                    return true;
                }
                // Try swapping
                if (_weaponSlots.Count > 0 && item is WeaponItem)
                {
                    return ReplaceWeapon(item as WeaponItem);
                }
                return false;
            }
            else
            {
                var emptySlot = _itemSlots.Find(s => !s.HasItem);
                if (emptySlot != null)
                {
                    emptySlot.SetItem(item);
                    OnInventoryChanged?.Invoke();
                    EventBus.Raise(new InventoryChangedEvent
                    {
                        PlayerId = gameObject.GetInstanceID().ToString(),
                        ItemId = item.ItemId,
                        ChangeType = InventoryChangeType.Added,
                        Amount = 1
                    });
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove an item from inventory.
        /// </summary>
        public bool RemoveItem(string itemId, int amount = 1)
        {
            // Check weapon slots
            foreach (var slot in _weaponSlots)
            {
                if (slot.HasItem && slot.Item.ItemId == itemId)
                {
                    slot.Clear();
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            // Check item slots
            foreach (var slot in _itemSlots)
            {
                if (slot.HasItem && slot.Item.ItemId == itemId)
                {
                    int remaining = slot.Item.RemoveFromStack(amount);
                    if (remaining <= 0)
                    {
                        slot.Clear();
                    }
                    OnInventoryChanged?.Invoke();
                    EventBus.Raise(new InventoryChangedEvent
                    {
                        PlayerId = gameObject.GetInstanceID().ToString(),
                        ItemId = itemId,
                        ChangeType = InventoryChangeType.Removed,
                        Amount = amount
                    });
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Replace a weapon slot with a new weapon (drops the old one).
        /// </summary>
        public bool ReplaceWeapon(WeaponItem newWeapon)
        {
            if (newWeapon == null) return false;

            int replaceIndex = _currentWeaponIndex >= 0 ? _currentWeaponIndex : 0;
            if (replaceIndex < _weaponSlots.Count)
            {
                // Drop existing weapon
                var oldItem = _weaponSlots[replaceIndex].Item;
                if (oldItem != null)
                {
                    oldItem.Drop(transform.position + transform.forward * 2f);
                }

                _weaponSlots[replaceIndex].SetItem(newWeapon);
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if an item exists in inventory.
        /// </summary>
        public bool HasItem(string itemId)
        {
            return _weaponSlots.Any(s => s.HasItem && s.Item.ItemId == itemId) ||
                   _itemSlots.Any(s => s.HasItem && s.Item.ItemId == itemId);
        }

        /// <summary>
        /// Get count of a specific item.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            int count = 0;
            foreach (var slot in _itemSlots)
            {
                if (slot.HasItem && slot.Item.ItemId == itemId)
                {
                    count += slot.Item.StackCount;
                }
            }
            return count;
        }

        /// <summary>
        /// Get the current weapon item.
        /// </summary>
        public WeaponItem GetCurrentWeapon()
        {
            if (_currentWeaponIndex >= 0 && _currentWeaponIndex < _weaponSlots.Count)
            {
                return _weaponSlots[_currentWeaponIndex].Item as WeaponItem;
            }
            return null;
        }

        /// <summary>
        /// Switch to a weapon slot.
        /// </summary>
        public void SwitchWeapon(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _weaponSlots.Count) return;
            if (!_weaponSlots[slotIndex].HasItem) return;

            _currentWeaponIndex = slotIndex;
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Use an item from the inventory.
        /// </summary>
        public bool UseItem(string itemId)
        {
            foreach (var slot in _itemSlots)
            {
                if (slot.HasItem && slot.Item.ItemId == itemId)
                {
                    bool used = slot.Item.Use(gameObject);
                    if (used && slot.Item.Data.consumeOnUse)
                    {
                        int remaining = slot.Item.RemoveFromStack(1);
                        if (remaining <= 0) slot.Clear();
                    }
                    OnInventoryChanged?.Invoke();
                    return used;
                }
            }
            return false;
        }

        /// <summary>
        /// Clear all items from inventory.
        /// </summary>
        public void Clear()
        {
            foreach (var slot in _weaponSlots) slot.Clear();
            foreach (var slot in _itemSlots) slot.Clear();
            _currentWeaponIndex = -1;
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Set backpack tier for additional slots.
        /// </summary>
        public void SetBackpackTier(int tier)
        {
            _useBackpack = true;
            _maxBackpackSlots = tier switch
            {
                1 => 4,
                2 => 8,
                3 => 12,
                _ => 0
            };
            InitializeSlots();
        }

        /// <summary>
        /// Get all items in a category.
        /// </summary>
        public List<IItem> GetItemsInCategory(ItemCategory category)
        {
            var items = new List<IItem>();
            foreach (var slot in _itemSlots)
            {
                if (slot.HasItem && slot.Item.Category == category)
                {
                    items.Add(slot.Item);
                }
            }
            return items;
        }

        /// <summary>
        /// Create an item instance from data.
        /// </summary>
        private IItem CreateItemFromData(ItemData data)
        {
            if (data == null) return null;

            if (data is WeaponData weaponData)
            {
                return new WeaponItem(weaponData);
            }
            else if (data is HealingItemData healingData)
            {
                return new HealingItem(healingData);
            }
            else
            {
                return new GenericItem(data);
            }
        }

        /// <summary>
        /// Check if the current weapon can be holstered/switched.
        /// </summary>
        public bool CanSwitchWeapon()
        {
            return _weaponSlots.Count(s => s.HasItem) > 1;
        }
    }

    /// <summary>
    /// Represents a single inventory slot.
    /// </summary>
    [System.Serializable]
    public class InventorySlot
    {
        public SlotType Type;
        public int Index;
        public bool HasItem => Item != null;

        public IItem Item { get; private set; }

        public InventorySlot(SlotType type, int index)
        {
            Type = type;
            Index = index;
        }

        public void SetItem(IItem item) => Item = item;
        public void Clear() => Item = null;
    }

    public enum SlotType
    {
        Weapon,
        Item,
        Armor,
        Backpack
    }

    // ── Concrete Item Implementations ──

    public class GenericItem : IItem
    {
        public string ItemId => Data.itemId;
        public string ItemName => Data.itemName;
        public ItemData Data { get; private set; }
        public int StackCount { get; private set; } = 1;
        public int MaxStack => Data.maxStack;
        public ItemCategory Category => Data.category;
        public ItemRarity Rarity => Data.rarity;
        public Sprite Icon => Data.icon;

        public GenericItem(ItemData data, int stackCount = 1)
        {
            Data = data;
            StackCount = stackCount;
        }

        public bool Use(GameObject user) => true;
        public void Pickup(GameObject picker) { }
        public void Drop(Vector3 position) { /* Instantiate world prefab */ }
        public int AddToStack(int amount) { StackCount = Mathf.Min(StackCount + amount, MaxStack); return amount; }
        public int RemoveFromStack(int amount) { StackCount -= amount; return Mathf.Max(0, StackCount); }
    }

    public class WeaponItem : IItem
    {
        public string ItemId => WeaponData.weaponId;
        public string ItemName => WeaponData.weaponName;
        public ItemData Data => WeaponData;
        public WeaponData WeaponData { get; private set; }
        public int StackCount { get; private set; } = 1;
        public int MaxStack => 1;
        public ItemCategory Category => ItemCategory.Weapon;
        public ItemRarity Rarity => WeaponData.rarity;
        public Sprite Icon => WeaponData.icon;

        public WeaponItem(WeaponData data) { WeaponData = data; }

        public bool Use(GameObject user) => false; // Weapons are equipped, not used
        public void Pickup(GameObject picker) { }
        public void Drop(Vector3 position)
        {
            if (WeaponData.pickupPrefab != null)
            {
                Object.Instantiate(WeaponData.pickupPrefab, position, Quaternion.identity);
            }
        }
        public int AddToStack(int amount) { return 0; } // Weapons don't stack
        public int RemoveFromStack(int amount) { StackCount -= amount; return Mathf.Max(0, StackCount); }
    }

    public class HealingItem : IItem
    {
        public string ItemId => Data.itemId;
        public string ItemName => Data.itemName;
        public ItemData Data => HealingData;
        public HealingItemData HealingData { get; private set; }
        public int StackCount { get; private set; } = 1;
        public int MaxStack => HealingData.maxStack;
        public ItemCategory Category => ItemCategory.Healing;
        public ItemRarity Rarity => HealingData.rarity;
        public Sprite Icon => HealingData.icon;

        public HealingItem(HealingItemData data, int stackCount = 1)
        {
            HealingData = data;
            StackCount = stackCount;
        }

        public bool Use(GameObject user)
        {
            var health = user.GetComponent<CharacterHealth>();
            if (health != null)
            {
                health.Heal(HealingData.healthRestoreAmount);
                if (HealingData.shieldRestoreAmount > 0)
                {
                    health.ApplyShield(HealingData.shieldRestoreAmount);
                }
                return true;
            }
            return false;
        }

        public void Pickup(GameObject picker) { }
        public void Drop(Vector3 position) { }
        public int AddToStack(int amount) { StackCount = Mathf.Min(StackCount + amount, MaxStack); return amount; }
        public int RemoveFromStack(int amount) { StackCount -= amount; return Mathf.Max(0, StackCount); }
    }
}
