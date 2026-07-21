using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameManager : MonoBehaviourPun
{
    [Header("UI System")]
    public GameObject endCanvas;
    public TMP_Text winnerText;

    void Start()
    {
        if (endCanvas != null) 
        {
            endCanvas.SetActive(false); // Versteckt das UI beim Start
        }
    }

    [PunRPC]
    public void RPC_ShowEndScreen(string message)
    {
        if (endCanvas != null) endCanvas.SetActive(true);
        if (winnerText != null) winnerText.text = "ROUND OVER\n" + message;
    }
}