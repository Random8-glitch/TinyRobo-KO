using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Derrota : MonoBehaviour
{
    [Header("Deteccion del jugador")]
    [SerializeField] private string nombreLayerJugador = "Player";
    [SerializeField] private float intervaloComprobacion = 0.5f;

    [Header("UI - Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float duracionFade = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoDerrota;
    [SerializeField] private float delayAntesDeCarga = 2f;

    [Header("Escena de derrota")]
    [SerializeField] private string gameOverScene = "GameOver";

    private bool yaDerrota = false;
    private int layerJugador;
    private float siguienteComprobacion = 0f;

    private void Start()
    {
        layerJugador = LayerMask.NameToLayer(nombreLayerJugador);

        if (layerJugador == -1)
        {
            Debug.LogError(
                "Derrota: no existe la Layer '" +
                nombreLayerJugador + "'. Revisa su nombre en Unity."
            );

            enabled = false;
            return;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.interactable = false;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning(
                "Derrota: no hay CanvasGroup asignado para el fade."
            );
        }

        siguienteComprobacion = Time.time + intervaloComprobacion;
    }

    private void Update()
    {
        if (yaDerrota)
            return;

        if (Time.time < siguienteComprobacion)
            return;

        siguienteComprobacion = Time.time + intervaloComprobacion;

        // Buscar objetos activos en la escena.
        GameObject[] objetos = FindObjectsByType<GameObject>(
            FindObjectsSortMode.None
        );

        bool jugadorEncontrado = false;

        foreach (GameObject objeto in objetos)
        {
            if (objeto.layer == layerJugador)
            {
                jugadorEncontrado = true;
                break;
            }
        }

        // Si no existe ningun objeto activo en la Layer Player,
        // iniciar la derrota.
        if (!jugadorEncontrado)
        {
            ActivarDerrota(
                "No se encontro ningun objeto activo en la Layer Player."
            );
        }
    }


    private void ActivarDerrota(string razon)
    {
        if (yaDerrota)
            return;

        yaDerrota = true;

        Debug.Log("GAME OVER: " + razon);

        // Pausar el juego.
        Time.timeScale = 0f;

        // Iniciar la secuencia de derrota.
        StartCoroutine(SecuenciaDerrota());
    }


    private IEnumerator SecuenciaDerrota()
    {
        // Reproducir sonido sin pausar el juego.
        if (audioSource != null && sonidoDerrota != null)
        {
            audioSource.PlayOneShot(sonidoDerrota);
        }

        yield return new WaitForSecondsRealtime(delayAntesDeCarga);

        // Fundido a negro.
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeANegro());
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene(gameOverScene);
    }

    private IEnumerator FadeANegro()
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionFade)
        {
            tiempoTranscurrido += Time.unscaledDeltaTime;

            fadeCanvasGroup.alpha = duracionFade > 0f
                ? Mathf.Clamp01(tiempoTranscurrido / duracionFade)
                : 1f;

            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }
}

