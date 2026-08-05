using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuDePlaza : MonoBehaviour
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
