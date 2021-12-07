using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Widget.Helpers
{
    [AddComponentMenu("UI/Drag Handler")]
    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class DragHandler : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public Action<PointerEventData> OnInitializePotentialDragged;
        public Action<PointerEventData> OnBeginDragged;
        public Action<PointerEventData> OnEndDragged;
        public Action<PointerEventData> OnDragged;

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragged?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDragged?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragged?.Invoke(eventData);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            OnInitializePotentialDragged?.Invoke(eventData);
        }
    }
}