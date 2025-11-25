using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleGameEndOnHit : MonoBehaviour
{
    public string playerTag = "Player";
    [Tooltip("Optional UI root (Canvas) to show when the player dies.")]
    public GameObject deathCanvas;
    public GameObject DistanceRun;

    void Awake()
    {
        if (deathCanvas == null)
        {
            var allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGameObjects)
            {
                if (go.name == "GameoverPanel")
                {
                    deathCanvas = go;
                    break;
                }
            }
            foreach (var go in allGameObjects)
            {
                if (go.name == "DistanceRun")
                {
                    DistanceRun = go;
                    break;
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
        {
            EndGame();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            EndGame();
        }
    }

    void EndGame()
    {
        Time.timeScale = 0f;
        if (deathCanvas != null)
        {
            deathCanvas.SetActive(true);
            DistanceRun.transform.localPosition = new Vector3(0, -80, 0);
        }
        Debug.Log("Game Over: player hit obstacle " + name);
    }
}
