using UnityEngine;
using UnityEngine.EventSystems;

public class BotonMover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Movimiento")]
    public float distancia = 50f;
    public bool moverDerecha = true;
    public float velocidad = 10f;

    [Header("Escalado al pasar el mouse (opcional)")]
    [Tooltip("Habilita o deshabilita el escalado al pasar el cursor por encima.")]
    [SerializeField] private bool habilitarEscala = false;
    [Tooltip("Multiplicador de escala aplicado al hacer hover (1 = sin cambio).")]
    [SerializeField] private float escalaMultiplicador = 1.15f;
    [Tooltip("Velocidad de interpolación de la escala.")]
    [SerializeField] private float velocidadEscala = 10f;

    private RectTransform rectTransform;
    private Vector2 posicionInicial;
    private Vector2 posicionObjetivo;

    private Vector3 escalaInicial;
    private Vector3 escalaObjetivo;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        posicionInicial = rectTransform.anchoredPosition;
        posicionObjetivo = posicionInicial;

        escalaInicial = rectTransform.localScale;
        escalaObjetivo = escalaInicial;
    }

    void Update()
    {
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            posicionObjetivo,
            Time.deltaTime * velocidad
        );

        if (habilitarEscala)
        {
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                escalaObjetivo,
                Time.deltaTime * velocidadEscala
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (moverDerecha)
            posicionObjetivo = posicionInicial + Vector2.right * distancia;
        else
            posicionObjetivo = posicionInicial + Vector2.left * distancia;

        if (habilitarEscala)
        {
            escalaObjetivo = escalaInicial * escalaMultiplicador;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        posicionObjetivo = posicionInicial;

        if (habilitarEscala)
        {
            escalaObjetivo = escalaInicial;
        }
    }
}