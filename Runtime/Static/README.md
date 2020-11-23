# STATIC OBJECT POOL for UNITY'S ScrollView
Script for displaying big lists by small amount of prefabs placed in pool (with the same height)

**NOTE:** All scripts and classes will be used from folder ```Static```. 

---

## USAGE 
Example on leaderboard list

1. Place all scripts into project directory inside assets folder.

2. Attatch object pool script to ScrollView GameObject on scene.

3. Attatch ScrollView with ObjectPool GameObject to script which will provide data and setup Object Pool script. (ex. some kind of controller script)

```
using ObjectPool.Static;

public ObjectPoolScript ObjectPooling;
```

4. Attatch script `PoolPrefab` to prefab which will be spawned in list.

5. Setup prefab which will be spawned in list

```
public LeaderBoardPrefab prefab; // Prefab GameObject with script LeaderboardPrefab which will be dragged in editor

public void Awake()  //It can be also Start method
{
    ObjectPooling.SetPoolElement(prefab.gameObject);
}
```

- First parameter is required and it is prefab which will be spawned in list. It must be of type GameObject. If it's not (for ex. it's prefab with script) then use `prefab.gameObject`.

- (*OPTIONAL*) Second parameter ``poolSize`` is for setting basic pool size from which objects will be taken when next data must be displayed. If it will be not enough then script will add next `poolSize` amount to entire pool. If it will be not setted by programmer then default value is 1 and if need value will be doubled from current poolSize.

- (*OPTIONAL*) Third parameter ``poolElementHeight`` is for setting pool element size in list. If not setted then height will be taken from ``poolElementPrefab`` GameObject

    ***This script should be called in start, awake method or anywhere before setting up ObjectPool script with data***


6. (*OPTIONAL*) Can set amount of prefabs which will stay active after going out of view. To do so use method `SetPrefabsOffset`. Default value (if not setted) is 0.  
**WARNING** - smaller offset is equal to better performance. 


7. Now data should be prepard and passed to ObjectPool script. First of all each single data from list must implement `IPoolElement` interface.

```
using ObjectPool.Static;

namespace DataObject.Leaderboard
{
    [Serializable]
    public class LeaderboardPlayerData: IPoolElement
    {
        public string PlayerID;
        public string Name;
        public long Score;
        public long Position;

        #region Field required by ObjectPooling script
        public int PoolElementIndex { get; set; }
        #endregion
    }
}
```

8. Then create method to setup prefab when it's spawned (displayed)

```
 public void SetupLeaderboardPlayerPrefab (GameObject playerPrefab, IPoolData playerData)
 {
    LeaderboardPlayerPrefab leaderboardPlayerPrefabScript = playerPrefab.GetComponent<LeaderboardPlayerPrefab>();

    leaderboardPlayerPrefabScript.ClickOnPlayer = OnClickPlayer;  // attatch action to prefab

    leaderboardPlayerPrefabScript.Setup(playerData as LeaderboardPlayerData); //call setup on prefab with data casted back to required data type
 }
```

9. Setup ObjectPool script with data.

```
ObjectPooling.Setup(ToPoolElements(LeaderboardPlayersList), SetupAuctionPrefab);

public List<IPoolElement> ToPoolElements(List<LeaderboardPlayerData> leaderboardPlayerDataList)
{
    return leaderboardPlayerDataList.Cast<IPoolElement>().ToList();
}
```

10. *ADDITIONAL* - LeaderBoardPrefab script

```
using System;
using UnityEngine;
using UnityEngine.UI;
using DataObject.Leaderboard;

    public class AuctionPrefab : MonoBehaviour
    {
        #region UI Elements
        public Text PlayerName;
        public Text PlayerScore;
        public Text PlayerPosition;
        #endregion

        public Action ClickOnPlayer;

        public void Setup(LeaderboardPlayerData data)
        {
           PlayerName.text = data.Name;
           PlayerScore.text = data.Score.ToString();
           PlayerPosition.text = data.Position.ToString();
           Button playerButton = GetComponent<Button>();
           if (Button != null)
           {
               playerButton.onClick.RemoveAllListeners();
               playerButton.onClick.AddListener(() => {ClickOnPlayer();})
           }
        }
}

```

## THAT'S ALL . ENJOY.

