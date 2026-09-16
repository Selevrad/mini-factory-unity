using System;

namespace MiniFactory.Config
{
    [Serializable]
    public class MachineDefinition
    {
        public string id = "machine";
        public string displayName = "Machine";
        public double unlockCost = 50;
        public double baseProduction = 1;
        public double productionGrowthPerLevel = 1.15;
        public double baseUpgradeCost = 25;
        public double upgradeCostGrowthPerLevel = 1.2;
    }
}
