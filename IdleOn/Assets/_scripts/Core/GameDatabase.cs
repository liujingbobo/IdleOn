using UnityEngine;
using IdleOn.Items;
using IdleOn.Crafting;
using IdleOn.Vault;

namespace IdleOn.Core
{
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "IdleOn/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        public static GameDatabase Instance { get; private set; }

        [SerializeField] private ItemDatabase     itemDatabase;
        [SerializeField] private CurrencyDatabase currencyDatabase;
        [SerializeField] private CraftingDatabase craftingDatabase;
        [SerializeField] private VaultDatabase    vaultDatabase;

        public ItemDatabase     Items    => itemDatabase;
        public CurrencyDatabase Currency => currencyDatabase;
        public CraftingDatabase Crafting => craftingDatabase;
        public VaultDatabase    Vault    => vaultDatabase;

        public static void Register(GameDatabase db)
        {
            if (db == null)
            {
                Debug.LogError("[GameDatabase] Register called with null — assign GameDatabase asset to GameBootstrap.");
                return;
            }
            Instance = db;
        }
    }
}
