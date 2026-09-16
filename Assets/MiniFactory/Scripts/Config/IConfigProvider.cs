namespace MiniFactory.Config
{
    // Abstraction so gameplay code never touches the config source directly.
    // A future RemoteConfigProvider (Firebase Remote Config, etc.) can implement
    // this same interface, with LocalConfigProvider values used as fallback,
    // without any change to Factory/GameBootstrap.
    public interface IConfigProvider
    {
        MachineDefinition[] GetMachineDefinitions();
        bool IsBoostEnabled();
        float GetBoostDurationSeconds();
        float GetBoostMultiplier();
        float GetMaxOfflineSeconds();
    }
}
