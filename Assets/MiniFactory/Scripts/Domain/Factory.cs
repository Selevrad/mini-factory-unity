using System;
using System.Collections.Generic;
using System.Linq;
using MiniFactory.Config;

namespace MiniFactory.Domain
{
    // Pure C# economy model - no MonoBehaviour, no Unity API dependency beyond
    // what IConfigProvider exposes, so it is directly unit-testable.
    public class Factory
    {
        public double Balance { get; private set; }
        public IReadOnlyList<Machine> Machines => _machines;
        public double BoostEndUnixTime { get; private set; }

        private readonly List<Machine> _machines = new List<Machine>();
        private readonly IConfigProvider _config;

        public Factory(IConfigProvider config, double startingBalance, IReadOnlyList<MachineSaveState> savedMachines, double boostEndUnixTime)
        {
            _config = config;
            Balance = startingBalance;
            BoostEndUnixTime = boostEndUnixTime;

            var defs = config.GetMachineDefinitions();
            for (int i = 0; i < defs.Length; i++)
            {
                bool unlocked = i == 0;
                int level = unlocked ? 1 : 0;
                if (savedMachines != null && i < savedMachines.Count)
                {
                    if (savedMachines[i].unlocked) unlocked = true;
                    if (savedMachines[i].level > 0) level = savedMachines[i].level;
                }
                _machines.Add(new Machine(defs[i], unlocked, level));
            }
        }

        public bool IsBoostActive(double nowUnixTime) => nowUnixTime < BoostEndUnixTime;

        public double TotalProductionPerSecond(double nowUnixTime)
        {
            double sum = _machines.Sum(m => m.CurrentProduction);
            if (IsBoostActive(nowUnixTime)) sum *= _config.GetBoostMultiplier();
            return sum;
        }

        public void Tick(double deltaSeconds, double nowUnixTime)
        {
            if (deltaSeconds <= 0) return;
            Balance += TotalProductionPerSecond(nowUnixTime) * deltaSeconds;
        }

        public bool TryUnlockMachine(int index)
        {
            var machine = _machines[index];
            if (machine.Unlocked) return false;
            if (Balance < machine.Definition.unlockCost) return false;
            Balance -= machine.Definition.unlockCost;
            machine.Unlocked = true;
            machine.Level = 1;
            return true;
        }

        public bool TryUpgradeMachine(int index)
        {
            var machine = _machines[index];
            if (!machine.Unlocked) return false;
            double cost = machine.NextUpgradeCost;
            if (Balance < cost) return false;
            Balance -= cost;
            machine.Level += 1;
            return true;
        }

        public bool TryStartBoost(double nowUnixTime)
        {
            if (!_config.IsBoostEnabled()) return false;
            BoostEndUnixTime = nowUnixTime + _config.GetBoostDurationSeconds();
            return true;
        }

        public void AddCurrency(double amount)
        {
            if (amount > 0) Balance += amount;
        }

        // elapsedSeconds: real time since the last save. Clamped to the configured
        // max offline duration, and only the portion of that window still covered
        // by an active boost is multiplied - matches "boost учитывает реально
        // прошедшее время" from the spec.
        public double ApplyOfflineProgress(double elapsedSeconds, double nowUnixTime)
        {
            double clamped = Math.Min(elapsedSeconds, _config.GetMaxOfflineSeconds());
            if (clamped <= 0) return 0;

            double windowStart = nowUnixTime - clamped;
            double boostActiveSeconds = Math.Clamp(BoostEndUnixTime - windowStart, 0, clamped);
            double normalSeconds = clamped - boostActiveSeconds;

            double baseSum = _machines.Sum(m => m.CurrentProduction);
            double income = baseSum * normalSeconds + baseSum * boostActiveSeconds * _config.GetBoostMultiplier();
            Balance += income;
            return income;
        }
    }
}
