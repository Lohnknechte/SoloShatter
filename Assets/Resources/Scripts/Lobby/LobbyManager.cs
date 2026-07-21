using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Panels")]
    public GameObject mainPanel;        
    public GameObject roomPanel;        

    [Header("Main Menu UI")]
    public TMP_InputField roomInput;   
    public Button createButton;        
    public Button joinButton;          
    public TMP_Text statusText;        

    [Header("Room Panel UI")]
    public TMP_Text roomCodeText;       
    public TMP_Text playerListText;     
    public Button startGameButton;     

    [Header("Scene Settings")]
    public string gameplaySceneName = "KampfSzene"; 

    void Start()
    {
        mainPanel.SetActive(true);
        roomPanel.SetActive(false);

        createButton.interactable = false;
        joinButton.interactable = false;

        PhotonNetwork.AutomaticallySyncScene = true;

        // Beseitigt den TimeoutDisconnect-Fehler
        if (PhotonNetwork.IsConnectedAndReady)
        {
            OnConnectedToMaster();
        }
        else if (!PhotonNetwork.IsConnected)
        {
            if (statusText != null) statusText.text = "Verbinde mit Server...";
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        if (statusText != null) statusText.text = "Verbunden! Lobby bereit.";
        
        if (createButton != null) createButton.interactable = true;
        if (joinButton != null) joinButton.interactable = true;
        
        PhotonNetwork.JoinLobby();
    }

    public void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        string roomName = GenerateRandomRoomCode(6);
        if (roomInput != null && !string.IsNullOrEmpty(roomInput.text))
        {
            roomName = roomInput.text.ToUpper();
        }

        RoomOptions roomOptions = new RoomOptions { MaxPlayers = 4 };
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public void JoinRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        if (roomInput != null && !string.IsNullOrEmpty(roomInput.text))
        {
            PhotonNetwork.JoinRoom(roomInput.text.ToUpper());
        }
        else
        {
            if (statusText != null) statusText.text = "Bitte Raumcode eingeben!";
        }
    }

    public override void OnJoinedRoom()
    {
        mainPanel.SetActive(false);
        roomPanel.SetActive(true);

        roomCodeText.text = "Code: " + PhotonNetwork.CurrentRoom.Name;
        UpdatePlayerList();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        playerListText.text = "<b>Spieler:</b>\n";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string name = string.IsNullOrEmpty(player.NickName) ? "Player " + player.ActorNumber : player.NickName;
            playerListText.text += name + (player.IsMasterClient ? " (Host)" : "") + "\n";
        }

        startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(gameplaySceneName);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (statusText != null) statusText.text = "Fehler: " + message;
    }

    private string GenerateRandomRoomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[length];
        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[Random.Range(0, chars.Length)];
        }
        return new string(stringChars);
    }
}