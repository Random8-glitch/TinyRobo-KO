using UnityEngine;
using UnityEngine.SceneManagement;

public class VolverAlMenuDeInicio : MonoBehaviour
{
    public void Starter()
    {
        SceneManager.LoadScene("Menu Inicio");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
