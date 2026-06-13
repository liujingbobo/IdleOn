using UnityEngine;

namespace IdleOn.Items
{
    [CreateAssetMenu(fileName = "NewCurrency", menuName = "IdleOn/Currency Definition")]
    public class CurrencyDefinition : ScriptableObject
    {
        public CurrencyType CurrencyType;
        public string       DisplayName;
        public Sprite       Icon;
    }
}
