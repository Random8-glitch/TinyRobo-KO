
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerDataManager))]
public class WeaponManager : MonoBehaviour
{
    [Serializable]
    public class WeaponRuntimeData
    {
        [Header("Datos del arma")]
        [SerializeField] private int weaponID;
        [SerializeField] private string weaponName;
        [SerializeField] private GameObject weaponPrefab;
        [SerializeField] private Sprite weaponIcon;
        [SerializeField] private int rank;

        [Header("Estado del arma")]
        [SerializeField] private bool comprado;
        [SerializeField] private bool activo;

        public int WeaponID => weaponID;
        public string WeaponName => weaponName;
        public GameObject WeaponPrefab => weaponPrefab;
        public Sprite WeaponIcon => weaponIcon;
        public int Rank => rank;

        public bool Comprado
        {
            get => comprado;
            set => comprado = value;
        }

        public bool Activo
        {
            get => activo;
            set => activo = value;
        }

        public WeaponRuntimeData(
            int weaponID,
            WeaponList.WeaponData weaponData,
            bool comprado,
            bool activo)
        {
            this.weaponID = weaponID;

            weaponName = weaponData.WeaponName;
            weaponPrefab = weaponData.WeaponPrefab;
            weaponIcon = weaponData.WeaponIcon;
            rank = weaponData.Rank;

            this.comprado = comprado;
            this.activo = activo;
        }
    }

    [Header("Chip equipado del jugador")]
    [Tooltip("ScriptableObject original del chip equipado.")]
    [SerializeField]
    private ModificadorStats chipEquipado;

    [Tooltip("Indica si ya se inicializó el estado del chip.")]
    [SerializeField]
    private bool chipInicializado;

    public ModificadorStats ChipEquipado => chipEquipado;

    public bool ChipInicializado => chipInicializado;

    public event Action OnChipEquipadoChanged;

    public static WeaponManager Instance { get; private set; }

    private const int MaxWeaponSlots = 3;

    [Header("Lista original de armas")]
    [SerializeField] private WeaponList weaponList;

    [Header("Datos de armas durante la partida")]
    [SerializeField]
    private List<WeaponRuntimeData> weapons =
        new List<WeaponRuntimeData>();

    [Header("Armas equipadas del jugador")]
    [Tooltip("Los ID de las tres armas equipadas. -1 indica una ranura vacía.")]
    [SerializeField]
    private List<int> equippedWeaponIDs = new List<int>();

    private PlayerDataManager playerDataManager;

    public IReadOnlyList<WeaponRuntimeData> Weapons => weapons;

    public IReadOnlyList<int> EquippedWeaponIDs =>
        equippedWeaponIDs.AsReadOnly();

    public event Action OnWeaponsChanged;
    public event Action OnEquippedWeaponsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        playerDataManager = GetComponent<PlayerDataManager>();

        if (playerDataManager == null)
        {
            Debug.LogError(
                "No se encontró PlayerDataManager en este GameObject.",
                this
            );
            return;
        }

        playerDataManager.OnPlayerDataChanged +=
            ActualizarArmasPorRango;

        InicializarRanurasEquipadas();
        CrearListaDeArmas();
    }

    private void OnDestroy()
    {
        if (playerDataManager != null)
        {
            playerDataManager.OnPlayerDataChanged -=
                ActualizarArmasPorRango;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InicializarRanurasEquipadas()
    {
        if (equippedWeaponIDs == null)
        {
            equippedWeaponIDs = new List<int>();
        }

        while (equippedWeaponIDs.Count < MaxWeaponSlots)
        {
            equippedWeaponIDs.Add(-1);
        }

        if (equippedWeaponIDs.Count > MaxWeaponSlots)
        {
            equippedWeaponIDs.RemoveRange(
                MaxWeaponSlots,
                equippedWeaponIDs.Count - MaxWeaponSlots
            );
        }
    }

    private void CrearListaDeArmas()
    {
        weapons.Clear();

        if (weaponList == null)
        {
            Debug.LogError(
                "No se ha asignado un WeaponList.",
                this
            );
            return;
        }

        int playerRank = playerDataManager.Rank;

        for (int weaponID = 0;
             weaponID < weaponList.WeaponCount;
             weaponID++)
        {
            WeaponList.WeaponData weaponData =
                weaponList.GetWeapon(weaponID);

            if (weaponData == null)
            {
                Debug.LogWarning(
                    $"El arma con ID {weaponID} no es válida.",
                    this
                );
                continue;
            }

            bool compradoInicial = weaponID < 3;

            bool activoInicial =
                playerRank >= weaponData.Rank;

            WeaponRuntimeData runtimeData =
                new WeaponRuntimeData(
                    weaponID,
                    weaponData,
                    compradoInicial,
                    activoInicial
                );

            weapons.Add(runtimeData);
        }
    }

    public void ActualizarArmasPorRango()
    {
        if (playerDataManager == null)
            return;

        int playerRank = playerDataManager.Rank;

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponRuntimeData weapon = weapons[i];

            if (weapon == null)
                continue;

            weapon.Activo =
                playerRank >= weapon.Rank;
        }

        OnWeaponsChanged?.Invoke();
    }

    public WeaponRuntimeData GetWeapon(int weaponID)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null &&
                weapons[i].WeaponID == weaponID)
            {
                return weapons[i];
            }
        }

        return null;
    }

    public bool ComprarArma(int weaponID)
    {
        WeaponRuntimeData weapon = GetWeapon(weaponID);

        if (weapon == null)
            return false;

        if (!weapon.Activo)
        {
            Debug.LogWarning(
                $"El arma con ID {weaponID} requiere rango " +
                $"{weapon.Rank}.",
                this
            );
            return false;
        }

        if (weapon.Comprado)
            return false;

        weapon.Comprado = true;

        OnWeaponsChanged?.Invoke();

        return true;
    }

    public bool EstaComprada(int weaponID)
    {
        WeaponRuntimeData weapon = GetWeapon(weaponID);

        return weapon != null && weapon.Comprado;
    }

    public bool EstaActiva(int weaponID)
    {
        WeaponRuntimeData weapon = GetWeapon(weaponID);

        return weapon != null && weapon.Activo;
    }

    // ==========================================
    // SISTEMA DE ARMAS EQUIPADAS
    // ==========================================

    public void ActualizarArmasEquipadas(int[] weaponIDs)
    {
        if (weaponIDs == null ||
            weaponIDs.Length != MaxWeaponSlots)
        {
            Debug.LogError(
                "La lista de armas equipadas debe " +
                "contener exactamente tres posiciones.",
                this
            );
            return;
        }

        InicializarRanurasEquipadas();

        bool huboCambios = false;

        for (int i = 0; i < MaxWeaponSlots; i++)
        {
            if (equippedWeaponIDs[i] != weaponIDs[i])
            {
                equippedWeaponIDs[i] = weaponIDs[i];
                huboCambios = true;
            }
        }

        if (huboCambios)
        {
            OnEquippedWeaponsChanged?.Invoke();
        }
    }

    public int GetEquippedWeapon(int slotIndex)
    {
        if (slotIndex < 0 ||
            slotIndex >= MaxWeaponSlots)
        {
            return -1;
        }

        InicializarRanurasEquipadas();

        return equippedWeaponIDs[slotIndex];
    }

    public int[] GetEquippedWeapons()
    {
        InicializarRanurasEquipadas();

        return equippedWeaponIDs.ToArray();
    }

    public bool IsWeaponEquipped(int weaponID)
    {
        if (weaponID < 0)
            return false;

        InicializarRanurasEquipadas();

        return equippedWeaponIDs.Contains(weaponID);
    }


    

    public void ActualizarChipEquipado(ModificadorStats chip)
    {
        // Siempre registrar el ScriptableObject original.
        if (chip != null && chip.Origen != null)
        {
            chip = chip.Origen;
        }

        bool huboCambios =
            !chipInicializado || chipEquipado != chip;

        chipEquipado = chip;
        chipInicializado = true;

        if (huboCambios)
        {
            OnChipEquipadoChanged?.Invoke();
        }
    }

    public ModificadorStats GetChipEquipado()
    {
        return chipEquipado;
    }

    public bool TieneChipEquipado()
    {
        return chipEquipado != null;
    }

    public bool EsChipEquipado(ModificadorStats chip)
    {
        if (chip == null)
            return false;

        ModificadorStats original =
            chip.Origen != null ? chip.Origen : chip;

        return chipEquipado == original;
    }

}
