using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponList",
    menuName = "Weapons/Weapon List"
)]
public class WeaponList : ScriptableObject
{
    [System.Serializable]
    public class WeaponData
    {
        [Tooltip("Nombre utilizado para identificar el arma.")]
        [SerializeField] private string weaponName;

        [Tooltip("Prefab físico del arma.")]
        [SerializeField] private GameObject weaponPrefab;

        [Tooltip("Icono mostrado en la interfaz.")]
        [SerializeField] private Sprite weaponIcon;

        [Tooltip("Rango mínimo necesario para desbloquear el arma.")]
        [Min(1)]
        [SerializeField] private int rank = 1;

        public string WeaponName => weaponName;
        public GameObject WeaponPrefab => weaponPrefab;
        public Sprite WeaponIcon => weaponIcon;
        public int Rank => rank;
    }

    [Header("Armas disponibles")]
    [Tooltip(
        "La posición de cada arma dentro de esta lista funciona como su ID."
    )]
    [SerializeField]
    private WeaponData[] weapons = new WeaponData[0];

    public int WeaponCount
    {
        get
        {
            if (weapons == null)
                return 0;

            return weapons.Length;
        }
    }

    public bool IsValidWeaponID(int weaponID)
    {
        return weapons != null &&
               weaponID >= 0 &&
               weaponID < weapons.Length &&
               weapons[weaponID] != null;
    }

    public WeaponData GetWeapon(int weaponID)
    {
        if (!IsValidWeaponID(weaponID))
            return null;

        return weapons[weaponID];
    }

    public GameObject GetWeaponPrefab(int weaponID)
    {
        WeaponData weapon = GetWeapon(weaponID);

        if (weapon == null)
            return null;

        return weapon.WeaponPrefab;
    }

    public Sprite GetWeaponIcon(int weaponID)
    {
        WeaponData weapon = GetWeapon(weaponID);

        if (weapon == null)
            return null;

        return weapon.WeaponIcon;
    }

    public string GetWeaponName(int weaponID)
    {
        WeaponData weapon = GetWeapon(weaponID);

        if (weapon == null)
            return string.Empty;

        return weapon.WeaponName;
    }

    public int GetWeaponRank(int weaponID)
    {
        WeaponData weapon = GetWeapon(weaponID);

        if (weapon == null)
            return 0;

        return weapon.Rank;
    }

    private void OnValidate()
    {
        if (weapons == null)
            return;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] == null)
            {
                Debug.LogWarning(
                    $"El elemento de arma con ID {i} está vacío.",
                    this
                );

                continue;
            }

            if (weapons[i].WeaponPrefab == null)
            {
                Debug.LogWarning(
                    $"El arma con ID {i} no tiene un prefab asignado.",
                    this
                );
            }

            if (weapons[i].WeaponIcon == null)
            {
                Debug.LogWarning(
                    $"El arma con ID {i} no tiene un icono asignado.",
                    this
                );
            }

            if (weapons[i].Rank < 1)
            {
                Debug.LogWarning(
                    $"El arma con ID {i} tiene un rango menor que 1.",
                    this
                );
            }
        }
    }
}