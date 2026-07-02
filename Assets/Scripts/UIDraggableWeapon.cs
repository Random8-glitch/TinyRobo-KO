using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIDraggableWeapon : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private float returnDuration = 0.5f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Coroutine returnCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        returnCoroutine = StartCoroutine(ReturnToOrigin());
    }

    private IEnumerator ReturnToOrigin()
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsedTime = 0f;

        while (elapsedTime < returnDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            rectTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                originalPosition,
                elapsedTime / returnDuration);

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        returnCoroutine = null;
    }
}