using System;
using MiniFactory.Config;

namespace MiniFactory.Domain
{
    public class Machine
    {
        public readonly MachineDefinition Definition;
        public bool Unlocked;
        public int Level;

        public Machine(MachineDefinition definition, bool unlocked, int level)
        {
            Definition = definition;
            Unlocked = unlocked;
            Level = level;
        }

        public double CurrentProduction
        {
            get
            {
                if (!Unlocked) return 0;
                return Definition.baseProduction * Math.Pow(Definition.productionGrowthPerLevel, Level - 1);
            }
        }

        public double NextUpgradeCost
        {
            get
            {
                // Level 1 -> 2 (the first upgrade after unlocking) costs exactly baseUpgradeCost;
                // each upgrade after that scales by upgradeCostGrowthPerLevel.
                int upgradesSoFar = Unlocked ? Math.Max(0, Level - 1) : 0;
                return Definition.baseUpgradeCost * Math.Pow(Definition.upgradeCostGrowthPerLevel, upgradesSoFar);
            }
        }
    }
}
