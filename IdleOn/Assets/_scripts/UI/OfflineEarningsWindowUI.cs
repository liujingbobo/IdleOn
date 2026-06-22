using System.Collections.Generic;
using UnityEngine;

namespace IdleOn.UI
{
    public class OfflineEarningsWindowUI : MonoBehaviour
    {
        [SerializeField] private GameObject windowPanel;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private EarningSlotUI slotPrefab;
        [SerializeField] private UIWindowMotion motion;

        private readonly List<EarningSlotUI> _spawnedSlots = new List<EarningSlotUI>();

        void Awake()
        {
            if (motion != null) motion.SetClosedImmediate();
        }

        public void Show(IReadOnlyList<(string Label, long Amount)> rewards)
        {
            if (rewards == null || rewards.Count == 0) return;

            foreach (var slot in _spawnedSlots)
                if (slot != null) Destroy(slot.gameObject);
            _spawnedSlots.Clear();

            if (slotContainer != null && slotPrefab != null)
            {
                foreach (var reward in rewards)
                {
                    var slot = Instantiate(slotPrefab, slotContainer);
                    slot.SetData(reward.Label, reward.Amount);
                    _spawnedSlots.Add(slot);
                }
            }

            if (motion != null) motion.PlayOpen();
            else if (windowPanel != null) windowPanel.SetActive(true);
        }

        public void Close()
        {
            if (motion != null) motion.PlayClose();
            else if (windowPanel != null) windowPanel.SetActive(false);
        }
    }
}
