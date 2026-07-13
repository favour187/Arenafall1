using UnityEngine;
using ArenaFall.Interfaces;

namespace ArenaFall.Data
{
    /// <summary>
    /// ScriptableObject data container for weapon configuration.
    /// All weapon stats are defined here and referenced by weapon controllers.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Arena Fall/Weapons/Weapon Data")]
    public class WeaponData : ItemData
    {
        [Header("Weapon General")]
        public string weaponId;
        public string weaponName;
        [TextArea] public new string description;
        public WeaponCategory weaponCategory;
        public new ItemRarity rarity;
        public new Sprite icon;
        public GameObject weaponPrefab;
        public new GameObject pickupPrefab;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(itemId)) itemId = weaponId;
            if (string.IsNullOrEmpty(itemName)) itemName = weaponName;
            category = ItemCategory.Weapon;
        }

        [Header("Damage")]
        public float baseDamage = 30f;
        public float headshotMultiplier = 2.0f;
        public float limbMultiplier = 0.75f;
        public float damageFalloffStart = 50f;
        public float damageFalloffEnd = 200f;
        public float damageFalloffMin = 0.5f;

        [Header("Fire Rate & Ammo")]
        public float fireRate = 600f; // RPM
        public int magazineSize = 30;
        public int maxReserveAmmo = 120;
        public float reloadTime = 2.5f;
        public float reloadEmptyTime = 3.2f;
        public FireMode[] availableFireModes = new FireMode[] { FireMode.Auto };

        [Header("Accuracy & Recoil")]
        public float baseAccuracy = 0.95f;
        public float accuracySpreadPerShot = 0.02f;
        public float accuracyRecoveryRate = 0.1f;
        public float hipFireSpread = 0.1f;
        public float adsSpreadMultiplier = 0.5f;
        public Vector2 recoilKick = new Vector2(1f, 1.5f);
        public float recoilRecovery = 0.5f;

        [Header("Range")]
        public float effectiveRange = 300f;
        public float maxRange = 500f;
        public float bulletSpeed = 500f;

        [Header("Aiming")]
        public float aimDownSightsSpeed = 0.3f;
        public float adsFieldOfView = 55f;
        public float aimSensitivityMultiplier = 0.7f;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip fireSuppressedSound;
        public AudioClip reloadSound;
        public AudioClip reloadEmptySound;
        public AudioClip equipSound;
        public AudioClip holsterSound;

        [Header("Visuals")]
        public GameObject muzzleFlashPrefab;
        public GameObject bulletImpactPrefab;
        public TrailRenderer bulletTrailPrefab;
        public Vector3 weaponPositionOffset;
        public Vector3 weaponRotationOffset;
        public Vector3 adsPositionOffset;
        public Vector3 adsRotationOffset;

        [Header("Animation")]
        public AnimationClip fireAnimation;
        public AnimationClip reloadAnimation;
        public AnimationClip reloadEmptyAnimation;
        public AnimationClip equipAnimation;
        public AnimationClip holsterAnimation;

        [Header("Attachments")]
        public AttachmentSlot[] allowedAttachments;

        public float GetFireRatePerSecond() => fireRate / 60f;
        public float GetFireInterval() => 60f / fireRate;
    }

    public enum WeaponCategory
    {
        AssaultRifle,
        SubmachineGun,
        Shotgun,
        SniperRifle,
        LightMachineGun,
        Pistol,
        Melee,
        Throwable
    }

    [System.Serializable]
    public class AttachmentSlot
    {
        public AttachmentSlotType slotType;
        public AttachmentData[] compatibleAttachments;
    }

    public enum AttachmentSlotType
    {
        Sight,
        Muzzle,
        Grip,
        Magazine,
        Barrel,
        Stock
    }
}
