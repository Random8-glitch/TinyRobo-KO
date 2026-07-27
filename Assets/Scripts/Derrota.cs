using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Derrota : MonoBehaviour
{

    [Header("Referencias")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private CronometroHUD cronometroHUD;

    [Header("UI - Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float duracionFade = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip soundoDerrota;
    [SerializeField] private float delayAntesDeCarga = 2f; // Espera a que termine el sonido

    [Header("Comportamiento")]
    [SerializeField] private string gameOverScene = "GameOver"; // Nombre de la escena de derrota

    private bool yaDerrota = false;

    private void Start()
    {
        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        if (cronometroHUD == null)
            cronometroHUD = FindObjectOfType<CronometroHUD>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (playerStats == null)
            Debug.LogWarning("Derrota: no se encontró PlayerStats en la escena.");

        if (cronometroHUD == null)
            Debug.LogWarning("Derrota: no se encontró CronometroHUD en la escena.");

        if (fadeCanvasGroup == null)
            Debug.LogWarning("Derrota: no se encontró CanvasGroup para el fade. Asigna una en el inspector.");
        else
        {
            // Asegurar que el fade comienza transparente y no bloquea input
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        if (yaDerrota)
            return;

        // Condición 1: Vida del jugador llega a 0
        if (playerStats != null && playerStats.vida <= 0f)
        {
            ActivarDerrota("La vida del jugador llegó a 0.");
            return;
        }

        // Condición 2: Tiempo del cronómetro se agota
        if (cronometroHUD != null && cronometroHUD.TiempoRestante <= 0f)
        {
            // Al agotarse el tiempo, comparar vida del jugador vs vida total de enemigos
            if (EvaluarDerrotaPorTiempo())
            {
                ActivarDerrota("El tiempo se agotó y la vida del jugador es inferior a la de los enemigos.");
            }
            else
            {
                Debug.Log("Tiempo agotado, pero el jugador tiene igual o más vida que los enemigos. No se ejecuta la derrota.");
            }
            return;
        }

    }

    // Devuelve true si debe ejecutarse la derrota al agotarse el tiempo
    private bool EvaluarDerrotaPorTiempo()
    {
        float vidaJugador = playerStats != null ? playerStats.vida : 0f;

        // Sumar vida de todos los EnemyStats activos en la escena
        EnemyStats[] enemigos = FindObjectsOfType<EnemyStats>();
        float vidaEnemigosTotal = 0f;
        foreach (var e in enemigos)
        {
            if (e != null)
                vidaEnemigosTotal += e.vida;
        }

        Debug.Log($"EvaluarDerrotaPorTiempo -> Vida jugador: {vidaJugador} | Vida enemigos total: {vidaEnemigosTotal}");

        return vidaJugador < vidaEnemigosTotal;
    }

    private void ActivarDerrota(string razon)
    {
        yaDerrota = true;
        Debug.Log($"¡GAME OVER! {razon}");

        // Pausar juego
        Time.timeScale = 0f;

        // Iniciar secuencia de derrota (fade + sonido + carga de escena)
        StartCoroutine(SecuenciaDerrota());
    }

    private IEnumerator SecuenciaDerrota()
    {
        // Reproducir sonido de derrota
        if (audioSource != null && soundoDerrota != null)
        {
            audioSource.PlayOneShot(soundoDerrota);
        }

        // Esperar a que termine el sonido (o el delay configurado)
        yield return new WaitForSecondsRealtime(delayAntesDeCarga);

        // Hacer fade a negro
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeANegro());
        }

        // Cargar escena de Game Over
        Time.timeScale = 1f; // Restaurar tiempo antes de cargar escena
        SceneManager.LoadScene(gameOverScene);
    }

    private IEnumerator FadeANegro()
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionFade)
        {
            tiempoTranscurrido += Time.unscaledDeltaTime;
            fadeCanvasGroup.alpha = Mathf.Clamp01(tiempoTranscurrido / duracionFade);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f; // Asegurar que está completamente negro
    }
}
