using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManagerWeaponEnemy : MonoBehaviour
{
    public static ManagerWeaponEnemy Instance { get; private set; }

    private const int MaxWeaponSlots = 3;

    [Header("Lista de armas")]
    [Tooltip("La misma WeaponList utilizada por el jugador.")]
    [SerializeField] private WeaponList weaponList;

    [Header("Weapon Holders del enemigo")]
    [Tooltip("Los 3 lugares donde aparecerán las armas del enemigo.")]
    [SerializeField]
    private Transform[] weaponHolders =
        new Transform[MaxWeaponSlots];

    [Header("Ranuras visuales del enemigo")]
    [Tooltip("Las 3 imágenes donde aparecerán los iconos.")]
    [SerializeField]
    private Image[] weaponSlotImages =
        new Image[MaxWeaponSlots];

    // Control de auto-equip
    [Header("Auto equipamiento (enemigo)")]
    [Tooltip("Si está activo, al iniciar el manager escogera aleatoriamente armas para el enemigo.")]
    [SerializeField] private bool autoEquipOnAwake = true;
    [Tooltip("Numero máximo de IDs a escanear si WeaponList no expone una lista directa.")]
    [SerializeField] private int maxScanWeaponID = 64;

    private readonly int[] equippedWeaponIDs =
        new int[MaxWeaponSlots];

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

        InitializeWeaponSlots();

        if (autoEquipOnAwake)
        {
            RandomizeAndEquipWeapons();
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
        if (weaponList == null)
        {
            Debug.LogError(
                "ManagerWeaponEnemy necesita una WeaponList.",
                this
            );
        }

        if (weaponHolders == null ||
            weaponHolders.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                $"ManagerWeaponEnemy necesita exactamente " +
                $"{MaxWeaponSlots} Weapon Holders.",
                this
            );
        }

        if (weaponSlotImages == null ||
            weaponSlotImages.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                $"ManagerWeaponEnemy necesita exactamente " +
                $"{MaxWeaponSlots} imágenes de UI.",
                this
            );
        }
    }

    /// <summary>
    /// Equipa o elimina un arma del enemigo.
    /// </summary>
    public void ToggleWeapon(int weaponID)
    {
        if (!IsValidWeaponID(weaponID))
        {
            Debug.LogWarning(
                $"El arma enemiga con ID {weaponID} no existe.",
                this
            );

            return;
        }

        int currentSlot = GetWeaponSlot(weaponID);

        if (currentSlot != -1)
        {
            RemoveWeaponFromSlot(currentSlot);
            return;
        }

        int emptySlot = GetFirstEmptySlot();

        if (emptySlot == -1)
        {
            Debug.Log(
                "Los tres puestos de armas del enemigo están ocupados.",
                this
            );

            return;
        }

        EquipWeaponInSlot(weaponID, emptySlot);
    }

    private void EquipWeaponInSlot(
        int weaponID,
        int slotIndex
    )
    {
        if (!IsValidSlot(slotIndex))
            return;

        Transform holder = GetWeaponHolder(slotIndex);

        if (holder == null)
        {
            Debug.LogError(
                $"No hay un Weapon Holder asignado " +
                $"a la ranura {slotIndex} del enemigo.",
                this
            );

            return;
        }

        GameObject weaponPrefab =
            weaponList.GetWeaponPrefab(weaponID);

        if (weaponPrefab == null)
        {
            Debug.LogError(
                $"El arma enemiga {weaponID} no tiene " +
                "un prefab asignado.",
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
        equippedWeaponIDs[slotIndex] = weaponID;

        UpdateWeaponSlotUI(slotIndex, weaponID);
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
        int weaponID
    )
    {
        Image slotImage = GetWeaponSlotImage(slotIndex);

        if (slotImage == null)
            return;

        Sprite weaponIcon =
            weaponList.GetWeaponIcon(weaponID);

        if (weaponIcon == null)
        {
            Debug.LogWarning(
                $"El arma enemiga {weaponID} " +
                "no tiene un icono válido.",
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

    private bool IsValidWeaponID(int weaponID)
    {
        return weaponList != null &&
               weaponList.IsValidWeaponID(weaponID);
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < MaxWeaponSlots;
    }

    // ------------------- Nuevo: auto-equip aleatorio -------------------

    // Obtiene la lista de weaponIDs válidos a partir de la WeaponList.
    // Si WeaponList expone un método público GetAllWeaponIDs lo usará;
    // en caso contrario escaneará IDs desde 0 hasta maxScanWeaponID y recogerá
    // aquellos para los que IsValidWeaponID devuelve true.
    private List<int> GetAvailableWeaponIDs()
    {
        List<int> result = new List<int>();

        if (weaponList == null)
            return result;

        // Intentar llamada directa si WeaponList tiene un método GetAllWeaponIDs()
        var method = weaponList.GetType().GetMethod("GetAllWeaponIDs");
        if (method != null)
        {
            var obj = method.Invoke(weaponList, null);
            if (obj is IEnumerable<int> ids)
            {
                foreach (int id in ids) result.Add(id);
                return result;
            }
        }

        // Fallback: escaneo por rango configurable
        for (int id = 0; id < maxScanWeaponID; id++)
        {
            if (IsValidWeaponID(id))
                result.Add(id);
        }

        return result;
    }

    // Elige aleatoriamente cuántas armas (1..MaxWeaponSlots) y qué armas,
    // y las equipa en ranuras aleatorias.
    private void RandomizeAndEquipWeapons()
    {
        ClearAllWeapons();

        List<int> available = GetAvailableWeaponIDs();

        if (available.Count == 0)
        {
            Debug.LogWarning("ManagerWeaponEnemy: no hay armas disponibles en WeaponList.", this);
            return;
        }

        // Numero de armas aleatorio entre 1 y MaxWeaponSlots
        int n = Random.Range(1, MaxWeaponSlots + 1);

        // Elegir n IDs sin repetición
        List<int> chosenIDs = new List<int>();
        List<int> pool = new List<int>(available);
        for (int i = 0; i < n && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            chosenIDs.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        // Elegir ranuras aleatorias para colocar las armas
        List<int> slotPool = new List<int> { 0, 1, 2 };
        for (int i = 0; i < chosenIDs.Count; i++)
        {
            if (slotPool.Count == 0) break;
            int sidx = Random.Range(0, slotPool.Count);
            int slot = slotPool[sidx];
            slotPool.RemoveAt(sidx);

            EquipWeaponInSlot(chosenIDs[i], slot);
        }

        Debug.Log($"ManagerWeaponEnemy: equipado aleatoriamente {chosenIDs.Count} arma(s).", this);
    }
}