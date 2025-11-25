using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public Transform player;
    public PlayerMovement playerMovement;

    public GameObject blockerLane;
    public GameObject hurdleJump;
    public GameObject barSlide;

    public float spawnZOffset = 30f;
    public float spawnInterval = 1.2f;

    public int laneCount = 3;
    public float laneWidth = 2.5f;

    float _nextSpawnTime;

    void Start()
    {
        if (player == null)
        {
            var pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                player = pm.transform;
                playerMovement = pm;
            }
        }

        if (playerMovement != null)
        {
            laneCount = Mathf.Max(1, playerMovement.laneCount);
            laneWidth = playerMovement.laneWidth;
        }

        _nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        if (player == null) return;
        if (Time.time < _nextSpawnTime) return;

        SpawnSimpleObstacleRow();
        _nextSpawnTime = Time.time + spawnInterval;
    }

    void SpawnSimpleObstacleRow()
    {
        float z = player.position.z + spawnZOffset;
        int safeLane = Random.Range(0, laneCount); 

        for (int lane = 0; lane < laneCount; lane++)
        {
            if (lane == safeLane) continue;
    
            GameObject prefab = ChooseRandomPrefab();
            if (prefab == null) continue;

            float x = LaneToX(lane);
            Instantiate(prefab, new Vector3(x, 0f, z), Quaternion.identity, transform);
        }
    }

    GameObject ChooseRandomPrefab()
    {
        // Very simple random choice between available prefabs
        GameObject[] options = { blockerLane, hurdleJump, barSlide };
        int attempts = 0;
        while (attempts < options.Length)
        {
            int i = Random.Range(0, options.Length);
            if (options[i] != null) return options[i];
            attempts++;
        }
        return null;
    }

    float LaneToX(int laneIndex)
    {
        int halfSpan = (laneCount - 1) / 2;
        return (laneIndex - halfSpan) * laneWidth;
    }
}
