using UnityEngine;

public class Player1Spawner : MonoBehaviour
{
    [Header("Spawn Einstellungen")]
    public Transform spawnPoint;
    public GameObject playerPrefab;

    void Start()
    {
        Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}