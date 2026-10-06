using UnityEngine;
using UnityEngine.UI;

public class ManagerWeapon : MonoBehaviour
{
    public static ManagerWeapon Instance { get; private set; }

    private const int MaxWeaponSlots = 3;

    [Header("Datos de armas")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Weapon Holders del jugador")]
    [Tooltip("Los 3 lugares donde aparecerán las armas del jugador.")]
    [SerializeField]
    private Transform[] weaponHolders =
        new Transform[MaxWeaponSlots];

    [Header("Ranuras visuales del jugador")]
    [Tooltip("Las 3 imágenes donde aparecerán los iconos.")]
    [SerializeField]
    private Image[] weaponSlotImages =
        new Image[MaxWeaponSlots];

    

    // -1 significa que la ranura está vacía.
    private readonly int[] equippedWeaponIDs =
        new int[MaxWeaponSlots];

    // Instancias físicas de las armas.
    private readonly GameObject[] weaponInstances =
        new GameObject[MaxWeaponSlots];

    public int WeaponSlotCount => MaxWeaponSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        weaponManager = WeaponManager.Instance;

        InitializeWeaponSlots();
    }

    private void Start()
    {
        weaponManager = WeaponManager.Instance;

        if (weaponManager == null)
        {
            Debug.LogError(
                "No existe un WeaponManager persistente.",
                this
            );
        }

        
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeWeaponSlots()
    {
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            equippedWeaponIDs[i] = -1;
            weaponInstances[i] = null;

            ClearWeaponSlotUI(i);
        }

        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (weaponManager == null)
        {
            Debug.LogError(
                "ManagerWeapon necesita una referencia a WeaponManager.",
                this
            );
        }

        if (weaponHolders == null ||
            weaponHolders.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                $"ManagerWeapon necesita exactamente " +
                $"{MaxWeaponSlots} Weapon Holders.",
                this
            );
        }

        if (weaponSlotImages == null ||
            weaponSlotImages.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                $"ManagerWeapon necesita exactamente " +
                $"{MaxWeaponSlots} imágenes de UI.",
                this
            );
        }
    }

    /// <summary>
    /// Equipa o elimina un arma del jugador.
    /// </summary>
    public void ToggleWeapon(int weaponID)
    {
        if (weaponManager == null)
        {
            Debug.LogError(
                "ManagerWeapon no tiene asignado un WeaponManager.",
                this
            );

            return;
        }

        WeaponManager.WeaponRuntimeData weapon =
            weaponManager.GetWeapon(weaponID);

        if (weapon == null)
        {
            Debug.LogWarning(
                $"El arma con ID {weaponID} no existe.",
                this
            );

            return;
        }

        // Opcional, pero recomendable:
        // no permitir equipar armas bloqueadas por rango.
        if (!weapon.Activo)
        {
            Debug.LogWarning(
                $"El arma {weapon.WeaponName} no está activa. " +
                $"Requiere rango {weapon.Rank}.",
                this
            );

            return;
        }

        // No permitir equipar armas que todavía no fueron compradas.
        if (!weapon.Comprado)
        {
            Debug.LogWarning(
                $"El arma {weapon.WeaponName} todavía no está comprada.",
                this
            );

            return;
        }

        int currentSlot = GetWeaponSlot(weaponID);

        // Si ya está equipada, la quitamos.
        if (currentSlot != -1)
        {
            RemoveWeaponFromSlot(currentSlot);
            return;
        }

        int emptySlot = GetFirstEmptySlot();

        if (emptySlot == -1)
        {
            Debug.Log(
                "Los tres puestos de armas del jugador están ocupados.",
                this
            );

            return;
        }

        EquipWeaponInSlot(weapon, emptySlot);
    }

    private void EquipWeaponInSlot(
    WeaponManager.WeaponRuntimeData weapon,
    int slotIndex
)
    {
        if (weapon == null)
            return;

        if (!IsValidSlot(slotIndex))
            return;

        Transform holder = GetWeaponHolder(slotIndex);

        if (holder == null)
        {
            Debug.LogError(
                $"No hay un Weapon Holder asignado " +
                $"a la ranura {slotIndex} del jugador.",
                this
            );

            return;
        }

        GameObject weaponPrefab = weapon.WeaponPrefab;

        if (weaponPrefab == null)
        {
            Debug.LogError(
                $"El arma {weapon.WeaponName} " +
                $"con ID {weapon.WeaponID} no tiene prefab.",
                this
            );

            return;
        }

        GameObject newWeapon = Instantiate(
            weaponPrefab,
            holder,
            false
        );

        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        weaponInstances[slotIndex] = newWeapon;
        equippedWeaponIDs[slotIndex] = weapon.WeaponID;

        UpdateWeaponSlotUI(
            slotIndex,
            weapon.WeaponIcon
        );
    }

    public void RemoveWeaponFromSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (equippedWeaponIDs[slotIndex] == -1)
            return;

        if (weaponInstances[slotIndex] != null)
        {
            Destroy(weaponInstances[slotIndex]);
        }

        weaponInstances[slotIndex] = null;
        equippedWeaponIDs[slotIndex] = -1;

        ClearWeaponSlotUI(slotIndex);
    }

    private void UpdateWeaponSlotUI(
    int slotIndex,
    Sprite weaponIcon
)
    {
        Image slotImage = GetWeaponSlotImage(slotIndex);

        if (slotImage == null)
            return;

        if (weaponIcon == null)
        {
            Debug.LogWarning(
                $"La ranura {slotIndex} recibió un arma sin icono.",
                this
            );

            ClearWeaponSlotUI(slotIndex);
            return;
        }

        slotImage.sprite = weaponIcon;
        slotImage.preserveAspect = true;
        slotImage.enabled = true;
    }

    private void ClearWeaponSlotUI(int slotIndex)
    {
        Image slotImage = GetWeaponSlotImage(slotIndex);

        if (slotImage == null)
            return;

        slotImage.sprite = null;
        slotImage.enabled = false;
    }

    public void ClearAllWeapons()
    {
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            RemoveWeaponFromSlot(i);
        }
    }

    public int GetWeaponSlot(int weaponID)
    {
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            if (equippedWeaponIDs[i] == weaponID)
            {
                return i;
            }
        }

        return -1;
    }

    public int GetEquippedWeapon(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return -1;

        return equippedWeaponIDs[slotIndex];
    }

    public bool IsWeaponEquipped(int weaponID)
    {
        return GetWeaponSlot(weaponID) != -1;
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return true;

        return equippedWeaponIDs[slotIndex] == -1;
    }

    public int[] GetEquippedWeapons()
    {
        return (int[])equippedWeaponIDs.Clone();
    }

    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            if (equippedWeaponIDs[i] == -1)
            {
                return i;
            }
        }

        return -1;
    }

    private Transform GetWeaponHolder(int slotIndex)
    {
        if (weaponHolders == null ||
            slotIndex < 0 ||
            slotIndex >= weaponHolders.Length)
        {
            return null;
        }

        return weaponHolders[slotIndex];
    }

    private Image GetWeaponSlotImage(int slotIndex)
    {
        if (weaponSlotImages == null ||
            slotIndex < 0 ||
            slotIndex >= weaponSlotImages.Length)
        {
            return null;
        }

        return weaponSlotImages[slotIndex];
    }
    
    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < MaxWeaponSlots;
    }

    
}