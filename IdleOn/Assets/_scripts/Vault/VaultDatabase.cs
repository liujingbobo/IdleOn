using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Vault
{
    [CreateAssetMenu(fileName = "VaultDatabase", menuName = "IdleOn/Vault Database")]
    public class VaultDatabase : ScriptableObject
    {
        [SerializeField] private List<VaultUpgradeDefinition> upgrades = new List<VaultUpgradeDefinition>();

        public IReadOnlyList<VaultUpgradeDefinition> Upgrades => upgrades;
    }
}
