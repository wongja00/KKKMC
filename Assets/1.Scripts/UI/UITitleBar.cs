using UnityEngine;
using UnityEngine.EventSystems;

public class UITitleBar : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private bool isDragging = false;
    private Vector2 dragOffset;
    [SerializeField] private RectTransform windowRectTransform;

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        Vector2 globalMousePos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out globalMousePos))
        {
            dragOffset = (Vector2)windowRectTransform.anchoredPosition - globalMousePos;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        Vector2 globalMousePos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out globalMousePos))
        {
            windowRectTransform.anchoredPosition = globalMousePos + dragOffset;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }
}
