
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManagerWeaponEnemy : MonoBehaviour
{
    public static ManagerWeaponEnemy Instance { get; private set; }

    private const int MaxWeaponSlots = 3;

    [Header("Lista de armas")]
    [SerializeField] private WeaponList weaponList;

    [Header("Weapon Holders del enemigo")]
    [SerializeField]
    private Transform[] weaponHolders =
        new Transform[MaxWeaponSlots];

    [Header("Ranuras visuales del enemigo")]
    [SerializeField]
    private Image[] weaponSlotImages =
        new Image[MaxWeaponSlots];

    [Header("Auto equipamiento")]
    [SerializeField] private bool autoEquipOnAwake = true;
    [SerializeField] private int maxScanWeaponID = 64;

    private readonly int[] equippedWeaponIDs =
        new int[MaxWeaponSlots];

    private readonly GameObject[] weaponInstances =
        new GameObject[MaxWeaponSlots];

    public int WeaponSlotCount => MaxWeaponSlots;

    public int EquippedWeaponCount
    {
        get
        {
            int cantidad = 0;

            for (int i = 0; i < MaxWeaponSlots; i++)
            {
                if (weaponInstances[i] != null)
                    cantidad++;
            }

            return cantidad;
        }
    }

    private void Awake()
    {
        // Cada enemigo debe tener su propio administrador.
        // No se utiliza el Singleton para evitar compartir referencias.
        InitializeWeaponSlots();

        if (autoEquipOnAwake)
            RandomizeAndEquipWeapons();
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
            Debug.LogError("ManagerWeaponEnemy necesita una WeaponList.", this);

        if (weaponHolders == null ||
            weaponHolders.Length != MaxWeaponSlots)
        {
            Debug.LogError("Se necesitan exactamente 3 Weapon Holders.", this);
        }

        if (weaponSlotImages == null ||
            weaponSlotImages.Length != MaxWeaponSlots)
        {
            Debug.LogError("Se necesitan exactamente 3 imágenes de UI.", this);
        }
    }

    public void ToggleWeapon(int weaponID)
    {
        if (!IsValidWeaponID(weaponID))
        {
            Debug.LogWarning($"El arma con ID {weaponID} no existe.", this);
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
            Debug.Log("Los tres slots están ocupados.", this);
            return;
        }

        EquipWeaponInSlot(weaponID, emptySlot);
    }

    private void EquipWeaponInSlot(int weaponID, int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        Transform holder = GetWeaponHolder(slotIndex);

        if (holder == null)
        {
            Debug.LogError($"No hay Weapon Holder en el slot {slotIndex}.", this);
            return;
        }

        GameObject weaponPrefab = weaponList.GetWeaponPrefab(weaponID);

        if (weaponPrefab == null)
        {
            Debug.LogError($"El arma {weaponID} no tiene prefab.", this);
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

    public GameObject GetWeaponInstance(int slot)
    {
        if (!IsValidSlot(slot))
            return null;

        return weaponInstances[slot];
    }

    // ÚNICO método GetWeaponHolder.
    // Es público para que EnemyWeaponAnalysis pueda consultarlo.
    public Transform GetWeaponHolder(int slotIndex)
    {
        if (weaponHolders == null ||
            slotIndex < 0 ||
            slotIndex >= weaponHolders.Length)
        {
            return null;
        }

        return weaponHolders[slotIndex];
    }

    public void RemoveWeaponFromSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (equippedWeaponIDs[slotIndex] == -1)
            return;

        if (weaponInstances[slotIndex] != null)
            Destroy(weaponInstances[slotIndex]);

        weaponInstances[slotIndex] = null;
        equippedWeaponIDs[slotIndex] = -1;

        ClearWeaponSlotUI(slotIndex);
    }

    public void ClearAllWeapons()
    {
        for (int i = 0; i < MaxWeaponSlots; i++)
            RemoveWeaponFromSlot(i);
    }

    public int GetWeaponSlot(int weaponID)
    {
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            if (equippedWeaponIDs[i] == weaponID)
                return i;
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
                return i;
        }

        return -1;
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

    private void UpdateWeaponSlotUI(int slotIndex, int weaponID)
    {
        Image slotImage = GetWeaponSlotImage(slotIndex);

        if (slotImage == null)
            return;

        Sprite weaponIcon = weaponList.GetWeaponIcon(weaponID);

        if (weaponIcon == null)
        {
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

    private List<int> GetAvailableWeaponIDs()
    {
        List<int> result = new List<int>();

        if (weaponList == null)
            return result;

        var method = weaponList.GetType().GetMethod("GetAllWeaponIDs");

        if (method != null)
        {
            var obj = method.Invoke(weaponList, null);

            if (obj is IEnumerable<int> ids)
            {
                foreach (int id in ids)
                {
                    if (IsValidWeaponID(id) && !result.Contains(id))
                        result.Add(id);
                }

                return result;
            }
        }

        for (int id = 0; id < maxScanWeaponID; id++)
        {
            if (IsValidWeaponID(id))
                result.Add(id);
        }

        return result;
    }

    private void RandomizeAndEquipWeapons()
    {
        ClearAllWeapons();

        List<int> available = GetAvailableWeaponIDs();

        if (available.Count == 0)
        {
            Debug.LogWarning("No hay armas disponibles en WeaponList.", this);
            return;
        }

        int cantidad = Random.Range(1, MaxWeaponSlots + 1);

        // Elegir tipos de armas sin repetir.
        for (int i = 0; i < cantidad && available.Count > 0; i++)
        {
            int indice = Random.Range(0, available.Count);
            int weaponID = available[indice];

            available.RemoveAt(indice);

            // Equipar en el siguiente holder fijo.
            // Nunca se aleatoriza la posición del holder.
            EquipWeaponInSlot(weaponID, i);
        }

        Debug.Log(
            $"ManagerWeaponEnemy: equipado con {EquippedWeaponCount} arma(s).",
            this
        );
    }
}
