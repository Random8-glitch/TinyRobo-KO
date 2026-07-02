using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelInicio;

    private void Start()
    {
        // Pausar el juego al iniciar
        Time.timeScale = 0f;
    }

    public void IniciarJuego()
    {
        // Reanudar el juego
        Time.timeScale = 1f;

        // Ocultar el panel
        if (panelInicio != null)
        {
            panelInicio.SetActive(false);
        }
    }
}