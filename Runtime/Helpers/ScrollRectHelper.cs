using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Widget.Helpers
{
    [RequireComponent(typeof(CustomScrollRect))]
    [AddComponentMenu("UI/Extensions/Scroll Rect Helper")]
    public class ScrollRectHelper : UIBehaviour
    {
        public event Action OnEndDragAction = delegate { };
        public event Action OnBeginDragAction = delegate { };

        [SerializeField]
        private bool horizontalScroll = true;

        [SerializeField]
        private bool verticalScroll = false;

        private CustomScrollRect _scrollRect;
        public CustomScrollRect ScrrollRect
        {
            get
            {
                if (_scrollRect == null)
                    _scrollRect = GetComponent<CustomScrollRect>();
                return _scrollRect;
            }
        }

        private DragHandler dragHandler;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            var handler = GetComponentInParent<DragHandler>();
            if (handler == null)
            {
                Debug.LogError($"No {nameof(DragHandler)} component in parent!");
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DestroyImmediate(this);
                };
            }
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            if (dragHandler)
            {
                dragHandler.OnBeginDragged += OnBeginDrag;
                dragHandler.OnEndDragged += OnEndDrag;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (dragHandler)
            {
                dragHandler.OnBeginDragged -= OnBeginDrag;
                dragHandler.OnEndDragged -= OnEndDrag;
            }
        }

        public void BlockScroll()
        {
            ScrrollRect.horizontal = false;
            ScrrollRect.vertical = false;
        }

        public void UnblockScroll()
        {
            ScrrollRect.horizontal = horizontalScroll;
            ScrrollRect.vertical = verticalScroll;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragAction();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragAction();
        }
    }
}