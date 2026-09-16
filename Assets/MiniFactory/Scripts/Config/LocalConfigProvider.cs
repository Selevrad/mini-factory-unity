namespace MiniFactory.Config
{
    public class LocalConfigProvider : IConfigProvider
    {
        private readonly EconomyConfig _config;

        public LocalConfigProvider(EconomyConfig config)
        {
            _config = config;
        }

        public MachineDefinition[] GetMachineDefinitions() => _config.machines;
        public bool IsBoostEnabled() => _config.boostEnabled;
        public float GetBoostDurationSeconds() => _config.boostDurationSeconds;
        public float GetBoostMultiplier() => _config.boostMultiplier;
        public float GetMaxOfflineSeconds() => _config.maxOfflineSeconds;
    }
}
