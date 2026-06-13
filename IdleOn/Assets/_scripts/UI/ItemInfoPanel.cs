using System.Text;
using UnityEngine;
using TMPro;
using IdleOn.Items;
using IdleOn.Core;

namespace IdleOn.UI
{
    public class ItemInfoPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text   nameText;
        [SerializeField] private TMP_Text   typeText;
        [SerializeField] private TMP_Text   descriptionText;
        [SerializeField] private GameObject slotRow;
        [SerializeField] private TMP_Text   slotText;
        [SerializeField] private GameObject statsRow;
        [SerializeField] private TMP_Text   statsText;
        [SerializeField] private GameObject quantityRow;
        [SerializeField] private TMP_Text   quantityText;

        private RectTransform _rect;
        private Canvas        _canvas;

        void Awake()
        {
            _rect   = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            gameObject.SetActive(false);
        }

        public void Show(ItemDefinition def, int quantity, RectTransform anchor)
        {
            if (def == null) { Hide(); return; }

            nameText.text        = def.DisplayName;
            typeText.text        = def.ItemType.ToString();
            descriptionText.text = def.Description;

            bool isEquipment = def.ItemType == ItemType.Equipment;
            slotRow.SetActive(isEquipment);
            if (isEquipment) slotText.text = def.EquipmentSlot.ToString();

            bool hasStats = isEquipment && AnyBonusNonZero(def.StatBonuses);
            statsRow.SetActive(hasStats);
            if (hasStats) statsText.text = BuildStatsText(def.StatBonuses);

            quantityRow.SetActive(quantity > 1);
            if (quantity > 1) quantityText.text = "Qty: " + quantity;

            gameObject.SetActive(true);
            Reposition(anchor);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Reposition(RectTransform anchor)
        {
            if (_canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform,
                anchor.position,
                _canvas.worldCamera,
                out Vector2 localPos);

            localPos += new Vector2(10f, -10f);

            var canvasHalf = ((RectTransform)_canvas.transform).sizeDelta * 0.5f;
            var panelHalf  = _rect.sizeDelta * 0.5f;
            localPos.x = Mathf.Clamp(localPos.x, -canvasHalf.x + panelHalf.x, canvasHalf.x - panelHalf.x);
            localPos.y = Mathf.Clamp(localPos.y, -canvasHalf.y + panelHalf.y, canvasHalf.y - panelHalf.y);

            _rect.anchoredPosition = localPos;
        }

        private static bool AnyBonusNonZero(StatSheet s)
        {
            return s.STR != 0 || s.AGI != 0 || s.WIS != 0 || s.LUK != 0
                || s.MaxHP != 0 || s.MaxMP != 0 || s.ATKMin != 0 || s.ATKMax != 0
                || s.DEF != 0 || s.ACC != 0 || s.CRITChance != 0 || s.MoveSpeed != 0;
        }

        private static string BuildStatsText(StatSheet s)
        {
            var sb = new StringBuilder();
            if (s.STR        != 0) sb.AppendLine("STR +" + s.STR);
            if (s.AGI        != 0) sb.AppendLine("AGI +" + s.AGI);
            if (s.WIS        != 0) sb.AppendLine("WIS +" + s.WIS);
            if (s.LUK        != 0) sb.AppendLine("LUK +" + s.LUK);
            if (s.MaxHP      != 0) sb.AppendLine("HP +" + s.MaxHP);
            if (s.MaxMP      != 0) sb.AppendLine("MP +" + s.MaxMP);
            if (s.ATKMin != 0 || s.ATKMax != 0)
                sb.AppendLine("ATK +" + s.ATKMin + "~" + s.ATKMax);
            if (s.DEF        != 0) sb.AppendLine("DEF +" + s.DEF);
            if (s.ACC        != 0) sb.AppendLine("ACC +" + s.ACC);
            if (s.CRITChance != 0) sb.AppendLine("CRIT +" + s.CRITChance + "%");
            if (s.MoveSpeed  != 0) sb.AppendLine("SPD +" + s.MoveSpeed);
            return sb.ToString().TrimEnd();
        }
    }
}
