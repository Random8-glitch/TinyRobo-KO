
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[DefaultExecutionOrder(100)]
public class ManagerWeapon : MonoBehaviour
{
    public static ManagerWeapon Instance { get; private set; }

    private const int MaxWeaponSlots = 3;

    [Header("Datos de armas")]
    [SerializeField] private WeaponManager weaponManager;

    // Se asignan automáticamente al buscar al jugador.
    // Orden: Vacio Arma 1, Vacio Arma 2, Vacio Arma 3.
    private readonly Transform[] weaponHolders =
        new Transform[MaxWeaponSlots];

    [Header("Ranuras visuales del jugador")]
    [Tooltip("Las 3 imágenes de los iconos.")]
    [SerializeField]
    private Image[] weaponSlotImages =
        new Image[MaxWeaponSlots];

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
    }

    private IEnumerator Start()
    {
        weaponManager = WeaponManager.Instance;

        if (weaponManager == null)
        {
            Debug.LogError(
                "No existe un WeaponManager persistente.",
                this
            );

            yield break;
        }

        // Esperar hasta que PlayerDataManager
        // tenga una carroza instanciada.
        while (PlayerDataManager.Instance == null ||
               PlayerDataManager.Instance.CarrozaActual == null)
        {
            yield return null;
        }

        // Buscar los puntos de las armas.
        if (!BuscarWeaponHolders())
        {
            Debug.LogError(
                "La carroza fue instanciada, pero no se " +
                "encontraron los tres puntos de armas.",
                this
            );

            yield break;
        }

        // Recuperar las armas solamente cuando
        // todos los puntos estén disponibles.
        RecuperarArmasEquipadas();
    }

    private bool BuscarWeaponHolders()
    {
        PlayerDataManager playerData =
            PlayerDataManager.Instance;

        if (playerData == null ||
            playerData.CarrozaActual == null)
        {
            return false;
        }

        // Buscar únicamente dentro de la carroza instanciada.
        Transform carroza =
            playerData.CarrozaActual.transform;

        Transform[] hijos =
            carroza.GetComponentsInChildren<Transform>(true);

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
        if (weaponSlotImages == null ||
            weaponSlotImages.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                "ManagerWeapon necesita exactamente " +
                "3 imágenes de UI.",
                this
            );
        }
    }

    // ==========================================
    // SINCRONIZACIÓN CON WEAPONMANAGER
    // ==========================================

    private void SincronizarArmasEquipadas()
    {
        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }

        if (weaponManager == null)
            return;

        weaponManager.ActualizarArmasEquipadas(
            GetEquippedWeapons()
        );
    }

    private void RecuperarArmasEquipadas()
    {
        if (weaponManager == null)
            return;

        int[] savedWeapons =
            weaponManager.GetEquippedWeapons();

        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            int weaponID = savedWeapons[i];

            if (weaponID == -1)
                continue;

            WeaponManager.WeaponRuntimeData weapon =
                weaponManager.GetWeapon(weaponID);

            if (weapon == null)
                continue;

            if (!weapon.Activo || !weapon.Comprado)
                continue;

            if (GetWeaponSlot(weaponID) != -1)
                continue;

            EquipWeaponInSlot(weapon, i, false);
        }

        SincronizarArmasEquipadas();
    }

    // ==========================================
    // EQUIPAR Y DESEQUIPAR ARMAS
    // ==========================================

    public void ToggleWeapon(int weaponID)
    {
        if (weaponManager == null)
        {
            weaponManager = WeaponManager.Instance;
        }

        if (weaponManager == null)
        {
            Debug.LogError(
                "ManagerWeapon no tiene un WeaponManager.",
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

        if (!weapon.Activo)
        {
            Debug.LogWarning(
                $"El arma {weapon.WeaponName} no está activa. " +
                $"Requiere rango {weapon.Rank}.",
                this
            );
            return;
        }

        if (!weapon.Comprado)
        {
            Debug.LogWarning(
                $"El arma {weapon.WeaponName} no está comprada.",
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
                "Los tres puestos de armas están ocupados.",
                this
            );
            return;
        }

        EquipWeaponInSlot(weapon, emptySlot);
    }

    private void EquipWeaponInSlot(
        WeaponManager.WeaponRuntimeData weapon,
        int slotIndex,
        bool sincronizar = true)
    {
        if (weapon == null)
            return;

        if (!IsValidSlot(slotIndex))
            return;

        if (!IsSlotEmpty(slotIndex))
            return;

        Transform holder = GetWeaponHolder(slotIndex);

        if (holder == null)
        {
            Debug.LogError(
                $"No hay Weapon Holder en la ranura {slotIndex}.",
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

        // Rotación especial únicamente para el arma ID 4
        if (weapon.WeaponID == 4 || weapon.WeaponID == 5)
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
        equippedWeaponIDs[slotIndex] = weapon.WeaponID;

        UpdateWeaponSlotUI(
            slotIndex,
            weapon.WeaponIcon
        );

        if (sincronizar)
        {
            SincronizarArmasEquipadas();
        }
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

        SincronizarArmasEquipadas();
    }

    // ==========================================
    // INTERFAZ DE USUARIO
    // ==========================================

    private void UpdateWeaponSlotUI(
        int slotIndex,
        Sprite weaponIcon)
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
        if (weaponID < 0)
            return false;

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


    private void OnEnable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnRobotEquipadoChanged +=
                ReconstruirArmasTrasCambioRobot;
        }
    }

    private void OnDisable()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnRobotEquipadoChanged -=
                ReconstruirArmasTrasCambioRobot;
        }
    }

    private void ReconstruirArmasTrasCambioRobot()
    {
        // Limpiar referencias de la carroza anterior.
        // No sincronizar con WeaponManager todavía.
        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            weaponHolders[i] = null;
            weaponInstances[i] = null;
            equippedWeaponIDs[i] = -1;

            ClearWeaponSlotUI(i);
        }

        // Localizar los puntos de la nueva carroza.
        if (!BuscarWeaponHolders())
        {
            Debug.LogWarning(
                "No se encontraron los puntos de armas " +
                "en la nueva carroza.",
                this
            );
            return;
        }

        // Restaurar las armas desde WeaponManager.
        RecuperarArmasEquipadas();
    }

}
