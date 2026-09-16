using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MiniFactory.Domain;

namespace MiniFactory.Runtime
{
    public class MachineRowView : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text productionText;
        [SerializeField] private Text costText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonLabel;
        [SerializeField] private Image levelBadge;

        private static readonly Color LockedColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        private static readonly Color UnlockedColor = Color.white;
        private static readonly Color UnlockActionColor = new Color(0.25f, 0.78f, 0.49f, 1f);
        private static readonly Color UpgradeActionColor = new Color(0.30f, 0.56f, 0.95f, 1f);
        private static readonly Color UnaffordableButtonColor = new Color(0.32f, 0.33f, 0.38f, 1f);
        private static readonly Color LevelBadgeLowColor = new Color(0.3f, 0.55f, 1f, 1f);
        private static readonly Color LevelBadgeHighColor = new Color(1f, 0.75f, 0.1f, 1f);
        private const float PulseDuration = 0.25f;
        private const float PulsePeakScale = 1.12f;
        private const int LevelBadgeColorCap = 10;

        private int _index;
        private bool _unlocked;
        private int _lastSeenLevel = -1;
        private Image _actionButtonImage;
        private RectTransform _rectTransform;
        private Coroutine _pulseCoroutine;
        private Action<int> _onUnlock;
        private Action<int> _onUpgrade;

        public void Init(int index, Action<int> onUnlock, Action<int> onUpgrade)
        {
            _index = index;
            _onUnlock = onUnlock;
            _onUpgrade = onUpgrade;
            _actionButtonImage = actionButton.GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
            actionButton.onClick.AddListener(OnActionClicked);
        }

        public void Refresh(Machine machine, double balance)
        {
            _unlocked = machine.Unlocked;
            nameText.text = machine.Definition.displayName;

            double actionCost;
            int currentLevel = _unlocked ? machine.Level : 0;
            if (!_unlocked)
            {
                stateText.text = "Заблокировано";
                productionText.text = "-";
                actionCost = machine.Definition.unlockCost;
                costText.text = $"Открыть: {actionCost:F0}";
                actionButtonLabel.text = "Открыть";
            }
            else
            {
                stateText.text = $"Открыто (Ур. {machine.Level})";
                productionText.text = $"{machine.CurrentProduction:F1}/с";
                actionCost = machine.NextUpgradeCost;
                costText.text = $"Улучшить: {actionCost:F0}";
                actionButtonLabel.text = "Улучшить";
            }

            Color rowColor = _unlocked ? UnlockedColor : LockedColor;
            nameText.color = rowColor;
            stateText.color = rowColor;
            productionText.color = rowColor;
            costText.color = rowColor;

            bool canAfford = balance >= actionCost;
            actionButton.interactable = canAfford;
            if (_actionButtonImage != null)
            {
                Color actionColor = _unlocked ? UpgradeActionColor : UnlockActionColor;
                _actionButtonImage.color = canAfford ? actionColor : UnaffordableButtonColor;
            }

            if (levelBadge != null)
            {
                levelBadge.gameObject.SetActive(_unlocked);
                if (_unlocked)
                {
                    float t = Mathf.Clamp01((currentLevel - 1) / (float)LevelBadgeColorCap);
                    levelBadge.color = Color.Lerp(LevelBadgeLowColor, LevelBadgeHighColor, t);
                }
            }

            // _lastSeenLevel starts at -1 so the very first Refresh (initial load / unlock)
            // never triggers a pulse - only a genuine level increase does.
            if (_lastSeenLevel >= 0 && currentLevel > _lastSeenLevel)
                PlayUpgradePulse();
            _lastSeenLevel = currentLevel;
        }

        private void PlayUpgradePulse()
        {
            if (_rectTransform == null) return;
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            float t = 0f;
            while (t < PulseDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < 0.5f
                    ? Mathf.Lerp(1f, PulsePeakScale, p * 2f)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - 0.5f) * 2f);
                _rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            _rectTransform.localScale = Vector3.one;
            _pulseCoroutine = null;
        }

        private void OnActionClicked()
        {
            if (_unlocked) _onUpgrade?.Invoke(_index);
            else _onUnlock?.Invoke(_index);
        }
    }
}
