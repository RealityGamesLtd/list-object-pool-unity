using UnityEngine;
using UnityEngine.EventSystems;
/// Credit srinivas sunil 
/// sourced from: https://bitbucket.org/ddreaper/unity-ui-extensions/pull-requests/21/develop_53/diff

/// <summary>
/// This is the most efficient way to handle scroll conflicts when there are multiple scroll rects, this is useful when there is a vertical scrollrect in/on a horizontal scrollrect or vice versa
/// Attach the script to the  rect scroll and assign other rectscroll in the inspecter (one is verticle and other is horizontal) gathered and modified from unity answers(delta snipper)
/// </summary>
namespace UI.Widget.Helpers
{
    [RequireComponent(typeof(CustomScrollRect))]
    [AddComponentMenu("UI/Extensions/Scrollrect Conflict Manager")]
    public class ScrollConflictManager : UIBehaviour
    {
        private DragHandler _parentDragHandler;
        private CustomScrollRect _parentScrollRect;
        private ScrollRectHelper _parentScrollRectHelper;
        private CustomScrollRect _myScrollRect;

        private bool scrollOther;
        private bool scrolledVerticaly;
        private bool scrolledHorizontaly;
        private PointerEventData _eventData;

        public bool IsDragged { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            _parentDragHandler = transform.parent.GetComponentInParent<DragHandler>();
            _parentScrollRect = transform.parent.GetComponentInParent<CustomScrollRect>();
            _parentScrollRectHelper = transform.parent.GetComponentInParent<ScrollRectHelper>();
            _myScrollRect = GetComponent<CustomScrollRect>();

            scrolledVerticaly = _myScrollRect.vertical;
            scrolledHorizontaly = _myScrollRect.horizontal;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_parentDragHandler)
            {
                _parentDragHandler.OnBeginDragged += OnBeginDrag;
                _parentDragHandler.OnEndDragged += OnEndDrag;
                _parentDragHandler.OnDragged += OnDrag;
            }
        }

        private void Update()
        {
            if (scrollOther)
            {
                _parentScrollRect.OnDrag(_eventData);
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_parentDragHandler)
            {
                _parentDragHandler.OnBeginDragged -= OnBeginDrag;
                _parentDragHandler.OnEndDragged -= OnEndDrag;
                _parentDragHandler.OnDragged -= OnDrag;
            }
        }

        private void ToggleParentScroll(PointerEventData eventData)
        {
            scrollOther = true;
            _myScrollRect.enabled = false;
            _parentScrollRect.OnBeginDrag(eventData);
            _parentScrollRectHelper.OnBeginDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragged = true;
            float horizontal = Mathf.Abs(eventData.position.x - eventData.pressPosition.x);
            float vertical = eventData.position.y - eventData.pressPosition.y;

            if (scrolledHorizontaly)
            {
                if (horizontal > Mathf.Abs(vertical))
                {
                    scrollOther = false;
                    _parentScrollRect.enabled = false;
                }
                else if (Mathf.Abs(vertical) > horizontal)
                {
                    ToggleParentScroll(eventData);
                }
            }
            else if (scrolledVerticaly)
            {
                if ((_myScrollRect.verticalNormalizedPosition >= (1f - Mathf.Epsilon) ||
                    _myScrollRect.content.anchoredPosition.y <= 0.01f) && vertical < 0)
                {
                    ToggleParentScroll(eventData);
                }
                else if (_myScrollRect.verticalNormalizedPosition <= Mathf.Epsilon && vertical > 0)
                {
                    ToggleParentScroll(eventData);
                }
                else if (Mathf.Abs(vertical) > horizontal)
                {
                    scrollOther = false;
                    _parentScrollRect.enabled = false;
                }
                else
                {
                    ToggleParentScroll(eventData);
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            _eventData = eventData;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            IsDragged = false;

            if (scrollOther)
            {
                scrollOther = false;
                _myScrollRect.enabled = true;
                _parentScrollRect.OnEndDrag(eventData);
                _parentScrollRectHelper.OnEndDrag(eventData);
            }
            else
            {
                _parentScrollRect.enabled = true;
            }
        }
    }
}
