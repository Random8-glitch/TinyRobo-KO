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
            bool activo
        )
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

    [Header("Lista original de armas")]
    [SerializeField] private WeaponList weaponList;

    [Header("Datos de armas durante la partida")]
    [SerializeField]
    private List<WeaponRuntimeData> weapons =
        new List<WeaponRuntimeData>();

    private PlayerDataManager playerDataManager;

    public IReadOnlyList<WeaponRuntimeData> Weapons => weapons;

    private void Awake()
    {
        playerDataManager = GetComponent<PlayerDataManager>();

        if (playerDataManager == null)
        {
            Debug.LogError(
                "No se encontró PlayerDataManager en este GameObject.",
                this
            );

            return;
        }

        /*
         * Se suscribe antes de crear la lista.
         *
         * Si el Awake de PlayerDataManager todavía no se ha ejecutado,
         * su LoadData invocará posteriormente este evento y actualizará
         * los estados con el rango cargado desde PlayerPrefs.
         */
        playerDataManager.OnPlayerDataChanged += ActualizarArmasPorRango;

        CrearListaDeArmas();
    }

    private void OnDestroy()
    {
        if (playerDataManager != null)
        {
            playerDataManager.OnPlayerDataChanged -=
                ActualizarArmasPorRango;
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

        for (
            int weaponID = 0;
            weaponID < weaponList.WeaponCount;
            weaponID++
        )
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

            // Las armas con ID 0, 1 y 2 comienzan compradas.
            bool compradoInicial = weaponID < 3;

            /*
             * El arma está activa cuando el rango del jugador
             * es igual o superior al rango requerido.
             */
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
        return true;
    }

    public bool EstaComprada(int weaponID)
    {
        WeaponRuntimeData weapon = GetWeapon(weaponID);

        return weapon != null &&
               weapon.Comprado;
    }

    public bool EstaActiva(int weaponID)
    {
        WeaponRuntimeData weapon = GetWeapon(weaponID);

        return weapon != null &&
               weapon.Activo;
    }
}