using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuDeInicio : MonoBehaviour
{
    public void Starter()
    {
        SceneManager.LoadScene("RoboTeam");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
