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
        //This tracks if the other one should be scrolling instead of the current one.
        private bool scrollOther;
        //This tracks wether the other one should scroll horizontally or vertically.
        private bool scrollOtherHorizontally;

        public bool IsDragged { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            _parentDragHandler = transform.parent.GetComponentInParent<DragHandler>();
            _parentScrollRect = transform.parent.GetComponentInParent<CustomScrollRect>();
            _parentScrollRectHelper = transform.parent.GetComponentInParent<ScrollRectHelper>();
            //Get the current scroll rect so we can disable it if the other one is scrolling
            _myScrollRect = GetComponent<CustomScrollRect>();
            //If the current scroll Rect has the vertical checked then the other one will be scrolling horizontally.
            scrollOtherHorizontally = _myScrollRect.vertical;
            //Check some attributes to let the user know if this wont work as expected
            if (scrollOtherHorizontally)
            {
                if (_myScrollRect.horizontal)
                    Debug.Log("You have added the SecondScrollRect to a scroll view that already has both directions selected");
                if (!_parentScrollRect.horizontal)
                    Debug.Log("The other scroll rect doesnt support scrolling horizontally");
            }
            else if (!_parentScrollRect.vertical)
            {
                Debug.Log("The other scroll rect doesnt support scrolling vertically");
            }
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

        //IBeginDragHandler
        public void OnBeginDrag(PointerEventData eventData)
        {
            IsDragged = true;
            //Get the absolute values of the x and y differences so we can see which one is bigger and scroll the other scroll rect accordingly
            float horizontal = Mathf.Abs(eventData.position.x - eventData.pressPosition.x);
            float vertical = Mathf.Abs(eventData.position.y - eventData.pressPosition.y);
            if (scrollOtherHorizontally)
            {
                if (horizontal > vertical)
                {
                    //disable the current scroll rect so it doesnt move.
                    ToggleParentScroll(eventData);
                }
                else if (vertical > horizontal)
                {
                    scrollOther = false;
                    _parentScrollRect.enabled = false;
                }
            }
            else if (vertical > horizontal)
            {
                //disable the current scroll rect so it doesnt move.
                ToggleParentScroll(eventData);
            }
        }

        private void ToggleParentScroll(PointerEventData eventData)
        {
            scrollOther = true;
            _myScrollRect.enabled = false;
            _parentScrollRect.OnBeginDrag(eventData);
            _parentScrollRectHelper.OnBeginDrag(eventData);
        }

        //IEndDragHandler
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

        private void Update()
        {
            if (scrollOther)
            {
                _parentScrollRect.OnDrag(_eventData);
            }
        }

        private PointerEventData _eventData;

        //IDragHandler
        public void OnDrag(PointerEventData eventData)
        {
            _eventData = eventData;
        }
    }
}
