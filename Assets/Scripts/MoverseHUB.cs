using UnityEngine;
using UnityEngine.SceneManagement;

public class MoverseHUB : MonoBehaviour
{
    [SerializeField] private GameObject HUB;
    [SerializeField] private GameObject Tienda;
    [SerializeField] private GameObject Taller;

    public void RegresarMenu()
    {
        SceneManager.LoadScene("Menu Inicio");
    }

    public void PeleaRapida()
    {
        SceneManager.LoadScene("RoboTeam");
    }

    public void AbrirHUB()
    {
        if (HUB != null)
        {
            HUB.SetActive(true);
        }

        if (Tienda != null)
        {
            Tienda.SetActive(false);
        }

        if (Taller != null)
        {
            Taller.SetActive(false);
        }
    }

    public void AbrirTienda()
    {
        if (HUB != null)
        {
            HUB.SetActive(false);
        }

        if (Tienda != null)
        {
            Tienda.SetActive(true);
        }

        if (Taller != null)
        {
            Taller.SetActive(false);
        }
    }

    public void AbrirTaller()
    {
        if (HUB != null)
        {
            HUB.SetActive(false);
        }

        if (Tienda != null)
        {
            Tienda.SetActive(false);
        }

        if (Taller != null)
        {
            Taller.SetActive(true);
        }
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
