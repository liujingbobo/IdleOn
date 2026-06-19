using UnityEngine;

namespace IdleOn.World
{
    // Phase 2 same-lane interactable. Player walks to InteractionX on the current lane, then
    // Interact() fires once it is within InteractionRange. No multi-lane / pathfinding.
    public interface IWorldInteractable
    {
        float InteractionX     { get; }
        float InteractionRange { get; }
        bool  CanInteract(GameObject player);
        void  Interact(GameObject player);
    }
}
