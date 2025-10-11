using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO; // Required for file operations

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image screenshotImage;
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI playtimeText;
    [SerializeField] private Button slotButton;

    private int slotNumber;
    private bool isSaveMode;
    private SaveLoadPanel parentPanel;

    public void Configure(int slot, bool isSaving, SaveMetadata metadata, SaveLoadPanel panel)
    {
        slotNumber = slot;
        isSaveMode = isSaving;
        parentPanel = panel;

        if (metadata != null && !string.IsNullOrEmpty(metadata.timestamp))
        {
            slotNameText.text = metadata.saveName;
            timestampText.text = metadata.timestamp;
            playtimeText.text = FormatPlaytime(metadata.totalPlaytime);

            Texture2D screenshot = LoadScreenshot(metadata.screenshotPath);
            if (screenshot != null)
            {
                screenshotImage.sprite = Sprite.Create(screenshot, new Rect(0, 0, screenshot.width, screenshot.height), new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            slotNameText.text = $"Empty Slot {slotNumber + 1}";
            timestampText.text = "--:--:--";
            playtimeText.text = "00:00:00";
        }

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        if (isSaveMode)
        {
            string saveName = $"Save {slotNumber + 1}";
            // Pass the parent panel's Refresh method as the action to perform on completion.
            SaveManager.Instance.SaveGame(slotNumber, saveName, parentPanel.Refresh);
        }
        else
        {
            SaveManager.Instance.LoadGame(slotNumber);
            parentPanel.Hide();
        }
    }

    private Texture2D LoadScreenshot(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            return tex;
        }
        return null;
    }

    private string FormatPlaytime(float seconds)
    {
        System.TimeSpan time = System.TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}", time.Hours, time.Minutes, time.Seconds);
    }
}