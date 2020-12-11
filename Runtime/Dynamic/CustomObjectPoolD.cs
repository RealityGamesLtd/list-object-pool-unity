using System;
using System.Collections.Generic;
using System.Linq;
using UI.Widget;
using UnityEngine;
using UnityEngine.UI;

namespace ObjectPool.Dynamic
{
    /// <summary>
    /// FOR PREFABS WITH DYNAMIC HEIGHT
    /// Script to spawn only a few prefabs to display huge lists of data. Must be attatched to Unity ScrollView GameObject. 
    /// Should be setted up in some kind of controller in following steps:
    /// 1. Use "SetPoolElement" to setup prefab which will be spawned in scroll list. Prefab must have attatched script ObjectPool.Dynamic PoolPrefab.
    ///    Optionally second param is to set pool start size. (default is 10);
    /// 2. Use "Setup" to spawn first prefabs on list. First parameter must be list of data which implements fields from ObjectPool.Dynamic IPoolElement interface. 
    ///    Second parameter is to attatch method which will be called when prefab is instantiated.
    /// 3. ADDITIONAL - There is methods to: 
    ///     - "ReturnAllToPool" - deactivate all prefabs
    ///     - "SetPrefabsOffset" - set offset prefabs (which will be not returned from pool when are out of view)
    /// </summary>
    [RequireComponent(typeof(CustomScrollRect))]
    public class CustomObjectPoolD : MonoBehaviour
    {
        // This field should be setted by script using ObjectPooling script (use method SetPoolElement). 
        // Prefab must be GameObject with script which inherit from PoolElement script
        private GameObject prefab;
        private CustomScrollRect scrollRect;
        public CustomScrollRect ScrollRectElement
        {
            get
            {
                if (scrollRect == null) scrollRect = GetComponent<CustomScrollRect>();
                return scrollRect;
            }
            private set
            {
                scrollRect = value;
            }
        }

        #region Data and setup fields
        private List<IPoolDataD> poolElementsData;
        private List<GameObject> poolPrefabs = new List<GameObject>();
        private int poolSize = 0;
        private int poolElementsDataLastIndex;

        private int offsetFirstPrefab = 0;
        private int offsetLastPrefab = 0;

        private float viewportHeight;
        private double scrollPosition;

        private int firstVisibleElementIndex = 0; //index of first element which is active
        private int lastVisibleElementIndex = 0;  //index of last element which is active

        Action<GameObject, IPoolDataD> callbackOnSpawn;
        /// <summary>
        /// this is not called when method ReturnAllToPool is called
        /// </summary>
        public event Action<GameObject, IPoolDataD> OnDeactivateSinglePoolElement = delegate { };
        #endregion

        public void Awake()
        {
            ScrollRectElement = GetComponent<CustomScrollRect>();
        }

        #region Public methods
        /// <summary>
        /// Display prefabs on screen from pool and attatch event to scroll. 
        /// </summary>
        /// <param name="dataList">List<PoolElement></param>
        /// <param name="callbackOnSpawn">Action<PoolElement> called when poolElement is spawned</param>
        public void Setup(List<IPoolDataD> dataList, Action<GameObject, IPoolDataD> callbackOnSpawn = null)
        {
            if (dataList == null || !dataList.Any())
            {
                Debug.LogError("data liust is empty or null");
                return;
            }
            this.callbackOnSpawn = callbackOnSpawn;
            ScrollRectElement.onValueChanged.RemoveAllListeners();

            viewportHeight = Screen.height / GetComponentInParent<Canvas>().scaleFactor + GetComponent<RectTransform>().offsetMax.y - GetComponent<RectTransform>().offsetMin.y;

            #region Check if prefab is null
            if (prefab == null)
            {
                Debug.LogError("Setup ObjectPooling - poolElement field is null. Script which uses ObjectPooling should set prefab to spawn in pool. To do that use SetPoolElement method.");
                ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta = new Vector2(ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta.x, 0);
                return;
            }
            #endregion

            ReturnAllToPool();
            poolElementsData = new List<IPoolDataD>(dataList);
            poolElementsDataLastIndex = poolElementsData.Count - 1;

            if (poolElementsData.Count > 0)
            {
                #region Enable proper prefabs
                int displayedPrefabs = 0;

                while (poolElementsData.Count > displayedPrefabs && Mathf.Abs(poolElementsData[displayedPrefabs].PrefabVerticalPosition) <= viewportHeight)
                {
                    SetupPoolPrefab(poolElementsData[displayedPrefabs]);
                    displayedPrefabs++;
                }

                firstVisibleElementIndex = 0;
                lastVisibleElementIndex = (displayedPrefabs - 1 > firstVisibleElementIndex) ? displayedPrefabs - 1 : firstVisibleElementIndex;
                #endregion

                float contentHeight = Mathf.Abs(poolElementsData[poolElementsDataLastIndex].PrefabVerticalPosition) + poolElementsData[poolElementsDataLastIndex].PrefabHeight;

                ScrollRectElement.onValueChanged.AddListener((Vector2 scrollRectVector) =>
                {
                    if (ScrollRectElement.velocity.sqrMagnitude < .1f)
                        ScrollRectElement.StopMovement();
                    else
                    {
                        PoolPrefabs();
                    }
                });
                ScrollRectElement.content.sizeDelta = new Vector2(ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta.x, contentHeight);
            }
            else
            {
                ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta = new Vector2(ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta.x, 0);
            }
        }

        /// <summary>
        /// Set prefab to spawn in pool
        /// </summary>
        /// <param name="poolElement">GameObject prefab which inherit from PoolElement script</param>
        /// <param name="poolSize">int poolSize (default is 10)</param>
        public void SetPoolElement(GameObject poolElement, int poolSize = 1)
        {
            prefab = poolElement;
            this.poolSize = poolSize;
        }

        /// <summary>
        /// Set prefabs offset after which prefabs will be deactivated
        /// </summary>
        /// <param name="offsetPrefabs"></param>
        public void SetPrefabsOffset(int offsetPrefabs = 0)
        {
            offsetFirstPrefab = offsetLastPrefab = offsetPrefabs;
        }

        /// <summary>
        /// Deactivate all pool objects in pool and scroll to top.
        /// </summary>
        public void ReturnAllToPool()
        {
            // FIX: scrolling to the top should be done via setting content local position y to 0, setting verticalNormalizedPosition on scroll rect will rebuild layout
            ScrollRectElement.content.localPosition = new Vector3(ScrollRectElement.content.localPosition.x, 0, ScrollRectElement.content.localPosition.z);
            //ScrollRectElement.verticalNormalizedPosition = 1;
            foreach (GameObject poolElement in poolPrefabs)
            {
                poolElement.SetActive(false);
            }
        }

        /// <summary>
        /// Clear pool list. Return all elements to pool and clear data list.
        /// </summary>
        public void Clear()
        {
            ReturnAllToPool();
            ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta = new Vector2(ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta.x, 0);
            poolElementsData = new List<IPoolDataD>();
        }
        #endregion

        #region Local methods
        /// <summary> 
        /// METHOD INVOKED ON SCROLL - Must be added to Listener "onValueChange" of ScrollRect.
        /// Method disable prefabs which are out of the view and enable prefabs which should appear (while scrolling down and up).
        /// </summary>
        /// <param name="dataList">List<MarketplaceSingleData></param>
        /// <param name="offsetPrefabs">Number of prefabs which should not be deactivated when are out of screen.</param>
        private void PoolPrefabs()
        {
            double currentScrollPosition = ScrollRectElement.content.anchoredPosition.y;
            double bottomScrollPosition = currentScrollPosition + viewportHeight;

            //if (scrollPosition > currentScrollPosition && currentScrollPosition < 0 || scrollPosition < currentScrollPosition && bottomScrollPosition > ScrollRectElement.content.rect.height)
                //return;

            #region Scroll down
            if (scrollPosition < currentScrollPosition)
            {
                float offsetFirstElementPositionABS = Mathf.Abs(poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)].PrefabVerticalPosition);
                float offsetFirstElementHeight = poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)].PrefabHeight;

                while (currentScrollPosition > offsetFirstElementPositionABS + offsetFirstElementHeight)
                {
                    if (firstVisibleElementIndex < poolElementsDataLastIndex)
                    {
                        if (firstVisibleElementIndex < poolElementsData.Count && firstVisibleElementIndex >= 0)
                            ReturnToPool(poolElementsData[firstVisibleElementIndex]);
                        firstVisibleElementIndex++;
                        offsetFirstElementPositionABS = Mathf.Abs(poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)].PrefabVerticalPosition);
                        offsetFirstElementHeight = poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)].PrefabHeight;
                    }
                    else
                    {
                        break;
                    }
                }

                float offsetLastElementPositionABS = Mathf.Abs(poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)].PrefabVerticalPosition);
                float offsetLastElementHeight = poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)].PrefabHeight;

                while (bottomScrollPosition > offsetLastElementPositionABS + offsetLastElementHeight)
                {
                    if (lastVisibleElementIndex < poolElementsDataLastIndex)
                    {
                        lastVisibleElementIndex++;
                        if (lastVisibleElementIndex < poolElementsData.Count && lastVisibleElementIndex >= 0)
                            SetupPoolPrefab(poolElementsData[lastVisibleElementIndex]);
                        offsetLastElementPositionABS = Mathf.Abs(poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)].PrefabVerticalPosition);
                        offsetLastElementHeight = poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)].PrefabHeight;
                    }
                    else
                    {
                        break;
                    }
                }

                scrollPosition = currentScrollPosition;
            }
            #endregion

            #region Scroll up
            else if (scrollPosition > currentScrollPosition)
            {
                float offsetFirstElementPositionABS = Mathf.Abs(poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)].PrefabVerticalPosition);

                while (currentScrollPosition < offsetFirstElementPositionABS)
                {
                    if (firstVisibleElementIndex > 0)
                    {
                        firstVisibleElementIndex--;
                        if (firstVisibleElementIndex < poolElementsData.Count && firstVisibleElementIndex >= 0)
                            SetupPoolPrefab(poolElementsData[firstVisibleElementIndex]);
                        offsetFirstElementPositionABS = Mathf.Abs(poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)].PrefabVerticalPosition);
                    }
                    else
                    {
                        break;
                    }
                }

                float offsetLastElementPositionABS = Mathf.Abs(poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)].PrefabVerticalPosition);

                while (bottomScrollPosition < offsetLastElementPositionABS)
                {
                    if (lastVisibleElementIndex > 0)
                    {
                        if (lastVisibleElementIndex < poolElementsData.Count && lastVisibleElementIndex >= 0)
                            ReturnToPool(poolElementsData[lastVisibleElementIndex]);
                        lastVisibleElementIndex--;
                        offsetLastElementPositionABS = Mathf.Abs(poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)].PrefabVerticalPosition);
                    }
                    else
                    {
                        break;
                    }
                }

                scrollPosition = currentScrollPosition;
            }
            #endregion
        }

        // Return not active poolElement from pool
        private GameObject GetFromPool()
        {
            foreach (GameObject poolElement in poolPrefabs)
            {
                if (!poolElement.activeInHierarchy)
                {
                    return poolElement;
                }
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject newPrefabElement = Instantiate(prefab, ScrollRectElement.content, false);
                newPrefabElement.SetActive(false);
                poolPrefabs.Add(newPrefabElement);
            }
            poolSize = poolPrefabs.Count;
            return GetFromPool();
        }

        // Deactivate pool element
        private void ReturnToPool(IPoolDataD poolElement)
        {
            foreach (GameObject prefab in poolPrefabs)
            {
                if (poolElement.PoolElementId == prefab.GetComponent<IPoolDataD>().PoolElementId)
                {
                    OnDeactivateSinglePoolElement?.Invoke(prefab, poolElement);
                    prefab.SetActive(false);
                }
            }
        }

        // Check if pool element is currently active. If not then activate it and setup
        private void SetupPoolPrefab(IPoolDataD poolElementData)
        {
            foreach (GameObject prefabFromPool in poolPrefabs)
            {
                if (prefabFromPool.activeInHierarchy && prefabFromPool.GetComponent<PoolPrefabD>().PoolElementId == poolElementData.PoolElementId)
                {
                    Debug.LogWarning("You try to activate currently active pool element.");
                    return;
                }
            }

            #region Activate prefab
            GameObject prefab = GetFromPool();
            prefab.SetActive(true);
            prefab.GetComponent<PoolPrefabD>().Setup(poolElementData.PoolElementId, poolElementData.PrefabHeight, poolElementData.PrefabVerticalPosition);
            if (callbackOnSpawn != null) callbackOnSpawn(prefab, poolElementData);
            RectTransform prefabRT = prefab.GetComponent<RectTransform>();
            prefabRT.anchoredPosition = new Vector2(prefabRT.anchoredPosition.x, poolElementData.PrefabVerticalPosition);
            #endregion
        }

        #region Check prefabs offsets if can be applied
        //Scroll Down
        private int CheckFirstIndexWithOffset(int offsetFirstPrefab)
        {
            if (offsetFirstPrefab <= 0 || firstVisibleElementIndex >= poolElementsData.Count)
            {
                return 0;
            }

            if (firstVisibleElementIndex + offsetFirstPrefab <= poolElementsDataLastIndex)
            {
                return offsetFirstPrefab;
            }
            else
            {
                return CheckFirstIndexWithOffset(offsetFirstPrefab - 1);
            }
        }

        //Scroll Up
        private int CheckFirstIndexWithOffset2(int offsetFirstPrefab)
        {
            if (offsetFirstPrefab <= 0 || firstVisibleElementIndex >= poolElementsData.Count)
            {
                return 0;
            }

            if (firstVisibleElementIndex + offsetFirstPrefab <= poolElementsDataLastIndex)
            {
                return offsetFirstPrefab;
            }
            else
            {
                return CheckFirstIndexWithOffset(offsetFirstPrefab - 1);
            }
        }

        //Scroll Down
        private int CheckLastIndexWithOffset(int offsetLastPrefab)
        {
            if (offsetLastPrefab <= 0 || lastVisibleElementIndex <= firstVisibleElementIndex)
            {
                return 0;
            }

            if (lastVisibleElementIndex - offsetLastPrefab > firstVisibleElementIndex)
            {
                return offsetLastPrefab;
            }
            else
            {
                return CheckLastIndexWithOffset(offsetLastPrefab - 1);
            }
        }
        #endregion
        #endregion
    }
}