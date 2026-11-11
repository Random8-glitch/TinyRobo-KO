using System.Collections;
using UnityEngine;

public class AgitarCamara : MonoBehaviour
{
    [Header("Configuracion de agitacion")]
    [SerializeField] private float duracionAgitacion = 0.2f;
    [SerializeField] private float intensidadAgitacion = 0.15f;

    private Vector3 posicionLocalInicial;
    private Coroutine agitacionActual;

    private void Awake()
    {
        posicionLocalInicial = transform.localPosition;
    }

    public void Agitar()
    {
        if (agitacionActual != null)
        {
            StopCoroutine(agitacionActual);
        }

        agitacionActual = StartCoroutine(
            EjecutarAgitacion()
        );
    }

    private IEnumerator EjecutarAgitacion()
    {
        float duracion = Mathf.Max(
            0f,
            duracionAgitacion
        );

        float intensidad = Mathf.Max(
            0f,
            intensidadAgitacion
        );

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            tiempoTranscurrido += Time.unscaledDeltaTime;

            float porcentajeRestante =
                1f - tiempoTranscurrido / duracion;

            Vector3 desplazamiento =
                Random.insideUnitSphere *
                intensidad *
                porcentajeRestante;

            transform.localPosition =
                posicionLocalInicial + desplazamiento;

            yield return null;
        }

        transform.localPosition = posicionLocalInicial;
        agitacionActual = null;
    }
}