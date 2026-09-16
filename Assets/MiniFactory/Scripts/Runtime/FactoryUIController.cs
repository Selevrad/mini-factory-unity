using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniFactory.Runtime
{
    public class FactoryUIController : MonoBehaviour
    {
        [SerializeField] private GameBootstrap game;
        [SerializeField] private Text balanceText;
        [SerializeField] private Text totalProductionText;
        [SerializeField] private Button boostButton;
        [SerializeField] private Text boostButtonLabel;
        [SerializeField] private Text boostStatusText;
        [SerializeField] private Button buyCoinsButton;
        [SerializeField] private MachineRowView[] machineRows;

        private static readonly Color BoostReadyColor = new Color(0.62f, 0.40f, 0.95f, 1f);
        private static readonly Color BoostActiveColor = new Color(0.32f, 0.33f, 0.38f, 1f);

        private Image _boostButtonImage;

        private void Start()
        {
            _boostButtonImage = boostButton.GetComponent<Image>();
            for (int i = 0; i < machineRows.Length; i++)
                machineRows[i].Init(i, game.UnlockMachine, game.UpgradeMachine);

            boostButton.onClick.AddListener(game.StartBoost);
            buyCoinsButton.onClick.AddListener(game.BuyCoinsPackSmall);

            game.StateChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (game != null) game.StateChanged -= Refresh;
        }

        private void Refresh()
        {
            var factory = game.Factory;
            balanceText.text = $"Balance: {factory.Balance:F0}";

            double now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            totalProductionText.text = $"Production: {factory.TotalProductionPerSecond(now):F1}/s";

            bool boostActive = factory.IsBoostActive(now);
            if (boostActive)
            {
                double remaining = factory.BoostEndUnixTime - now;
                boostStatusText.text = $"Boost active: {remaining:F0}s left";
                boostButtonLabel.text = "Boost Active";
                boostButton.interactable = false;
                if (_boostButtonImage != null) _boostButtonImage.color = BoostActiveColor;
            }
            else
            {
                boostStatusText.text = "Boost inactive";
                boostButtonLabel.text = "Start Boost";
                boostButton.interactable = true;
                if (_boostButtonImage != null) _boostButtonImage.color = BoostReadyColor;
            }

            for (int i = 0; i < machineRows.Length && i < factory.Machines.Count; i++)
                machineRows[i].Refresh(factory.Machines[i], factory.Balance);
        }
    }
}
