using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const int MaxWeaponSlots = 3;

    [Header("Weapon Holders")]
    [Tooltip("Los 3 lugares donde aparecerán físicamente las armas.")]
    [SerializeField]
    private Transform[] weaponHolders =
        new Transform[MaxWeaponSlots];

    [Header("Armas disponibles")]
    [Tooltip("El índice del prefab funciona como ID del arma.")]
    [SerializeField] private GameObject[] weaponPrefabs;

    [Header("Iconos de las armas")]
    [Tooltip("Cada icono debe tener el mismo índice que su prefab.")]
    [SerializeField] private Sprite[] weaponIcons;

    [Header("Ranuras visuales del UI")]
    [Tooltip("Las 3 imágenes donde aparecerán los iconos.")]
    [SerializeField]
    private Image[] weaponSlotImages =
        new Image[MaxWeaponSlots];

    // ID del arma almacenada en cada puesto.
    // -1 significa que el puesto está vacío.
    private readonly int[] equippedWeaponIDs =
        new int[MaxWeaponSlots];

    // Instancia física del arma de cada puesto.
    private readonly GameObject[] weaponInstances =
        new GameObject[MaxWeaponSlots];

    [Header("UI de pausa")]
    [SerializeField] private GameObject pauseUI;

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
    }

    private void Start()
    {
        Time.timeScale = 0f;

        if (pauseUI != null)
        {
            pauseUI.SetActive(true);
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
        if (weaponHolders == null ||
            weaponHolders.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                $"GameManager necesita exactamente " +
                $"{MaxWeaponSlots} Weapon Holders.",
                this
            );
        }

        if (weaponSlotImages == null ||
            weaponSlotImages.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                $"GameManager necesita exactamente " +
                $"{MaxWeaponSlots} imágenes de UI.",
                this
            );
        }

        if (weaponPrefabs == null)
        {
            Debug.LogError(
                "La lista de prefabs de armas no está configurada.",
                this
            );
        }

        if (weaponIcons == null)
        {
            Debug.LogError(
                "La lista de iconos de armas no está configurada.",
                this
            );
        }
        else if (
            weaponPrefabs != null &&
            weaponIcons.Length < weaponPrefabs.Length
        )
        {
            Debug.LogWarning(
                "Hay menos iconos que prefabs de armas. " +
                "Algunas armas no mostrarán icono.",
                this
            );
        }
    }

    /// <summary>
    /// Método asignado a los botones de armas.
    ///
    /// Si el arma ya está equipada, se elimina.
    /// Si no está equipada, ocupa el primer puesto vacío.
    /// Si los tres puestos están ocupados, no sucede nada.
    /// </summary>
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

        // Si el arma ya está en la lista, quitarla.
        int currentSlot = GetWeaponSlot(weaponID);

        if (currentSlot != -1)
        {
            RemoveWeaponFromSlot(currentSlot);
            return;
        }

        // Buscar el primer puesto vacío.
        int emptySlot = GetFirstEmptySlot();

        if (emptySlot == -1)
        {
            Debug.Log(
                "Los tres puestos de armas están ocupados.",
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
                $"al puesto {slotIndex}.",
                this
            );

            return;
        }

        GameObject weaponPrefab = weaponPrefabs[weaponID];

        if (weaponPrefab == null)
        {
            Debug.LogError(
                $"El prefab del arma {weaponID} " +
                "no está asignado.",
                this
            );

            return;
        }

        // Crear el arma física.
        GameObject newWeapon = Instantiate(
            weaponPrefab,
            holder,
            false
        );

        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        weaponInstances[slotIndex] = newWeapon;
        equippedWeaponIDs[slotIndex] = weaponID;

        // Colocar su icono en el mismo puesto del UI.
        UpdateWeaponSlotUI(slotIndex, weaponID);
    }

    public void RemoveWeaponFromSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (equippedWeaponIDs[slotIndex] == -1)
            return;

        // Eliminar el arma física.
        if (weaponInstances[slotIndex] != null)
        {
            Destroy(weaponInstances[slotIndex]);
        }

        weaponInstances[slotIndex] = null;
        equippedWeaponIDs[slotIndex] = -1;

        // Vaciar la misma posición del UI.
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

        if (!IsValidWeaponIcon(weaponID))
        {
            Debug.LogWarning(
                $"El arma {weaponID} no tiene un icono válido.",
                this
            );

            ClearWeaponSlotUI(slotIndex);
            return;
        }

        slotImage.sprite = weaponIcons[weaponID];
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
        if (weaponHolders == null)
            return null;

        if (slotIndex < 0 ||
            slotIndex >= weaponHolders.Length)
        {
            return null;
        }

        return weaponHolders[slotIndex];
    }

    private Image GetWeaponSlotImage(int slotIndex)
    {
        if (weaponSlotImages == null)
            return null;

        if (slotIndex < 0 ||
            slotIndex >= weaponSlotImages.Length)
        {
            return null;
        }

        return weaponSlotImages[slotIndex];
    }

    private bool IsValidWeaponID(int weaponID)
    {
        return weaponPrefabs != null &&
               weaponID >= 0 &&
               weaponID < weaponPrefabs.Length;
    }

    private bool IsValidWeaponIcon(int weaponID)
    {
        return weaponIcons != null &&
               weaponID >= 0 &&
               weaponID < weaponIcons.Length &&
               weaponIcons[weaponID] != null;
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < MaxWeaponSlots;
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;

        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }
    }
}