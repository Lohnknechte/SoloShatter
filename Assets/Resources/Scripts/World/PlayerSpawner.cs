using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform p1SpawnPoint;
    public Transform p2SpawnPoint;

    [Header("Prefab Settings")]
    [Tooltip("Der Name deiner Prefab-Datei im Assets/Resources/ Ordner")]
    public string playerPrefabName = "PlayerPrefab"; 

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        {
            // Der Host (1. Player) nimmt Spawn 1, der Beigetretene (2. Player) nimmt Spawn 2
            Transform spawnToUse = PhotonNetwork.IsMasterClient ? p1SpawnPoint : p2SpawnPoint;

            // Spawnt EXAKT 1 Charakter pro PC über das Netzwerk
            PhotonNetwork.Instantiate(playerPrefabName, spawnToUse.position, spawnToUse.rotation);
        }
    }
}