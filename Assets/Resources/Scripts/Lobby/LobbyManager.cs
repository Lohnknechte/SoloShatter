using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public TMP_InputField roomInput;   
    public Button createButton;        
    public Button joinButton;          
    public TMP_Text statusText;        

    [Header("Scene Settings")]
    public string gameplaySceneName = "KampfSzene"; 

    void Start()
    {
        if (createButton == null || joinButton == null || statusText == null)
        {
            Debug.LogError("FEHLER: Bitte ziehe die Buttons und den Status-Text im Inspector auf das LobbyManager-Objekt!");
            return;
        }

        // Knöpfe sofort ausschalten beim Start!
        createButton.interactable = false;
        joinButton.interactable = false;

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        statusText.text = "Verbinde mit Server...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (statusText != null) statusText.text = "Verbunden! Lobby bereit.";
        
        // Erst JETZT werden die Knöpfe freigeschaltet
        if (createButton != null) createButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;
        
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        // DOPPELTER SCHUTZ: Wenn wir noch gar nicht bereit sind, brechen wir sauber ab
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            if (statusText != null) statusText.text = "Bitte warten... Verbindung wird aufgebaut.";
            Debug.LogWarning("CreateRoom abgebrochen: Photon ist noch nicht bereit.");
            return;
        }

        Debug.Log(">>> DER CODE WURDE ERREICHT! CreateRoom läuft! <<<");

        string roomName = "StandardRaum";
        if (roomInput != null && !string.IsNullOrEmpty(roomInput.text))
        {
            roomName = roomInput.text;
        }

        if (statusText != null) statusText.text = "Erstelle Raum: " + roomName + "...";
        
        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            if (statusText != null) statusText.text = "Bitte warten... Verbindung wird aufgebaut.";
            return;
        }

        string roomName = "Play";
        if (roomInput != null && !string.IsNullOrEmpty(roomInput.text))
        {
            roomName = roomInput.text;
        }

        if (statusText != null) statusText.text = "Trete Raum bei: " + roomName + "...";
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        if (statusText != null) statusText.text = "Raum erfolgreich betreten! Lade Spiel...";
        
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(gameplaySceneName);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (statusText != null) statusText.text = "Beitreten fehlgeschlagen: " + message;
    }
}