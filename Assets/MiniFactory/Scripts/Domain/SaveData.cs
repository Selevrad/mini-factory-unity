using System;

namespace MiniFactory.Domain
{
    [Serializable]
    public struct MachineSaveState
    {
        public bool unlocked;
        public int level;
    }

    [Serializable]
    public class FactorySaveData
    {
        public double balance;
        public double boostEndUnixTime;
        public double lastSaveUnixTime;
        public MachineSaveState[] machines;
    }
}
