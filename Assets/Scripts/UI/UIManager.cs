using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    // --- Singleton Setup ---
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // --- UI Element References ---
    [Header("Player HUD Elements")]
    [SerializeField] private TextMeshProUGUI player1MoneyText;
    [SerializeField] private TextMeshProUGUI player2MoneyText;

    [Header("Duel Result Elements")]
    [SerializeField] private TextMeshProUGUI duelLogText;

    [Header("Build Panel")]
    [SerializeField] private GameObject buildPanel;

    // --- Public Methods ---

    public void UpdatePlayerMoney(PlayerController player)
    {
        // Based on the player's ID, update the correct text element
        if (player.playerId == 0)
        {
            player1MoneyText.text = $"Player 1: ${player.money}";
        }
        else if (player.playerId == 1)
        {
            player2MoneyText.text = $"Player 2: ${player.money}";
        }
    }

    public void LogDuelMessage(string message)
    {
        // This will display duel results on the screen
        Debug.Log(message); // We still log to the console for our own debugging
        duelLogText.text = message;
    }

    public void ShowBuildPanel()
    {
        buildPanel.SetActive(true);
    }

    public void HideBuildPanel()
    {
        buildPanel.SetActive(false);
    }

    public void OnBuildHouseClicked()
    {
        GameManager.Instance.PlayerChoseToBuild("House");
        HideBuildPanel();
    }

    public void OnBuildShopClicked()
    {
        GameManager.Instance.PlayerChoseToBuild("Shop");
        HideBuildPanel();
    }
}