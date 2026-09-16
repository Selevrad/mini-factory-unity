using UnityEngine;

namespace MiniFactory.Config
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "MiniFactory/Economy Config")]
    public class EconomyConfig : ScriptableObject
    {
        public MachineDefinition[] machines;

        [Header("Boost")]
        public bool boostEnabled = true;
        public float boostDurationSeconds = 30f;
        public float boostMultiplier = 2f;

        [Header("Offline Production")]
        public float maxOfflineSeconds = 3600f;
    }
}
