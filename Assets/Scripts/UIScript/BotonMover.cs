using UnityEngine;
using UnityEngine.EventSystems;

public class BotonMover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Movimiento")]
    public float distancia = 50f;
    public bool moverDerecha = true;
    public float velocidad = 10f;

    private RectTransform rectTransform;
    private Vector2 posicionInicial;
    private Vector2 posicionObjetivo;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        posicionInicial = rectTransform.anchoredPosition;
        posicionObjetivo = posicionInicial;
    }

    void Update()
    {
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            posicionObjetivo,
            Time.deltaTime * velocidad
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (moverDerecha)
            posicionObjetivo = posicionInicial + Vector2.right * distancia;
        else
            posicionObjetivo = posicionInicial + Vector2.left * distancia;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        posicionObjetivo = posicionInicial;
    }
}