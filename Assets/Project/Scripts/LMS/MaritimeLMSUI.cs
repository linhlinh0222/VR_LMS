using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MaritimeLMS
{
    /// <summary>
    /// Manages the heads-up display (HUD) for the maritime training simulation.
    /// </summary>
    public class MaritimeLMSUI : MonoBehaviour
    {
        [Header("UI Component References")]
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI objectiveListText;
        public TextMeshProUGUI speedText;
        public RectTransform compassRose;
        public Image panelBackground;

        [Header("Simulation References")]
        public ShipController ship;
        public ShipBridgeInput bridgeInput;
        [SerializeField] private bool legacyAutoCompleteObjectives;

        private void Start()
        {
            ApplyHUDStyling();
        }

        private void ApplyHUDStyling()
        {
            RectTransform panelRect = panelBackground != null
                ? panelBackground.rectTransform
                : GetComponentInChildren<Image>()?.rectTransform;
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.up;
                panelRect.anchorMax = Vector2.up;
                panelRect.pivot = Vector2.up;
                panelRect.anchoredPosition = new Vector2(14f, -14f);
                panelRect.sizeDelta = new Vector2(270f, 150f);
            }

            if (panelBackground != null)
            {
                panelBackground.color = new Color(0.02f, 0.07f, 0.10f, 0.56f);
#if UNITY_EDITOR
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Textures/LMS_HUD_Background.png");
                if (sprite != null)
                {
                    panelBackground.sprite = sprite;
                    panelBackground.type = Image.Type.Sliced;
                }
#endif
            }

            ConfigureText(titleText, 15f, 11f, 17f, TextAlignmentOptions.Center, TextOverflowModes.Ellipsis);
            ConfigureText(objectiveListText, 12f, 8f, 13f, TextAlignmentOptions.TopLeft, TextOverflowModes.Truncate);
            ConfigureText(speedText, 15f, 10f, 16f, TextAlignmentOptions.Center, TextOverflowModes.Truncate);

            SetRect(titleText != null ? titleText.rectTransform : null, Vector2.up, Vector2.one, new Vector2(0.5f, 1f), new Vector2(-12f, -7f), new Vector2(-24f, 38f));
            SetRect(objectiveListText != null ? objectiveListText.rectTransform : null, new Vector2(0f, 0f), Vector2.one, new Vector2(0f, 1f), new Vector2(82f, -50f), new Vector2(-94f, -86f));
            SetRect(speedText != null ? speedText.rectTransform : null, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(80f, 12f), new Vector2(-94f, 32f));
            SetRect(compassRose, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(14f, 15f), new Vector2(58f, 58f));
        }

        private static void ConfigureText(
            TextMeshProUGUI text,
            float fontSize,
            float minFontSize,
            float maxFontSize,
            TextAlignmentOptions alignment,
            TextOverflowModes overflowMode)
        {
            if (text == null)
            {
                return;
            }

            text.fontSize = fontSize;
            text.enableAutoSizing = true;
            text.fontSizeMin = minFontSize;
            text.fontSizeMax = maxFontSize;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = overflowMode;
            text.alignment = alignment;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private void Update()
        {
            if (MaritimeLMSManager.Instance == null) return;

            UpdateHUDText();
            if (legacyAutoCompleteObjectives)
            {
                UpdateObjectivesLogic();
            }
            UpdateCompass();
        }

        private void UpdateHUDText()
        {
            if (titleText != null) 
                titleText.text = MaritimeLMSManager.Instance.GetLessonTitle().ToUpper();

            if (objectiveListText != null)
            {
                string objectivesStr = "";
                foreach (var obj in MaritimeLMSManager.Instance.GetObjectives())
                {
                    string status = obj.isCompleted ? "<color=#00FFD1>[x]</color> " : "<color=#AAAAAA>[ ]</color> ";
                    objectivesStr += $"{status} {obj.description}\n";
                }
                objectiveListText.text = objectivesStr;
            }

            if (ship != null && speedText != null)
            {
                speedText.text = $"<size=70%>KNOTS</size>\n<color=#00E5FF>{ship.CurrentSpeed:F1}</color>";
            }
        }

        private void UpdateCompass()
        {
            if (ship != null && compassRose != null)
            {
                // Invert rotation for compass behavior (North stays North while ship rotates)
                compassRose.localRotation = Quaternion.Euler(0, 0, ship.transform.eulerAngles.y);
            }
        }

        private void UpdateObjectivesLogic()
        {
            var manager = MaritimeLMSManager.Instance;
            if (manager == null || manager.objectives == null) return;

            // Scenario logic: checking physical state against lesson objectives
            if (manager.objectives.Count > 0 && !manager.objectives[0].isCompleted && bridgeInput?.telegraph != null)
            {
                if (bridgeInput.telegraph.NormalizedValue > 0.7f)
                    manager.CompleteObjective(0);
            }

            if (manager.objectives.Count > 1 && !manager.objectives[1].isCompleted && ship != null)
            {
                if (ship.CurrentSpeed > 5f)
                    manager.CompleteObjective(1);
            }

            if (manager.objectives.Count > 2 && !manager.objectives[2].isCompleted && bridgeInput?.helmWheel != null)
            {
                // Detect significant steering input
                if (Mathf.Abs(bridgeInput.helmWheel.NormalizedValue - 0.5f) > 0.3f)
                    manager.CompleteObjective(2);
            }
        }
    }
}
