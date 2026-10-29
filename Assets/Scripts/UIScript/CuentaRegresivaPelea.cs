
using System.Collections;
using UnityEngine;
using TMPro;

public class CuentaRegresivaPelea : MonoBehaviour
{
    [Header("Interfaz")]
    [SerializeField] private TMP_Text textoCuenta;

    [Header("Cuenta regresiva")]
    [SerializeField] private float esperaInicial = 2f;
    [SerializeField] private int numeroInicial = 3;
    [SerializeField] private float duracionNumero = 1f;
    [SerializeField] private string textoFinal = "¡PELEA!";
    [SerializeField] private float duracionTextoFinal = 1f;

    [Header("Efectos de entrada")]
    [SerializeField] private float duracionEntrada = 0.3f;

    [Range(0f, 1f)]
    [SerializeField] private float opacidadInicial = 0f;

    [Range(0f, 1f)]
    [SerializeField] private float opacidadFinal = 1f;

    [SerializeField] private float escalaInicial = 0.5f;
    [SerializeField] private float escalaFinal = 1f;

    private Color colorOriginal;
    private Vector3 escalaOriginal;

    private void Awake()
    {
        if (textoCuenta != null)
        {
            colorOriginal = textoCuenta.color;
            escalaOriginal = textoCuenta.transform.localScale;
        }
    }

    private void Start()
    {
        Time.timeScale = 0f;

        if (textoCuenta != null)
            textoCuenta.gameObject.SetActive(false);

        StartCoroutine(IniciarCuentaRegresiva());
    }

    private IEnumerator IniciarCuentaRegresiva()
    {
        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, esperaInicial)
        );

        if (textoCuenta == null)
        {
            Debug.LogError(
                "CuentaRegresivaPelea: asigna el texto en el Inspector."
            );

            Time.timeScale = 1f;
            yield break;
        }

        textoCuenta.gameObject.SetActive(true);

        for (int i = numeroInicial; i > 1; i--)
        {
            textoCuenta.text = i.ToString();

            yield return StartCoroutine(
                MostrarConEntrada(duracionNumero)
            );
        }

        textoCuenta.text = textoFinal;

        yield return StartCoroutine(
            MostrarConEntrada(duracionTextoFinal)
        );

        textoCuenta.gameObject.SetActive(false);
        textoCuenta.color = colorOriginal;
        textoCuenta.transform.localScale = escalaOriginal;

        Time.timeScale = 1f;
    }

    private IEnumerator MostrarConEntrada(float duracionVisible)
    {
        Transform objetivo = textoCuenta.transform;

        // Estado inicial del efecto.
        Color color = colorOriginal;
        color.a = colorOriginal.a * opacidadInicial;
        textoCuenta.color = color;

        objetivo.localScale = escalaOriginal * escalaInicial;

        float duracion = Mathf.Max(0f, duracionEntrada);
        float tiempo = 0f;

        // Animar opacidad y tamaño simultáneamente.
        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;

            float progreso = duracion > 0f
                ? Mathf.Clamp01(tiempo / duracion)
                : 1f;

            // Suavizar la entrada.
            float suavizado = Mathf.SmoothStep(0f, 1f, progreso);

            color = colorOriginal;
            color.a = colorOriginal.a * Mathf.Lerp(
                opacidadInicial,
                opacidadFinal,
                suavizado
            );

            textoCuenta.color = color;

            objetivo.localScale = escalaOriginal * Mathf.Lerp(
                escalaInicial,
                escalaFinal,
                suavizado
            );

            yield return null;
        }

        // Asegurar el estado final exacto.
        color = colorOriginal;
        color.a = colorOriginal.a * opacidadFinal;
        textoCuenta.color = color;
        objetivo.localScale = escalaOriginal * escalaFinal;

        // La animación forma parte de la duración total del texto.
        float tiempoRestante = Mathf.Max(
            0f,
            duracionVisible - duracion
        );

        yield return new WaitForSecondsRealtime(tiempoRestante);
    }
}
