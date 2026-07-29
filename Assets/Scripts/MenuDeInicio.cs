using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuDeInicio : MonoBehaviour
{
    public void Starter()
    {
        SceneManager.LoadScene("HubPrincipal");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
