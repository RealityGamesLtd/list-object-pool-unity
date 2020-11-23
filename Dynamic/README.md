# DYNAMIC OBJECT POOL for UNITY'S ScrollView
Script for displaying big lists by small amount of prefabs placed in pool (with different height)

**NOTE:** All scripts and classes will be used from folder ```Dynamic```. 

---

## USAGE 
Example on leaderboard list

1. Place all script into project directory inside assets folder.

2. Attatch object pool script to ScrollView GameObject on scene.

3. Attatch ScrollView with ObjectPool GameObject to script which will provide data and setup Object Pool script. (ex. some kind of controller script)

```
using ObjectPool.Dynamic;

public ObjectPoolScript ObjectPooling;
```

4. Attatch script `PoolPrefab` to prefab which will be spawned in list.

5. Setup prefab which will be spawned in list

```
public LeaderBoardPrefab prefab; // Prefab GameObject with script LeaderboardPrefab which will be dragged in  editor
//... (some code) ...
public void Awake() //also can be Start
{
    ObjectPooling.SetPoolElement(prefab.gameObject, poolSize: 5);
}
```

- First parameter is required and it is prefab which will be spawned in list. It must be of type GameObject. If it's not (for ex. it's prefab with script) then use `.gameObject`.

- Second parameter is optional and it's basic pool size from which objects will be taken when next data must be displayed. If it will be not enough then script will add next `poolSize` amount to entire pool. If it will be not setted by programmer then default value is 10.

    ***This script should be called in start, awake method or anywhere before setting up ObjectPool script with data***

6. (*OPTIONAL*) Can set amount of prefabs which will stay active after going out of view. To do so use method `SetPrefabsOffset`. Default value (if not setted) is 0.  
**WARNING** - less offset is equal to better performance. 

7. Now data should be prepard and passed to ObjectPool script. First of all each single data from list must implement `IPoolElement` interface.

```
using ObjectPool;

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
        public string PoolElementId { get; set; }
        public float PrefabHeight { get; set; }
        public float PrefabVerticalPosition { get; set; }
        #endregion
    }
}
```

8. When data object is prepared all data object must have those additional fields filled.

```
using System.Collections.Generic;

List<LeaderboardPlayerData> LeaderboardPlayersList;
// ... some code ... also code which will set PlayerList some data
float leaderboardHeight = 0;
float prefabHeight = 300; //this value should be specified separately for each single data

foreach (LeaderboardPlayerData leaderboardPlayerData in LeaderboardPlayersList)
{
   leaderboardPlayerData.PrefabHeight = prefabHeight;
   leaderboardPlayerData.PrefabVerticalPosition = -leaderboardHeight;
   leaderboardPlayerData.PoolElementId = leaderboardPlayer.PlayerID;
   leaderboardPlayerData += prefabHeight;     
}
```

*IMPORTANT* PrefabVerticalPosition must be negative content height value (in example leaderboardHeight). Because elements goes down in list.

9. Then create method to setup prefab when it's spawned (displayed)

```
public void SetupLeaderboardPrefab(GameObject leaderboardPrefabGameObject, IPoolElement leaderboardPlayerData)
{
    LeaderBoardPrefab leaderBoardPrefab = leaderboardPrefabGameObject.GetComponent<LeaderBoardPrefab>();
    leaderBoardPrefab.Setup(leaderboardPlayerData as LeaderboardPlayerData);
}
```

10. Setup ObjectPool script with data.

```
ObjectPooling.Setup(ToPoolElements(LeaderboardPlayersList), SetupAuctionPrefab);

public List<IPoolElement> ToPoolElements(List<LeaderboardPlayerData> leaderboardPlayerDataList)
{
    return leaderboardPlayerDataList.Cast<IPoolElement>().ToList();
}
```

11. *ADDITIONAL* - LeaderBoardPrefab script

```
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

        public void Setup(LeaderboardPlayerData data)
        {
           PlayerName.text = data.Name;
           PlayerScore.text = data.Score.ToString();
           PlayerPosition.text = data.Position.ToString();
        }
}

```

## THAT'S ALL . ENJOY.

