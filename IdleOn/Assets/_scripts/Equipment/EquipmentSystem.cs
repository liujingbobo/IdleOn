using System.Collections.Generic;
using UnityEngine;
using IdleOn.Core;
using IdleOn.Items;
using IdleOn.Save;
using IdleOn.Inventory;
using IdleOn.Characters;

namespace IdleOn.Equipment
{
    public class EquipmentSystem : MonoBehaviour
    {
        public static EquipmentSystem Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private EquipmentData Data => SaveManager.Instance?.CurrentSave?.Equipment;

        // ── Public API ───────────────────────────────────────────────────────

        public bool Equip(string itemId)
        {
            var def = GameDatabase.Instance?.Items?.GetItem(itemId);
            if (def == null || def.ItemType != ItemType.Equipment) return false;

            var data = Data;
            if (data == null) return false;

            var slot = def.EquipmentSlot;
            var inv  = InventorySystem.Instance;
            if (inv == null) return false;

            // Remove new item first so the freed slot can receive the swapped-out item
            if (!inv.RemoveItem(itemId, 1)) return false;

            if (data.IsOccupied(slot))
            {
                string oldId = data.Get(slot);
                inv.TryAddItem(oldId, 1); // guaranteed space since we just freed one
                data.Clear(slot);
            }

            data.Set(slot, itemId);
            PlayerStats.Instance?.Recalculate();
            GameEvents.RaiseEquipmentChanged();
            GameEvents.RaiseItemEquipped(itemId);
            return true;
        }

        public bool Unequip(EquipmentSlot slot)
        {
            var data = Data;
            if (data == null) return false;

            string itemId = data.Get(slot);
            if (itemId == null) return false;

            if (InventorySystem.Instance == null) return false;
            if (!InventorySystem.Instance.TryAddItem(itemId, 1)) return false;

            data.Clear(slot);
            PlayerStats.Instance?.Recalculate();
            GameEvents.RaiseEquipmentChanged();
            return true;
        }

        public string GetEquipped(EquipmentSlot slot)    => Data?.Get(slot);
        public bool   IsSlotOccupied(EquipmentSlot slot) => Data?.IsOccupied(slot) ?? false;

        public IReadOnlyList<EquipmentSlotEntry> GetAllEquipped()
        {
            return Data?.AllEquipped ?? (IReadOnlyList<EquipmentSlotEntry>)System.Array.Empty<EquipmentSlotEntry>();
        }

        // ── Debug ────────────────────────────────────────────────────────────

        [ContextMenu("Debug: Print Equipped Items")]
        private void DebugPrintEquipped()
        {
            var data = Data;
            if (data == null) { Debug.Log("[EquipmentSystem] No save data."); return; }

            if (data.AllEquipped.Count == 0)
            {
                Debug.Log("[EquipmentSystem] Nothing equipped.");
                return;
            }

            foreach (var e in data.AllEquipped)
                Debug.Log($"[EquipmentSystem] {e.Slot} → {e.ItemId}");
        }
    }
}
