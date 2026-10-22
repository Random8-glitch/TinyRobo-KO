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
        SceneManager.LoadScene("HubPrincipal");
    }

    public void AbrirTienda()
    {
        SceneManager.LoadScene("Tienda");
    }

    public void AbrirTaller()
    {
        SceneManager.LoadScene("Taller");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
