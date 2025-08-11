using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // --- UI Element References ---
    [Header("Player HUD")]
    [SerializeField] private TextMeshProUGUI player1MoneyText;
    [SerializeField] private TextMeshProUGUI player2MoneyText;

    [Header("Duel Display")]
    [SerializeField] private TextMeshProUGUI player1DuelText; // For "Player 1: Rock (10)"
    [SerializeField] private TextMeshProUGUI player2DuelText; // For "Bot: Paper (8)"
    [SerializeField] private TextMeshProUGUI duelLogText;     // For "Player 1 Wins!" etc.

    [Header("Build Panel")]
    [SerializeField] private GameObject buildPanel;

    [Header("Buttons")]
    [SerializeField] private Button rollButton; // Reference to the main "Roll" button.

    // --- Unity Lifecycle Methods ---

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

    public void UpdatePlayerMoney(PlayerController player)
    {
        if (player.playerId == 0) // Player 1 (Human)
        {
            player1MoneyText.text = $"Player 1: ${player.money}";
        }
        else if (player.playerId == 1) // Player 2 (Bot)
        {
            player2MoneyText.text = $"Bot: ${player.money}";
        }
    }

    public void ShowDuelResults(DuelChoice p1Choice, DuelChoice p2Choice, int p1Sum, int p2Sum)
    {
        // Build the string for Player 1's result
        string p1Special = p1Choice.isSpecial ? " (S!)" : ""; // "S!" for Special
        player1DuelText.text = $"Player 1: {p1Choice.type}{p1Special} [{p1Sum}]";
        player1DuelText.gameObject.SetActive(true);

        // Build the string for Player 2's (Bot's) result
        string p2Special = p2Choice.isSpecial ? " (S!)" : "";
        player2DuelText.text = $"Bot: {p2Choice.type}{p2Special} [{p2Sum}]";
        player2DuelText.gameObject.SetActive(true);
    }

    // This is for general messages like "Player 1 Wins!" or "Tie! Rerolling..."
    public void LogDuelMessage(string message)
    {
        Debug.Log(message); // Keep logging to the console for our own debugging.
        duelLogText.text = message;
    }

    // Controls whether the main "Roll" button can be clicked.
    public void SetRollButtonInteractable(bool isInteractable)
    {
        if (rollButton != null)
        {
            rollButton.interactable = isInteractable;
        }
    }

    // --- Build Panel Methods ---

    public void ShowBuildPanel()
    {
        buildPanel.SetActive(true);
        // We've disabled the Roll button, so the player MUST make a choice.
    }

    public void HideBuildPanel()
    {
        buildPanel.SetActive(false);
    }

    // These methods are called directly BY THE BUTTONS' OnClick() events in the Unity Editor.
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