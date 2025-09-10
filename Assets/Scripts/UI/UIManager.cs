using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    // --- Singleton Setup ---
    public static UIManager Instance { get; private set; }

    // --- References ---
    [Header("Player HUD")]
    [SerializeField] private TextMeshProUGUI player1MoneyText;
    [SerializeField] private TextMeshProUGUI player2MoneyText;

    [Header("Duel Display")]
    [SerializeField] private TextMeshProUGUI player1DuelText;
    [SerializeField] private TextMeshProUGUI player2DuelText;
    [SerializeField] private TextMeshProUGUI duelLogText;

    [Header("Choice Panels")]
    [SerializeField] private GameObject rpsChoicePanel;
    [SerializeField] private GameObject buildPanel;
    [SerializeField] private GameObject chanceCardPanel;

    [Header("Build Panel Buttons")]
    [SerializeField] private Button buildHouseButton;
    [SerializeField] private Button buildShopButton;
    [SerializeField] private Button passTurnButton;

    [Header("Chance Card Panel")]
    [SerializeField] private TextMeshProUGUI chanceCardTitleText;
    [SerializeField] private TextMeshProUGUI chanceCardDescriptionText;
    [SerializeField] private GameObject continueTextObject;

    [Header("Athlete Panel")]
    [SerializeField] private GameObject athleteChoicePanel;

    // --- Unity Methods ---
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

    // --- Public Methods Called by GameManager ---

    public void UpdatePlayerMoney(PlayerController player)
    {
        string playerName = (player.playerId == 0) ? "Player 1" : "Bot";
        TextMeshProUGUI targetText = (player.playerId == 0) ? player1MoneyText : player2MoneyText;

        if (targetText != null)
        {
            targetText.text = $"{playerName}: ${player.money}";
        }
    }

    public void ShowDuelResults(DuelChoice p1Choice, DuelChoice p2Choice, int p1Sum, int p2Sum)
    {
        string p1Special = p1Choice.isSpecial ? " (S!)" : "";
        player1DuelText.text = $"Player 1: {p1Choice.type}{p1Special} [{p1Sum}]";

        string p2Special = p2Choice.isSpecial ? " (S!)" : "";
        player2DuelText.text = $"Bot: {p2Choice.type}{p2Special} [{p2Sum}]";
    }

    public void LogDuelMessage(string message)
    {
        if (duelLogText != null)
        {
            duelLogText.text = message;
        }
    }

    // --- Panel Control ---

    public void ShowRpsChoicePanel()
    {
        if (rpsChoicePanel != null) rpsChoicePanel.SetActive(true);
    }

    public void HideRpsChoicePanel()
    {
        if (rpsChoicePanel != null) rpsChoicePanel.SetActive(false);
    }

    public void ShowBuildPanel(PlayerController player, BuildingData houseData, BuildingData shopData)
    {
        if (buildPanel == null) return;

        if (buildHouseButton != null)
            buildHouseButton.interactable = (player.money >= houseData.buildingCost);

        if (buildShopButton != null)
            buildShopButton.interactable = (player.money >= shopData.buildingCost);

        if (passTurnButton != null)
            passTurnButton.interactable = true;

        buildPanel.SetActive(true);
    }

    public void HideBuildPanel()
    {
        if (buildPanel != null) buildPanel.SetActive(false);
    }

    // This is the coroutine for the chance card, as we designed.
    public IEnumerator ShowChanceCardCoroutine(ChanceCardData card)
    {
        if (chanceCardPanel == null) yield break; // Safety check

        chanceCardTitleText.text = card.cardTitle;
        chanceCardDescriptionText.text = card.cardDescription;
        if (continueTextObject != null) continueTextObject.SetActive(false);

        chanceCardPanel.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        if (continueTextObject != null) continueTextObject.SetActive(true);

        // This requires the Input System Package
        yield return new WaitUntil(() => UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame);

        chanceCardPanel.SetActive(false);

        GameManager.Instance.ResumeAfterChanceCard();
    }


    // Methods called by UI buttons 

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

    public void OnPassTurnClicked()
    {
        // We need to add this method to GameManager.cs
        GameManager.Instance.PlayerChoseToPass();
        HideBuildPanel();
    }

    public void ShowAthleteChoicePanel()
    {
        if (athleteChoicePanel != null) athleteChoicePanel.SetActive(true);
    }

    public void HideAthleteChoicePanel()
    {
        if (athleteChoicePanel != null) athleteChoicePanel.SetActive(false);
    }

    // These will be called BY the buttons in the Inspector
    public void OnAthleteChooseYes()
    {
        GameManager.Instance.PlayerChoseAthleteBonus(true);
        HideAthleteChoicePanel();
    }

    public void OnAthleteChooseNo()
    {
        GameManager.Instance.PlayerChoseAthleteBonus(false);
        HideAthleteChoicePanel();
    }
}