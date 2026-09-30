using UnityEngine;
using UnityEngine.UI;

public class PlayerWeaponToggle : MonoBehaviour
{
    [SerializeField] private int weaponID;
    [SerializeField] private Button button;

    private WeaponManager weaponManager;

    private void Start()
    {
        weaponManager = WeaponManager.Instance;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (weaponManager != null)
        {
            weaponManager.OnWeaponsChanged += ActualizarBoton;
        }

        ActualizarBoton();
    }

    private void OnDestroy()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponsChanged -= ActualizarBoton;
        }
    }

    public void ActualizarBoton()
    {
        if (button == null)
            return;

        if (weaponManager == null)
        {
            button.interactable = false;
            return;
        }

        WeaponManager.WeaponRuntimeData weapon =
            weaponManager.GetWeapon(weaponID);

        button.interactable =
            weapon != null &&
            weapon.Comprado;
    }

    public void PresionarBoton()
    {
        if (!button.interactable)
            return;

        if (ManagerWeapon.Instance == null)
            return;

        ManagerWeapon.Instance.ToggleWeapon(weaponID);
    }
}