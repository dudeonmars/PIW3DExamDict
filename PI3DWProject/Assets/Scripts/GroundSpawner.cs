using UnityEngine;

namespace PI3D.Running
{
    public class GroundSpawner : MonoBehaviour
    {
        public Transform player;
        public GameObject groundPrefab; // 20 units long along +Z
        public int segmentsAhead = 5;

        float segmentLength = 20f;
        int _nextIndex;

        void Start()
        {
            if (player == null)
            {
                var pm = Object.FindFirstObjectByType<PlayerMovement>();
                if (pm != null) player = pm.transform;
            }

            // Spawn initial segments in front of player
            if (player == null || groundPrefab == null) return;
            _nextIndex = 0;
            for (int i = 0; i < segmentsAhead; i++)
            {
                SpawnSegment();
            }
        }

        void Update()
        {
            if (player == null || groundPrefab == null) return;

            // When player crosses into next segment, spawn one more in front
            float thresholdZ = (_nextIndex - segmentsAhead + 1) * segmentLength;
            if (player.position.z > thresholdZ)
            {
                SpawnSegment();
            }
        }

        void SpawnSegment()
        {
            float z = _nextIndex * segmentLength;
            Instantiate(groundPrefab, new Vector3(-0.33f, -0.5f, z), Quaternion.identity, transform);
            _nextIndex++;
        }
    }
}
