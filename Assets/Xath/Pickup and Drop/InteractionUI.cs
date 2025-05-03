using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [Header("Pickup UI References")]
    public GameObject pickupPromptPanel;
    public TextMeshProUGUI pickupPromptText;

    [Header("Drop UI References")]
    public GameObject dropPromptPanel;
    public TextMeshProUGUI dropPromptText;

    [Header("Animation Settings")]
    public float fadeSpeed = 5f;

    private CanvasGroup pickupCanvasGroup;
    private CanvasGroup dropCanvasGroup;

    private bool shouldShowPickup = false;
    private bool shouldShowDrop = false;

    private void Awake()
    {
        // Set up the pickup prompt panel
        pickupCanvasGroup = pickupPromptPanel.GetComponent<CanvasGroup>();
        if (pickupCanvasGroup == null)
        {
            pickupCanvasGroup = pickupPromptPanel.AddComponent<CanvasGroup>();
        }

        // Set up the drop prompt panel
        dropCanvasGroup = dropPromptPanel.GetComponent<CanvasGroup>();
        if (dropCanvasGroup == null)
        {
            dropCanvasGroup = dropPromptPanel.AddComponent<CanvasGroup>();
        }

        // Make sure both are hidden at start
        pickupCanvasGroup.alpha = 0;
        dropCanvasGroup.alpha = 0;
        pickupPromptPanel.SetActive(false);
        dropPromptPanel.SetActive(false);
    }

    private void Update()
    {
        // Smooth fade for pickup prompt
        UpdatePromptVisibility(pickupPromptPanel, pickupCanvasGroup, shouldShowPickup);

        // Smooth fade for drop prompt
        UpdatePromptVisibility(dropPromptPanel, dropCanvasGroup, shouldShowDrop);
    }

    private void UpdatePromptVisibility(GameObject panel, CanvasGroup canvasGroup, bool shouldShow)
    {
        if (shouldShow && canvasGroup.alpha < 1)
        {
            panel.SetActive(true);
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
        }
        else if (!shouldShow && canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            if (canvasGroup.alpha <= 0)
            {
                panel.SetActive(false);
            }
        }
    }

    public void ShowPickupPrompt(bool show, string promptText = "")
    {
        shouldShowPickup = show;
        if (show && !string.IsNullOrEmpty(promptText))
        {
            pickupPromptText.text = promptText;
        }
    }

    public void ShowDropPrompt(bool show, string promptText = "")
    {
        shouldShowDrop = show;
        if (show && !string.IsNullOrEmpty(promptText))
        {
            dropPromptText.text = promptText;
        }
    }

    // Hide all prompts
    public void HideAllPrompts()
    {
        shouldShowPickup = false;
        shouldShowDrop = false;
    }
}