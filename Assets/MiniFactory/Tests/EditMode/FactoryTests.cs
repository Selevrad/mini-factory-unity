using NUnit.Framework;
using MiniFactory.Config;
using MiniFactory.Domain;

namespace MiniFactory.Tests.EditMode
{
    public class FakeConfigProvider : IConfigProvider
    {
        public MachineDefinition[] Machines = {
            new MachineDefinition { id = "m0", displayName = "M0", unlockCost = 0, baseProduction = 1, productionGrowthPerLevel = 1.1, baseUpgradeCost = 10, upgradeCostGrowthPerLevel = 1.2 },
            new MachineDefinition { id = "m1", displayName = "M1", unlockCost = 50, baseProduction = 3, productionGrowthPerLevel = 1.1, baseUpgradeCost = 20, upgradeCostGrowthPerLevel = 1.2 },
        };

        public bool BoostEnabled = true;
        public float BoostDuration = 30f;
        public float BoostMultiplier = 2f;
        public float MaxOfflineSeconds = 3600f;

        public MachineDefinition[] GetMachineDefinitions() => Machines;
        public bool IsBoostEnabled() => BoostEnabled;
        public float GetBoostDurationSeconds() => BoostDuration;
        public float GetBoostMultiplier() => BoostMultiplier;
        public float GetMaxOfflineSeconds() => MaxOfflineSeconds;
    }

    public class FactoryTests
    {
        private static Factory CreateFactory(FakeConfigProvider config, double startingBalance = 100, double boostEndUnixTime = 0)
        {
            return new Factory(config, startingBalance, null, boostEndUnixTime);
        }

        [Test]
        public void UnlockMachine_DeductsCostAndUnlocks()
        {
            var config = new FakeConfigProvider();
            var factory = CreateFactory(config, startingBalance: 100);

            bool result = factory.TryUnlockMachine(1);

            Assert.IsTrue(result);
            Assert.IsTrue(factory.Machines[1].Unlocked);
            Assert.AreEqual(1, factory.Machines[1].Level);
            Assert.AreEqual(50, factory.Balance);
        }

        [Test]
        public void UnlockMachine_FailsWhenBalanceInsufficient()
        {
            var config = new FakeConfigProvider();
            var factory = CreateFactory(config, startingBalance: 10);

            bool result = factory.TryUnlockMachine(1);

            Assert.IsFalse(result);
            Assert.IsFalse(factory.Machines[1].Unlocked);
            Assert.AreEqual(10, factory.Balance);
        }

        [Test]
        public void UpgradeMachine_IncreasesLevelAndProduction()
        {
            var config = new FakeConfigProvider();
            var factory = CreateFactory(config, startingBalance: 100);
            double productionBefore = factory.Machines[0].CurrentProduction;

            bool result = factory.TryUpgradeMachine(0);

            Assert.IsTrue(result);
            Assert.AreEqual(2, factory.Machines[0].Level);
            Assert.Greater(factory.Machines[0].CurrentProduction, productionBefore);
            Assert.AreEqual(90, factory.Balance); // 100 - baseUpgradeCost(10)
        }

        [Test]
        public void TotalProductionPerSecond_AppliesBoostMultiplier()
        {
            var config = new FakeConfigProvider();
            double now = 1000;
            var factory = CreateFactory(config, startingBalance: 0, boostEndUnixTime: now + 10);

            double boosted = factory.TotalProductionPerSecond(now);
            double unboosted = factory.TotalProductionPerSecond(now + 20);

            Assert.AreEqual(1.0 * config.BoostMultiplier, boosted, 0.0001);
            Assert.AreEqual(1.0, unboosted, 0.0001);
        }

        [Test]
        public void ApplyOfflineProgress_ClampsToMaxOfflineDuration()
        {
            var config = new FakeConfigProvider { MaxOfflineSeconds = 100 };
            var factory = CreateFactory(config, startingBalance: 0);

            double income = factory.ApplyOfflineProgress(elapsedSeconds: 10000, nowUnixTime: 20000);

            // machine0 produces 1/s, clamped to 100s of offline time, no boost active.
            Assert.AreEqual(100, income, 0.0001);
            Assert.AreEqual(100, factory.Balance, 0.0001);
        }

        [Test]
        public void ApplyOfflineProgress_AppliesBoostOnlyForTimeBoostWasActive()
        {
            var config = new FakeConfigProvider { MaxOfflineSeconds = 1000 };
            double now = 20000;
            // Boost was set to end 50s into a 200s offline window.
            var factory = CreateFactory(config, startingBalance: 0, boostEndUnixTime: now - 150);

            double income = factory.ApplyOfflineProgress(elapsedSeconds: 200, nowUnixTime: now);

            // 50s boosted at 2x + 150s normal, machine0 base production = 1/s.
            double expected = 50 * config.BoostMultiplier + 150;
            Assert.AreEqual(expected, income, 0.0001);
        }
    }
}
