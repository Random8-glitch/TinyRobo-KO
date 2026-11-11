using UnityEngine;
using UnityEngine.SceneManagement;

public class jefeBoton : MonoBehaviour
{
    public void Starter()
    {
        SceneManager.LoadScene("PeleaZack");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
