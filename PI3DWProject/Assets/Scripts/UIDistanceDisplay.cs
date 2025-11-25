using UnityEngine;

public class UIDistanceDisplay : MonoBehaviour
{
    public PlayerMovement player;
    public TMPro.TextMeshProUGUI text;    
    public string format = "Distance: {0:F1} m";

    float _startZ;

    void Awake()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerMovement>();
        }
        if (player != null)
        {
            _startZ = player.transform.position.z;
        }
    }

    void Update()
    {
        if (player == null || text == null) return;
        float distance = Mathf.Max(0f, player.transform.position.z - _startZ);
        text.text = string.Format(format, distance);
    }
}
