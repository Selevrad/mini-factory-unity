using System;
using System.Collections.Generic;
using UnityEngine;
using MiniFactory.Config;
using MiniFactory.Domain;
using MiniFactory.Services.Save;
using MiniFactory.Services.Analytics;
using MiniFactory.Services.IAP;

namespace MiniFactory.Runtime
{
    // Composition root: wires config/save/analytics/IAP into a Factory and
    // exposes the small surface the UI needs. Also owns mobile lifecycle
    // (pause/quit) persistence and the per-frame production tick.
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private string coinsPackSmallProductId = "coins_pack_small";
        [SerializeField] private double coinsPackSmallAmount = 100;
        [SerializeField] private float autosaveIntervalSeconds = 30f;

        public Factory Factory { get; private set; }
        public AnalyticsService Analytics { get; private set; }
        public IPurchasingService Purchasing { get; private set; }

        public event Action StateChanged;

        private ISaveService _saveService;
        private bool _wasBoostActive;
        private float _autosaveTimer;

        private static double NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private void Awake()
        {
            // Without this, losing OS/Editor focus pauses the whole player loop
            // (Update, coroutines, deferred Destroy) - harmless for an idle game
            // whose offline progress is already handled via OnApplicationPause,
            // and it keeps Editor testing responsive while another window has focus.
            Application.runInBackground = true;

            _saveService = new JsonFileSaveService();

            Analytics = new AnalyticsService();
            Analytics.RegisterProvider(new ConsoleAnalyticsProvider());

            IConfigProvider config = new LocalConfigProvider(economyConfig);

            var saved = _saveService.Load();
            double startingBalance = saved?.balance ?? 0;
            double boostEnd = saved?.boostEndUnixTime ?? 0;

            Factory = new Factory(config, startingBalance, saved?.machines, boostEnd);
            _wasBoostActive = Factory.IsBoostActive(NowUnix);

            Purchasing = new UnityPurchasingService(new[] { coinsPackSmallProductId });
            Purchasing.PurchaseSucceeded += OnPurchaseSucceeded;
            Purchasing.PurchaseFailed += OnPurchaseFailed;
            Purchasing.InitializeFailed += OnInitializeFailed;
            Purchasing.Initialize();

            Analytics.LogEvent("game_started");

            if (saved != null)
            {
                double elapsed = NowUnix - saved.lastSaveUnixTime;
                if (elapsed > 0)
                {
                    double income = Factory.ApplyOfflineProgress(elapsed, NowUnix);
                    if (income > 0)
                    {
                        Analytics.LogEvent("offline_income_applied", new Dictionary<string, object>
                        {
                            { "amount", income },
                            { "elapsed_seconds", elapsed }
                        });
                    }
                }
            }

            StateChanged?.Invoke();
        }

        private void Update()
        {
            if (Factory == null) return;

            double now = NowUnix;
            Factory.Tick(Time.unscaledDeltaTime, now);

            bool boostActiveNow = Factory.IsBoostActive(now);
            if (_wasBoostActive && !boostActiveNow)
                Analytics.LogEvent("boost_finished");
            _wasBoostActive = boostActiveNow;

            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer >= autosaveIntervalSeconds)
            {
                _autosaveTimer = 0f;
                SaveNow();
            }

            StateChanged?.Invoke();
        }

        public void UnlockMachine(int index)
        {
            if (!Factory.TryUnlockMachine(index)) return;
            Analytics.LogEvent("machine_unlocked", new Dictionary<string, object>
            {
                { "machine_index", index },
                { "machine_id", Factory.Machines[index].Definition.id }
            });
            SaveNow();
            StateChanged?.Invoke();
        }

        public void UpgradeMachine(int index)
        {
            if (!Factory.TryUpgradeMachine(index)) return;
            Analytics.LogEvent("machine_upgraded", new Dictionary<string, object>
            {
                { "machine_index", index },
                { "machine_id", Factory.Machines[index].Definition.id },
                { "new_level", Factory.Machines[index].Level }
            });
            SaveNow();
            StateChanged?.Invoke();
        }

        public void StartBoost()
        {
            if (!Factory.TryStartBoost(NowUnix)) return;
            _wasBoostActive = true;
            Analytics.LogEvent("boost_started");
            SaveNow();
            StateChanged?.Invoke();
        }

        public void BuyCoinsPackSmall()
        {
            Purchasing.BuyProduct(coinsPackSmallProductId);
        }

        private void OnPurchaseSucceeded(string productId)
        {
            if (productId == coinsPackSmallProductId)
                Factory.AddCurrency(coinsPackSmallAmount);

            Analytics.LogEvent("purchase_succeeded", new Dictionary<string, object> { { "product_id", productId } });
            SaveNow();
            StateChanged?.Invoke();
        }

        private void OnPurchaseFailed(string productId, PurchaseFailureKind reason)
        {
            Analytics.LogEvent("purchase_failed", new Dictionary<string, object>
            {
                { "product_id", productId },
                { "reason", reason.ToString() }
            });
        }

        private void OnInitializeFailed(string message)
        {
            Debug.LogWarning($"[MiniFactory] IAP init failed: {message}");
        }

        private void SaveNow()
        {
            // Guards OnApplicationQuit/Pause firing before Awake has finished
            // (e.g. quitting mid-initialization) so it can't crash on shutdown.
            if (Factory == null) return;

            var machines = new MachineSaveState[Factory.Machines.Count];
            for (int i = 0; i < machines.Length; i++)
            {
                machines[i] = new MachineSaveState
                {
                    unlocked = Factory.Machines[i].Unlocked,
                    level = Factory.Machines[i].Level
                };
            }

            _saveService.Save(new FactorySaveData
            {
                balance = Factory.Balance,
                boostEndUnixTime = Factory.BoostEndUnixTime,
                lastSaveUnixTime = NowUnix,
                machines = machines
            });
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveNow();
        }

        private void OnApplicationQuit()
        {
            SaveNow();
        }
    }
}
