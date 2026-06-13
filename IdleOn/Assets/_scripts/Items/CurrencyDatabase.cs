using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.Items
{
    [CreateAssetMenu(fileName = "CurrencyDatabase", menuName = "IdleOn/Currency Database")]
    public class CurrencyDatabase : ScriptableObject
    {
        [SerializeField] private List<CurrencyDefinition> currencies = new List<CurrencyDefinition>();

        public IReadOnlyList<CurrencyDefinition> AllCurrencies => currencies;

        public CurrencyDefinition GetCurrency(CurrencyType type)
        {
            foreach (var c in currencies)
                if (c != null && c.CurrencyType == type) return c;
            return null;
        }
    }
}
