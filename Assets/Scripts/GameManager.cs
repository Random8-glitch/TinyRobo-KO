using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Lugar donde aparecerá el arma")]
    public Transform weaponHolder;

    [Header("Lista de armas")]
    public GameObject[] weaponPrefabs;

    private GameObject currentWeapon;

    private int equippedWeapon = -1;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipWeapon(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipWeapon(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            EquipWeapon(2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            EquipWeapon(3);
        }
    }

    public void EquipWeapon(int weaponID)
    {
        // Comprobar que exista
        if (weaponID < 0 || weaponID >= weaponPrefabs.Length)
            return;

        // Eliminar la anterior
        if (currentWeapon != null)
        {
            Destroy(currentWeapon);
        }

        // Crear la nueva
        currentWeapon = Instantiate(
            weaponPrefabs[weaponID],
            weaponHolder.position,
            weaponHolder.rotation,
            weaponHolder
        );

        equippedWeapon = weaponID;
    }

    public int GetEquippedWeapon()
    {
        return equippedWeapon;
    }
}