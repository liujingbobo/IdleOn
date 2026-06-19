using UnityEngine;

namespace IdleOn.World
{
    // Walk-up portal. Travels to destinationMapId via the existing MapSystem if valid; otherwise
    // logs a safe warning. Does not rewrite or extend the map system.
    public class PortalInteractable : WorldInteractable
    {
        [SerializeField] private string destinationMapId;

        public override void Interact(GameObject player)
        {
            if (string.IsNullOrEmpty(destinationMapId))
            {
                Debug.LogWarning("[Portal] destinationMapId is empty — no transition.", this);
                return;
            }
            if (MapSystem.Instance == null)
            {
                Debug.LogWarning($"[Portal] MapSystem.Instance is null — would travel to '{destinationMapId}'.", this);
                return;
            }
            MapSystem.Instance.TravelTo(destinationMapId);
        }
    }
}
