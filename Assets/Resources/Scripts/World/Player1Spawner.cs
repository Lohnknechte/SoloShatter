using UnityEngine;
using Photon.Pun;

public class Player1Spawner : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // Da wir durch die Lobby bereits im Raum sind, spawnen wir den Stickman sofort!
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("Spawner aktiv: Erzeuge New SkeletonDataAsset...");
            // Wenn dein Prefab im Resources-Ordner "Player" heißt:
            PhotonNetwork.Instantiate("Player", spawnPoint.position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Fehler: Spawner wurde gestartet, aber wir sind in keinem Photon-Raum!");
        }
    }
}