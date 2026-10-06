using UnityEngine;
using UnityEngine.UI;

public class BattleStart : MonoBehaviour
{
    [Header("UI de pausa")]
    [SerializeField] private GameObject menuUI;

    [Header("Menús de selección")]
    [SerializeField] private GameObject weaponUI;
    [SerializeField] private GameObject chipUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 0f;

        if (menuUI != null)
        {
            menuUI.SetActive(true);
        }
    }

    public void ContinueGame()
    {
        Time.timeScale = 1f;

        if (menuUI != null)
        {
            menuUI.SetActive(false);
        }
    }

    public void AbrirWeaponMenu()
    {
        if (weaponUI != null)
        {
            weaponUI.SetActive(true);
        }

        if (chipUI != null)
        {
            chipUI.SetActive(false);
        }
    }

    public void AbrirChipMenu()
    {
        if (weaponUI != null)
        {
            weaponUI.SetActive(false);
        }

        if (chipUI != null)
        {
            chipUI.SetActive(true);
        }
    }
}
