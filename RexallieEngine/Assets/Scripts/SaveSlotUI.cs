using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        if (metadata != null)
        {
            // A save file exists, so display its data.
            slotNameText.text = metadata.saveName;
            timestampText.text = metadata.timestamp;
            playtimeText.text = FormatPlaytime(metadata.totalPlaytime);

            // Load the screenshot from the file path
            Texture2D screenshot = LoadScreenshot(metadata.screenshotPath);
            if (screenshot != null)
            {
                screenshotImage.sprite = Sprite.Create(screenshot, new Rect(0, 0, screenshot.width, screenshot.height), new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            // No save file, display as an empty slot.
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
            // In save mode, we can prompt for a name or just save directly.
            // For now, we'll use a default name.
            string saveName = $"Save {slotNumber + 1}";
            SaveManager.Instance.SaveGame(slotNumber, saveName);
            parentPanel.Hide(); // Close the panel after saving
        }
        else
        {
            // In load mode, just load the game.
            SaveManager.Instance.LoadGame(slotNumber);
            parentPanel.Hide(); // Close the panel after loading
        }
    }

    private Texture2D LoadScreenshot(string path)
    {
        if (System.IO.File.Exists(path))
        {
            byte[] fileData = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); // This will auto-resize the texture
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