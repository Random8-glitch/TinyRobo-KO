

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public class ManagerWeaponEnemy : MonoBehaviour
{
    public static ManagerWeaponEnemy Instance { get; private set; }

    private const int MaxWeaponSlots = 3;

    [Header("Lista de armas")]
    [SerializeField] private WeaponList weaponList;

    // Se buscan automáticamente dentro del enemigo.
    // Orden: Vacio Arma 1, Vacio Arma 2, Vacio Arma 3.
    private readonly Transform[] weaponHolders =
        new Transform[MaxWeaponSlots];

    [Header("Ranuras visuales del enemigo")]
    [SerializeField]
    private Image[] weaponSlotImages =
        new Image[MaxWeaponSlots];

    [Header("Auto equipamiento")]
    [SerializeField] private bool autoEquipOnAwake = true;
    [SerializeField] private int maxScanWeaponID = 64;

    [Header("Chips disponibles para este enemigo")]
    [Tooltip("50% de probabilidad de equipar un chip aleatorio.")]
    [SerializeField]
    private List<ModificadorStats> chipsDisponibles =
        new List<ModificadorStats>();

    private EquiparChips equiparChips;

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

    // ==========================================
    // INICIALIZACIÓN
    // ==========================================

    private void Awake()
    {
        InitializeWeaponSlots();

        // Buscar los puntos de armas dentro del enemigo.
        bool holdersEncontrados = BuscarWeaponHolders();

        // Buscar el componente de chips del propio enemigo.
        equiparChips = BuscarEquiparChipsEnemy();

        // Equipar armas solamente si existen los holders.
        if (autoEquipOnAwake)
        {
            if (holdersEncontrados)
            {
                RandomizeAndEquipWeapons();
            }
            else
            {
                Debug.LogError(
                    "ManagerWeaponEnemy: no se encontraron " +
                    "los tres puntos de armas.",
                    this
                );
            }
        }
    }

    private IEnumerator Start()
    {
        // Esperar a que EquiparChips termine su Start.
        yield return null;

        // Volver a buscar por si aún no estaba disponible.
        if (equiparChips == null)
        {
            equiparChips = BuscarEquiparChipsEnemy();
        }

        RandomizeAndEquipChip();
    }

    // ==========================================
    // BÚSQUEDA AUTOMÁTICA DE WEAPON HOLDERS
    // ==========================================

    private bool BuscarWeaponHolders()
    {
        // Buscar únicamente dentro del enemigo
        // que contiene este administrador.
        Transform enemigo = transform;

        Transform[] hijos =
            enemigo.GetComponentsInChildren<Transform>(true);

        // Limpiar referencias anteriores.
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            weaponHolders[i] = null;
        }

        foreach (Transform hijo in hijos)
        {
            if (hijo.name == "Vacio Arma 1")
            {
                weaponHolders[0] = hijo;
            }
            else if (hijo.name == "Vacio Arma 2")
            {
                weaponHolders[1] = hijo;
            }
            else if (hijo.name == "Vacio Arma 3")
            {
                weaponHolders[2] = hijo;
            }
        }

        // Comprobar que encontramos los tres puntos.
        return weaponHolders[0] != null &&
               weaponHolders[1] != null &&
               weaponHolders[2] != null;
    }

    // ==========================================
    // BÚSQUEDA DEL COMPONENTE EQUIPARCHIPS
    // ==========================================

    private EquiparChips BuscarEquiparChipsEnemy()
    {
        // Primero buscar en el mismo GameObject.
        EquiparChips encontrado =
            GetComponent<EquiparChips>();

        if (encontrado != null)
            return encontrado;

        // Después buscar dentro de los hijos del enemigo.
        encontrado =
            GetComponentInChildren<EquiparChips>(true);

        if (encontrado == null)
        {
            Debug.LogWarning(
                "ManagerWeaponEnemy: no se encontró " +
                "EquiparChips dentro de este enemigo.",
                this
            );
        }

        return encontrado;
    }

    // ==========================================
    // EQUIPAMIENTO ALEATORIO DE CHIPS
    // ==========================================

    private void RandomizeAndEquipChip()
    {
        if (equiparChips == null)
        {
            Debug.LogWarning(
                "ManagerWeaponEnemy: no hay " +
                "EquiparChips disponible.",
                this
            );

            return;
        }

        // 50% de probabilidad de quedarse sin chip.
        if (Random.value < 0.5f)
        {
            equiparChips.Desequipar();

            Debug.Log(
                "ManagerWeaponEnemy: enemigo sin chip.",
                this
            );

            return;
        }

        List<ModificadorStats> chipsValidos =
            new List<ModificadorStats>();

        if (chipsDisponibles != null)
        {
            foreach (ModificadorStats chip in chipsDisponibles)
            {
                if (chip != null)
                    chipsValidos.Add(chip);
            }
        }

        if (chipsValidos.Count == 0)
        {
            equiparChips.Desequipar();

            Debug.LogWarning(
                "ManagerWeaponEnemy: no hay chips válidos.",
                this
            );

            return;
        }

        int indice = Random.Range(0, chipsValidos.Count);

        ModificadorStats chipElegido =
            chipsValidos[indice];

        equiparChips.Equipar(chipElegido);

        Debug.Log(
            $"ManagerWeaponEnemy: chip equipado: " +
            $"{chipElegido.name}.",
            this
        );
    }

    // ==========================================
    // INICIALIZACIÓN DE RANURAS
    // ==========================================

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

        if (weaponSlotImages == null ||
            weaponSlotImages.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                "Se necesitan exactamente 3 imágenes de UI.",
                this
            );
        }
    }

    // ==========================================
    // EQUIPAR Y DESEQUIPAR ARMAS
    // ==========================================

    public void ToggleWeapon(int weaponID)
    {
        if (!IsValidWeaponID(weaponID))
        {
            Debug.LogWarning(
                $"El arma con ID {weaponID} no existe.",
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
                "Los tres slots están ocupados.",
                this
            );

            return;
        }

        EquipWeaponInSlot(weaponID, emptySlot);
    }

    private void EquipWeaponInSlot(
        int weaponID,
        int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (!IsSlotEmpty(slotIndex))
            return;

        Transform holder = GetWeaponHolder(slotIndex);

        if (holder == null)
        {
            Debug.LogError(
                $"No hay Weapon Holder en el slot {slotIndex}.",
                this
            );

            return;
        }

        if (weaponList == null)
            return;

        GameObject weaponPrefab =
            weaponList.GetWeaponPrefab(weaponID);

        if (weaponPrefab == null)
        {
            Debug.LogError(
                $"El arma {weaponID} no tiene prefab.",
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

        // Igual que ManagerWeapon:
        // rotación especial para las armas ID 4 y 5.
        if (weaponID == 4 || weaponID == 5)
        {
            newWeapon.transform.localRotation =
                Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            newWeapon.transform.localRotation =
                Quaternion.identity;
        }

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

    // Público para EnemyWeaponAnalysis.
    public Transform GetWeaponHolder(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        return weaponHolders[slotIndex];
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

    public void ClearAllWeapons()
    {
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            RemoveWeaponFromSlot(i);
        }
    }

    // ==========================================
    // CONSULTAS DE ARMAS
    // ==========================================

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

    // ==========================================
    // INTERFAZ DE USUARIO
    // ==========================================

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

    private void UpdateWeaponSlotUI(
        int slotIndex,
        int weaponID)
    {
        Image slotImage = GetWeaponSlotImage(slotIndex);

        if (slotImage == null)
            return;

        Sprite weaponIcon =
            weaponList.GetWeaponIcon(weaponID);

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

    // ==========================================
    // VALIDACIONES
    // ==========================================

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

    // ==========================================
    // OBTENER ARMAS DISPONIBLES
    // ==========================================

    private List<int> GetAvailableWeaponIDs()
    {
        List<int> result = new List<int>();

        if (weaponList == null)
            return result;

        var method = weaponList.GetType().GetMethod(
            "GetAllWeaponIDs"
        );

        if (method != null)
        {
            var obj = method.Invoke(weaponList, null);

            if (obj is IEnumerable<int> ids)
            {
                foreach (int id in ids)
                {
                    if (IsValidWeaponID(id) &&
                        !result.Contains(id))
                    {
                        result.Add(id);
                    }
                }

                return result;
            }
        }

        for (int id = 0; id < maxScanWeaponID; id++)
        {
            if (IsValidWeaponID(id))
            {
                result.Add(id);
            }
        }

        return result;
    }

    // ==========================================
    // EQUIPAMIENTO ALEATORIO DE ARMAS
    // ==========================================

    private void RandomizeAndEquipWeapons()
    {
        ClearAllWeapons();

        List<int> available = GetAvailableWeaponIDs();

        if (available.Count == 0)
        {
            Debug.LogWarning(
                "No hay armas disponibles en WeaponList.",
                this
            );

            return;
        }

        // Seleccionar entre 1 y 3 armas.
        int cantidad = Random.Range(
            1,
            MaxWeaponSlots + 1
        );

        // Elegir armas sin repetir IDs.
        for (int i = 0;
             i < cantidad && available.Count > 0;
             i++)
        {
            int indice = Random.Range(
                0,
                available.Count
            );

            int weaponID = available[indice];

            available.RemoveAt(indice);

            // Los holders mantienen su orden fijo.
            EquipWeaponInSlot(weaponID, i);
        }

        Debug.Log(
            $"ManagerWeaponEnemy: equipado con " +
            $"{EquippedWeaponCount} arma(s).",
            this
        );
    }
}
