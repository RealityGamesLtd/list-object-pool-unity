using System.Collections.Generic;
using UnityEngine;
using UI.Widget;
using ObjectPool.Static;
using System;

namespace UI.Utils
{
    [RequireComponent(typeof(CustomScrollRect))]
    public class CustomObjectPoolS : MonoBehaviour
    {
        public GameObject Prefab { get; private set; }

        private CustomScrollRect scrollRect;
        public CustomScrollRect ScrollRect
        {
            get
            {
                if (scrollRect == null) scrollRect = GetComponent<CustomScrollRect>();
                return scrollRect;
            }
        }

        public List<GameObject> GetElements => poolPrefabs;

        #region Data and setup fields
        private List<IPoolDataS> poolElementsData;
        private List<GameObject> poolPrefabs = new List<GameObject>();
        private int poolSize = 0;
        private float poolElementHeight;

        private int offsetFirstPrefab = 0;
        private int offsetLastPrefab = 0;

        private float contentHeightOffset = 0;

        private float viewportHeight;

        private int firstVisibleElementIndex = 0; //index of first element which is active
        private int lastVisibleElementIndex = 0;  //index of last element which is active

        private double scrollPosition = 0; // updates every element pool height reach

        private Action<GameObject, IPoolDataS> callbackOnSpawn;
        /// <summary>
        /// this is not called when method ReturnAllToPool is called
        /// </summary>
        public event Action<GameObject, IPoolDataS> OnDeactivateSinglePoolElement = delegate { };
        public event Action<int> OnSpawned = delegate { };
        #endregion

        #region Public methods
        public long CountDataElements()
        {
            return poolElementsData?.Count ?? 0;
        }

        public int GetCursorPosition()
        {
            if (poolElementsData?.Count < 1) return 0;

            int cursorPosition = (int)Math.Floor(ScrollRect.content.anchoredPosition.y / poolElementHeight);

            return cursorPosition;
        }

        public void Setup(List<IPoolDataS> dataList, Action<GameObject, IPoolDataS> callbackOnSpawn, int cursorPosition)
        {
            Setup(dataList, callbackOnSpawn);

            if (cursorPosition >= dataList.Count) cursorPosition = dataList.Count - 1;
            if (cursorPosition < 0) cursorPosition = 0;

            var newContentPos = new Vector2(ScrollRect.content.anchoredPosition.x, cursorPosition * poolElementHeight);
            ScrollRect.content.anchoredPosition = newContentPos;
            PoolPrefabs();
        }

        public void SetupWithCurrentScrollPosition(List<IPoolDataS> dataList, Action<GameObject, IPoolDataS> callbackOnSpawn)
        {
            float yScrollPos = 0;
            if (scrollRect != null)
                yScrollPos = ScrollRect.content.anchoredPosition.y;

            Setup(dataList, callbackOnSpawn);

            var newContentPos = new Vector2(ScrollRect.content.anchoredPosition.x, yScrollPos);
            ScrollRect.content.anchoredPosition = newContentPos;
            PoolPrefabs();
        }

        /// <summary>
        /// Display prefabs on screen from pool and attatch event to scroll. 
        /// </summary>
        /// <param name="dataList">List<PoolElement></param>
        /// <param name="callbackOnSpawn">Action<PoolElement> called when poolElement is spawned</param>
        public void Setup(List<IPoolDataS> dataList, Action<GameObject, IPoolDataS> callbackOnSpawn = null)
        {
            if(dataList == null || dataList.Count < 1)
            {
                Clear();
                OnSpawned?.Invoke(0);
                return;
            }

            this.callbackOnSpawn = callbackOnSpawn;
            ScrollRect.onValueChanged.RemoveListener(HandleScrolling);

            if (ScrollRect.viewport == null)
            {
                Debug.LogError("Viewport is null in custom scroll rect");
                OnSpawned?.Invoke(0);
                return;
            }
            
            RectTransform rectTransform = GetComponent<RectTransform>();
            viewportHeight = ScrollRect.viewport.GetComponent<RectTransform>().rect.height;

            #region Check if prefab is null
            if (Prefab == null)
            {
                Debug.LogError("Setup ObjectPooling - poolElement field is null. Script which uses ObjectPooling should set prefab to spawn in pool. To do that use SetPoolElement method.");
                ScrollRect.content.sizeDelta = new Vector2(ScrollRect.content.sizeDelta.x, 0);
                OnSpawned?.Invoke(0);
                return;
            }
            #endregion
            ReturnAllToPool();
            ScrollRect.verticalNormalizedPosition = 1;
            poolElementsData = new List<IPoolDataS>(dataList);
            OnSpawned?.Invoke(poolElementsData.Count);

            if (poolElementsData.Count > 0)
            {
                #region Setup indexes to data
                int index = 0;
                foreach (IPoolDataS poolData in poolElementsData)
                {
                    poolData.PoolElementIndex = index;
                    index++;
                }
                #endregion

                #region Enable proper prefabs
                int displayedPrefabs = 0;

                while (poolElementsData.Count > displayedPrefabs && displayedPrefabs * poolElementHeight < viewportHeight + offsetLastPrefab * poolElementHeight)
                {
                    try
                    {
                        SetupPoolPrefab(poolElementsData[displayedPrefabs]);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                    displayedPrefabs++;
                }

                firstVisibleElementIndex = 0;
                lastVisibleElementIndex = (displayedPrefabs - 1 > firstVisibleElementIndex) ? displayedPrefabs - 1 : firstVisibleElementIndex;
                #endregion

                float contentHeight = poolElementsData.Count * poolElementHeight;

                ScrollRect.onValueChanged.AddListener(HandleScrolling);
                ScrollRect.content.sizeDelta = new Vector2(ScrollRect.content.sizeDelta.x, contentHeight + contentHeightOffset);
            }
            else
            {
                ScrollRect.content.sizeDelta = new Vector2(ScrollRect.content.sizeDelta.x, 0);
            }
        }

        /// <summary>
        /// !IMPORTANT this value should be set before calling setup.
        /// Sets offset to content height
        /// </summary>
        public void SetContentHeightOffset(float offset)
        {
            contentHeightOffset = offset;
        }

        public void AppendData(List<IPoolDataS> dataList)
        {
            if (poolElementsData?.Count < 1)
            {
                Debug.LogError($"Cannot append data if pool has not been setup!");
                return;
            }

            int index = poolElementsData.Count;
            foreach (var poolData in dataList)
            {
                poolData.PoolElementIndex = index;
                index++;
            }

            poolElementsData.AddRange(dataList);

            var contentHeight = poolElementsData.Count * poolElementHeight;
            ScrollRect.content.sizeDelta = new Vector2(ScrollRect.content.sizeDelta.x, contentHeight);
        }

        private void HandleScrolling(Vector2 scrollRectVector)
        {
            if (ScrollRect.velocity.sqrMagnitude < .1f)
                ScrollRect.StopMovement();
            else
                PoolPrefabs();
        }

        /// <summary>
        /// Set prefab to spawn in pool
        /// </summary>
        /// <param name="poolElement">GameObject prefab which inherit from PoolElement script</param>
        /// <param name="poolSize">int poolSize (default is 10)</param>
        public void SetPoolElement(GameObject poolElement, int poolSize = 1, float poolElementHeight = 0)
        {
            Prefab = poolElement;
            this.poolSize = poolSize;

            if (poolElementHeight > 0)
                this.poolElementHeight = poolElementHeight;
            else
                this.poolElementHeight = Prefab.GetComponent<RectTransform>().sizeDelta.y;
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
            foreach (GameObject poolElement in poolPrefabs)
            {
                if (poolElement != null) poolElement.SetActive(false);
            }
        }

        /// <summary>
        /// Clear pool list. Return all elements to pool and clear data list.
        /// </summary>
        public void Clear()
        {
            ReturnAllToPool(); 
            ScrollRect.content.sizeDelta = new Vector2(ScrollRect.content.sizeDelta.x, 0);
            poolElementsData = new List<IPoolDataS>();
        }

        /// <summary>
        /// Returns amount of pool elements which are visible on screen at the same time
        /// </summary>
        public int GetAmountOfVisibleElementsInViewport()
        {
            return Mathf.CeilToInt(viewportHeight / poolElementHeight);
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
            double currentScrollPosition = ScrollRect.content.anchoredPosition.y;
            // if (scrollPosition > currentScrollPosition && currentScrollPosition < 0 || scrollPosition < currentScrollPosition && currentScrollPosition > poolElementsData.Count * poolElementHeight - viewportHeight)
            //     return;

            scrollPosition = currentScrollPosition;

            int oldFirstIndex = firstVisibleElementIndex;
            int oldLastIndex = lastVisibleElementIndex;

            #region Set firstVisibleElementIndex value
            firstVisibleElementIndex = (int)(Mathf.Abs((float)scrollPosition) / poolElementHeight) - offsetFirstPrefab;

            if (firstVisibleElementIndex >= poolElementsData.Count) firstVisibleElementIndex = poolElementsData.Count - 1;
            if (firstVisibleElementIndex < 0) firstVisibleElementIndex = 0;
            #endregion

            #region Set lastVisibleElementIndex value\
            lastVisibleElementIndex = Mathf.FloorToInt((Mathf.Abs((float)scrollPosition) + viewportHeight) / poolElementHeight) + offsetLastPrefab;

            if (lastVisibleElementIndex >= poolElementsData.Count) lastVisibleElementIndex = poolElementsData.Count - 1;
            if (lastVisibleElementIndex < 0) lastVisibleElementIndex = 0;
            #endregion

            //SCROLL DOWN
            if (lastVisibleElementIndex > oldLastIndex || oldFirstIndex < firstVisibleElementIndex)
            {
                #region Return to pool
                for (int i = oldFirstIndex; i < firstVisibleElementIndex; i++)
                {
                    if(i < poolElementsData.Count && i >= 0)
                        ReturnToPool(poolElementsData[i]);
                }
                #endregion

                #region Activate from pool
                //+1 to ommit visible element and prevent activating active item
                for (int i = oldLastIndex + 1; i <= lastVisibleElementIndex; i++)
                {
                    if (i < poolElementsData.Count && i >= 0)
                        SetupPoolPrefab(poolElementsData[i]);
                }
                #endregion
            }
            //SCROLL UP
            else if (lastVisibleElementIndex < oldLastIndex || oldFirstIndex > firstVisibleElementIndex)
            {
                #region Return to pool
                for (int i = oldLastIndex; i > lastVisibleElementIndex; i--)
                {
                    if (i < poolElementsData.Count && i >= 0)
                        ReturnToPool(poolElementsData[i]);
                }
                #endregion

                #region Activate from pool
                //-1 to ommit visible element and prevent activating active item
                for (int i = oldFirstIndex - 1; i >= firstVisibleElementIndex; i--)
                {
                    if (i < poolElementsData.Count && i >= 0)
                        SetupPoolPrefab(poolElementsData[i]);
                }
                #endregion
            }

        }

        // Return not active poolElement from pool
        private GameObject GetFromPool()
        {
            foreach (GameObject poolElement in poolPrefabs)
            {
                if (poolElement == null) continue;
                if (!poolElement.activeInHierarchy)
                {
                    return poolElement;
                }
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject newPrefabElement = Instantiate(Prefab, ScrollRect.content, false);
                newPrefabElement.SetActive(false);
                poolPrefabs.Add(newPrefabElement);
            }
            poolSize = poolPrefabs.Count;
            return GetFromPool();
        }

        // Deactivate pool element
        private void ReturnToPool(IPoolDataS poolElement)
        {
            foreach (GameObject prefab in poolPrefabs)
            {
                if (poolElement.PoolElementIndex == prefab.GetComponent<PoolPrefabS>().PoolElementIndex)
                {
                    OnDeactivateSinglePoolElement?.Invoke(prefab, poolElement);
                    prefab.SetActive(false);
                }
            }
        }

        // Check if pool element is currently active. If not then activate it and setup
        private void SetupPoolPrefab(IPoolDataS poolElementData)
        {
            foreach (GameObject prefabFromPool in poolPrefabs)
            {
                if (prefabFromPool.activeInHierarchy && prefabFromPool.GetComponent<PoolPrefabS>().PoolElementIndex == poolElementData.PoolElementIndex)
                {
                    Debug.LogWarning("You try to activate currently active pool element with index " + poolElementData.PoolElementIndex);
                    return;
                }
            }

            #region Activate prefab
            GameObject prefab = GetFromPool();
            if(prefab != null)
            {
                prefab.SetActive(true);
                prefab.GetComponent<PoolPrefabS>().SetIndex(poolElementData.PoolElementIndex);
                callbackOnSpawn?.Invoke(prefab, poolElementData);
                RectTransform prefabRT = prefab.GetComponent<RectTransform>();
                prefabRT.anchoredPosition = new Vector2(prefabRT.anchoredPosition.x, -(poolElementData.PoolElementIndex * poolElementHeight));
            }
            #endregion
        }

        #region Check prefabs offsets if can be applied
        private int CheckFirstIndexWithOffset(int offsetFirstPrefab)
        {
            if (offsetFirstPrefab <= 0 || firstVisibleElementIndex + 1 >= poolElementsData.Count)
            {
                return 0;
            }

            if (firstVisibleElementIndex + offsetFirstPrefab + 1 <= poolElementsData.Count - 1)
            {
                return offsetFirstPrefab;
            }
            else
            {
                return CheckFirstIndexWithOffset(offsetFirstPrefab - 1);
            }
        }

        private int CheckLastIndexWithOffset(int offsetLastPrefab)
        {
            if (offsetLastPrefab <= 0 || lastVisibleElementIndex - 1 <= firstVisibleElementIndex)
            {
                return 0;
            }

            if (lastVisibleElementIndex - 1 - offsetLastPrefab > firstVisibleElementIndex)
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