using System;
using System.Collections.Generic;
using System.Linq;
using UI.Widget.Helpers;
using UnityEngine;

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
    public class ParametrizedCustomObjectPoolD<TData, TEnum> : MonoBehaviour
        where TData: IParametrizedPoolDataD<TEnum> 
        where TEnum: Enum
    {
        private CustomScrollRect scrollRect;

        public CustomScrollRect ScrollRectElement
        {
            get
            {
                if (scrollRect == null) scrollRect = GetComponent<CustomScrollRect>();
                return scrollRect;
            }
            private set { scrollRect = value; }
        }
        
        // Data and setup fields
        private List<TData> poolElementsData;
        private Dictionary<TEnum, Pool> poolByType = new Dictionary<TEnum, Pool>();

        private int poolElementsDataLastIndex;

        private int offsetFirstPrefab = 0;
        private int offsetLastPrefab = 0;

        private float viewportHeight;
        private double scrollPosition;

        private int firstVisibleElementIndex = 0; //index of first element which is active
        private int lastVisibleElementIndex = 0; //index of last element which is active

        /// <summary>
        /// this is not called when method ReturnAllToPool is called
        /// </summary>
        public event Action<ParametrizedPoolPrefabD<TData, TEnum>, TData> OnDeactivateSinglePoolElement = delegate { };
        public event Action<int> OnSpawned = delegate { };

        public virtual void Awake()
        {
            ScrollRectElement = GetComponent<CustomScrollRect>();
        }
        
        // Public methods

        /// <summary>
        /// Display prefabs on screen from pool and attatch event to scroll. 
        /// </summary>
        /// <param name="dataList">List<PoolElement></param>
        /// <param name="callbackOnSpawn">Action<PoolElement> called when poolElement is spawned</param>
        public void Setup(List<TData> dataList)
        {
            if (dataList == null || !dataList.Any())
            {
                OnSpawned?.Invoke(0);
                return;
            }

            ScrollRectElement.onValueChanged.RemoveAllListeners();

            if (ScrollRectElement.viewport == null)
            {
                Debug.LogError("Viewport is null in custom scroll rect");
                OnSpawned?.Invoke(0);
                return;
            }

            viewportHeight = ScrollRectElement.viewport.GetComponent<RectTransform>().rect.height;

            // Check if prefabs are null
            var typesWithNullPrefabs = poolByType
                .Where(poolData => poolData.Value.PoolItemPrefab == null)
                .Select(i => i.Key)
                .ToList();

            if (typesWithNullPrefabs.Count > 0)
            {
                Debug.LogError(
                    $"Setup ObjectPooling - PoolItemPrefab fields are null for {string.Join(", ", typesWithNullPrefabs)} types. Script which uses ObjectPooling should set prefab to spawn in pool. To do that use SetPoolElement method.");
                ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta = new Vector2(ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta.x, 0);
                
                OnSpawned?.Invoke(0);
                return;
            }

            var missedTypes = dataList
                .Select(i => i.PoolElementType)
                .Distinct()
                .Where(elementType => !poolByType.ContainsKey(elementType))
                .ToList();

            if (missedTypes.Count > 0)
            {
                Debug.LogError($"Setup ObjectPooling - Missed configs for prefab types: [{string.Join(", ", missedTypes)}]. Script which uses ObjectPooling should set prefabs to spawn in pool. To do that use SetPoolElement method.");
                ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta = new Vector2(ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta.x, 0);
                
                OnSpawned?.Invoke(0);
                return;
            }

            ReturnAllToPool();
            poolElementsData = new List<TData>(dataList);
            poolElementsDataLastIndex = poolElementsData.Count - 1;
            OnSpawned?.Invoke(poolElementsData.Count);

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
                lastVisibleElementIndex = (displayedPrefabs - 1 > firstVisibleElementIndex)
                    ? displayedPrefabs - 1
                    : firstVisibleElementIndex;

                #endregion

                float contentHeight = Mathf.Abs(poolElementsData[poolElementsDataLastIndex].PrefabVerticalPosition) + poolElementsData[poolElementsDataLastIndex].PrefabHeight;

                ScrollRectElement.onValueChanged.AddListener((Vector2 scrollRectVector) =>
                {
                    if (ScrollRectElement.velocity.sqrMagnitude < .1f) ScrollRectElement.StopMovement();
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
        /// <param name="setupPoolDataDict">Dictionary with PoolInitialization data by element type</param>
        public void SetPoolElements(Dictionary<TEnum, PoolInitializationData> setupPoolDataDict)
        {
            foreach (var setupPoolData in setupPoolDataDict)
            {
                if (!poolByType.TryGetValue(setupPoolData.Key, out var poolData))
                {
                    poolData = new Pool(this);
                    poolByType.Add(setupPoolData.Key, poolData);
                }

                poolData.PoolItemPrefab = setupPoolData.Value.PoolItemPrefab;
                poolData.PoolSize = setupPoolData.Value.InitPoolSize;
                poolData.CallbackOnSpawn = setupPoolData.Value.CallbackOnSpawn;
            }
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
            foreach (var typePoolPair in poolByType)
            {
                typePoolPair.Value.ReturnAllToPool();
            }
        }

        /// <summary>
        /// Clear pool list. Return all elements to pool and clear data list.
        /// </summary>
        public void Clear()
        {
            ReturnAllToPool();
            ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta = new Vector2(ScrollRectElement.content.GetComponent<RectTransform>().sizeDelta.x, 0);
            poolElementsData = new List<TData>();
        }

        // Local methods
        
        /// <summary>
        /// Return not active poolElement from pool
        /// </summary>
        /// <param name="poolElementData"></param>
        private ParametrizedPoolPrefabD<TData, TEnum> GetFromPool(TData poolElementData)
        {
            if (!poolByType.TryGetValue(poolElementData.PoolElementType, out var pool))
            {
                Debug.LogError($"GetFromPool - filed to get the pool of type ({poolElementData}) due to it wasn't provided during SetPoolElement initialization");
                return null;
            }

            return pool.GetItem();
        }

        // Deactivate pool element
        private void ReturnToPool(TData poolElementData)
        {
            if (!poolByType.TryGetValue(poolElementData.PoolElementType, out var pool))
            {
                Debug.LogError($"ReturnToPool - filed to get the pool of type ({poolElementData}) due to it wasn't provided during SetPoolElement initialization");
                return;
            }

            pool.ReturnToPool(poolElementData);
        }

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
                float offsetFirstElementPositionABS =
                    Mathf.Abs(poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)]
                        .PrefabVerticalPosition);
                float offsetFirstElementHeight =
                    poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)]
                        .PrefabHeight;

                while (currentScrollPosition > offsetFirstElementPositionABS + offsetFirstElementHeight)
                {
                    if (firstVisibleElementIndex < poolElementsDataLastIndex)
                    {
                        if (firstVisibleElementIndex < poolElementsData.Count && firstVisibleElementIndex >= 0)
                            ReturnToPool(poolElementsData[firstVisibleElementIndex]);
                        firstVisibleElementIndex++;
                        offsetFirstElementPositionABS =
                            Mathf.Abs(poolElementsData[
                                    firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)]
                                .PrefabVerticalPosition);
                        offsetFirstElementHeight =
                            poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)]
                                .PrefabHeight;
                    }
                    else
                    {
                        break;
                    }
                }

                float offsetLastElementPositionABS =
                    Mathf.Abs(poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)]
                        .PrefabVerticalPosition);
                float offsetLastElementHeight =
                    poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)].PrefabHeight;

                while (bottomScrollPosition > offsetLastElementPositionABS + offsetLastElementHeight)
                {
                    if (lastVisibleElementIndex < poolElementsDataLastIndex)
                    {
                        lastVisibleElementIndex++;
                        if (lastVisibleElementIndex < poolElementsData.Count && lastVisibleElementIndex >= 0)
                            SetupPoolPrefab(poolElementsData[lastVisibleElementIndex]);
                        offsetLastElementPositionABS =
                            Mathf.Abs(poolElementsData[
                                    lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)]
                                .PrefabVerticalPosition);
                        offsetLastElementHeight =
                            poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)]
                                .PrefabHeight;
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
                float offsetFirstElementPositionABS =
                    Mathf.Abs(poolElementsData[firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)]
                        .PrefabVerticalPosition);

                while (currentScrollPosition < offsetFirstElementPositionABS)
                {
                    if (firstVisibleElementIndex > 0)
                    {
                        firstVisibleElementIndex--;
                        if (firstVisibleElementIndex < poolElementsData.Count && firstVisibleElementIndex >= 0)
                            SetupPoolPrefab(poolElementsData[firstVisibleElementIndex]);
                        offsetFirstElementPositionABS =
                            Mathf.Abs(poolElementsData[
                                    firstVisibleElementIndex + CheckFirstIndexWithOffset(offsetFirstPrefab)]
                                .PrefabVerticalPosition);
                    }
                    else
                    {
                        break;
                    }
                }

                float offsetLastElementPositionABS =
                    Mathf.Abs(poolElementsData[lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)]
                        .PrefabVerticalPosition);

                while (bottomScrollPosition < offsetLastElementPositionABS)
                {
                    if (lastVisibleElementIndex > 0)
                    {
                        if (lastVisibleElementIndex < poolElementsData.Count && lastVisibleElementIndex >= 0)
                            ReturnToPool(poolElementsData[lastVisibleElementIndex]);
                        lastVisibleElementIndex--;
                        offsetLastElementPositionABS =
                            Mathf.Abs(poolElementsData[
                                    lastVisibleElementIndex - CheckLastIndexWithOffset(offsetLastPrefab)]
                                .PrefabVerticalPosition);
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

        // Check if pool element is currently active. If not then activate it and setup
        private void SetupPoolPrefab(TData poolElementData)
        {
            if (!poolByType.TryGetValue(poolElementData.PoolElementType, out var pool))
            {
                Debug.LogError($"SetupPoolPrefab - filed to get the pool of type ({poolElementData.PoolElementType}) due to it wasn't provided during SetPoolElement initialization");
                return;
            }

            foreach (var prefabFromPool in pool.PoolElements)
            {
                if (prefabFromPool.gameObject.activeInHierarchy &&
                    prefabFromPool.PoolElementId == poolElementData.PoolElementId)
                {
                    Debug.LogWarning("You try to activate currently active pool element.");
                    return;
                }
            }

            var prefab = GetFromPool(poolElementData);
            
            // Activate prefab
            prefab.gameObject.SetActive(true);
            prefab.Setup(poolElementData);
            pool.CallbackOnSpawn?.Invoke(prefab.GetComponent<ParametrizedPoolPrefabD<TData, TEnum>>(), poolElementData);
            RectTransform prefabRT = prefab.GetComponent<RectTransform>();
            prefabRT.anchoredPosition = new Vector2(prefabRT.anchoredPosition.x, poolElementData.PrefabVerticalPosition);
        }

        // Check prefabs offsets if can be applied
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

        public struct PoolInitializationData
        {
            public PoolInitializationData(ParametrizedPoolPrefabD<TData, TEnum> poolItemPrefab, int initPoolSize, Action<ParametrizedPoolPrefabD<TData, TEnum>, TData> callbackOnSpawn = null)
            {
                PoolItemPrefab = poolItemPrefab;
                InitPoolSize = initPoolSize;
                CallbackOnSpawn = callbackOnSpawn;
            }

            public ParametrizedPoolPrefabD<TData, TEnum> PoolItemPrefab;
            public int InitPoolSize;
            public Action<ParametrizedPoolPrefabD<TData, TEnum>, TData> CallbackOnSpawn;
        }

        private class Pool
        {
            public readonly List<ParametrizedPoolPrefabD<TData, TEnum>> PoolElements = new List<ParametrizedPoolPrefabD<TData, TEnum>>();
            private readonly ParametrizedCustomObjectPoolD<TData, TEnum> objectPoolList;

            public ParametrizedPoolPrefabD<TData, TEnum> PoolItemPrefab { get; set; }
            public int PoolSize { get; set; }
            public Action<ParametrizedPoolPrefabD<TData, TEnum>, TData> CallbackOnSpawn { get; set; }

            public Pool(ParametrizedCustomObjectPoolD<TData, TEnum> objectPoolList)
            {
                this.objectPoolList = objectPoolList;
            }

            // Return not active poolElement from pool
            public ParametrizedPoolPrefabD<TData, TEnum> GetItem()
            {
                foreach (var poolElement in PoolElements)
                {
                    if (!poolElement.gameObject.activeInHierarchy)
                    {
                        return poolElement;
                    }
                }

                for (int i = 0; i < PoolSize; i++)
                {
                    var newPrefabElement = Instantiate(PoolItemPrefab, objectPoolList.ScrollRectElement.content, false);
                    newPrefabElement.gameObject.SetActive(false);
                    PoolElements.Add(newPrefabElement);
                }

                PoolSize = PoolElements.Count;
                return GetItem();
            }

            /// <summary>
            /// Deactivate all pool objects in pool and scroll to top.
            /// </summary>
            public void ReturnAllToPool()
            {
                foreach (var poolElement in PoolElements)
                {
                    DisposeElement(poolElement);
                    poolElement.gameObject.SetActive(false);
                }
            }

            // Deactivate pool element
            public void ReturnToPool(TData poolElement)
            {
                foreach (var poolPrefabD in PoolElements)
                {
                    var prefab = poolPrefabD;
                    if (poolElement.PoolElementId == prefab.PoolElementId)
                    {
                        objectPoolList.OnDeactivateSinglePoolElement?.Invoke(prefab, poolElement);

                        DisposeElement(prefab);
                        prefab.gameObject.SetActive(false);
                    }
                }
            }

            /// <summary>
            /// Disposes element if is disposable
            /// </summary>
            /// <param name="poolPrefab">Element to dispose</param>
            private void DisposeElement(ParametrizedPoolPrefabD<TData, TEnum> poolPrefab)
            {
                var disposables = poolPrefab.GetComponents<IDisposable>();
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}