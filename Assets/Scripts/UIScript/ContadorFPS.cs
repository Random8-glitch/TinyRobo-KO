using TMPro;
using UnityEngine;

public class ContadorFPS : MonoBehaviour
{
    [Header("Texto de FPS")]
    [SerializeField] private TMP_Text textoFPS;

    [Header("Actualizacion")]
    [SerializeField] private float intervaloActualizacion = 0.5f;

    private float tiempoTranscurrido;
    private int fotogramas;

    private void Update()
    {
        fotogramas++;
        tiempoTranscurrido += Time.unscaledDeltaTime;

        if (tiempoTranscurrido < intervaloActualizacion)
        {
            return;
        }

        float fps = fotogramas / tiempoTranscurrido;

        if (textoFPS != null)
        {
            textoFPS.text = $"FPS: {Mathf.RoundToInt(fps)}";
        }

        fotogramas = 0;
        tiempoTranscurrido = 0f;
    }
}