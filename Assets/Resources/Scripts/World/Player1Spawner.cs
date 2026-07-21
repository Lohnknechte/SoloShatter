using UnityEngine;
using Photon.Pun;

public class Player1Spawner : MonoBehaviour
{
    [Header("Spawn Einstellungen")]
    public Transform spawnPoint;
    
    [Tooltip("Der genaue Name des Prefabs im Ordner 'Assets/Resources/'")]
    public string playerPrefabName = "PlayerPrefab"; 

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // Spawnt den Player über das Netzwerk für alle!
            PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);
        }
    }
}