
using UnityEngine;
using System.Collections;

public class TrampaPisoAbatible : MonoBehaviour
{
    [Header("Placas del piso")]
    [SerializeField] private Transform placaIzquierda;
    [SerializeField] private Transform placaDerecha;

    [Header("Configuracion de apertura")]
    [SerializeField] private float distanciaApertura = 2f;
    [SerializeField] private float duracionMovimiento = 1f;
    [SerializeField] private float tiempoAbierta = 3f;

    [Header("Opciones")]
    [SerializeField] private bool cerrarAutomaticamente = true;
    [SerializeField] private bool activarUnaSolaVez = false;

    private Vector3 posicionInicialIzquierda;
    private Vector3 posicionInicialDerecha;

    private bool activada = false;
    private bool usada = false;

    private Coroutine rutina;

    private void Awake()
    {
        posicionInicialIzquierda = placaIzquierda.localPosition;
        posicionInicialDerecha = placaDerecha.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (activada)
            return;

        if (activarUnaSolaVez && usada)
            return;

        rutina = StartCoroutine(AbrirTrampa());
    }

    private IEnumerator AbrirTrampa()
    {
        activada = true;
        usada = true;

        Vector3 destinoIzquierda = posicionInicialIzquierda +
            Vector3.left * distanciaApertura;

        Vector3 destinoDerecha = posicionInicialDerecha +
            Vector3.right * distanciaApertura;

        float tiempo = 0f;

        while (tiempo < duracionMovimiento)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(tiempo / duracionMovimiento);

            placaIzquierda.localPosition = Vector3.Lerp(
                posicionInicialIzquierda, destinoIzquierda, t);

            placaDerecha.localPosition = Vector3.Lerp(
                posicionInicialDerecha, destinoDerecha, t);

            yield return null;
        }

        placaIzquierda.localPosition = destinoIzquierda;
        placaDerecha.localPosition = destinoDerecha;

        if (cerrarAutomaticamente)
        {
            yield return new WaitForSeconds(tiempoAbierta);

            tiempo = 0f;

            while (tiempo < duracionMovimiento)
            {
                tiempo += Time.deltaTime;

                float t = Mathf.Clamp01(tiempo / duracionMovimiento);

                placaIzquierda.localPosition = Vector3.Lerp(
                    destinoIzquierda, posicionInicialIzquierda, t);

                placaDerecha.localPosition = Vector3.Lerp(
                    destinoDerecha, posicionInicialDerecha, t);

                yield return null;
            }

            placaIzquierda.localPosition = posicionInicialIzquierda;
            placaDerecha.localPosition = posicionInicialDerecha;
        }

        activada = false;
    }
}
