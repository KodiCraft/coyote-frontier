using System.Linq;
using Content.Shared._NF.Shipyard.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;

namespace Content.Shared._Coyote.VesselConsent;

public abstract class SharedVesselConsentSystem : EntitySystem
{
    protected bool PlayerOwnsCurrentVessel(EntityUid? player)
    {
        if (!TryComp(player, out TransformComponent? trans))
            return false;
        if (!TryComp(player, out InventoryComponent? inv))
            return false;

        return inv.Slots.Zip(inv.Containers)
            .Where(tuple =>
            {
                if (tuple.First.Name != "id")
                    return false;

                if (tuple.Second.ContainedEntity is not {} ent)
                    return false;
                if (!TryComp(ent, out ItemSlotsComponent? itemSlots))
                    return false;
                var id = itemSlots.Slots.GetValueOrDefault("PDA-id")?.ContainerSlot?.ContainedEntity;
                if (!TryComp(id, out ShuttleDeedComponent? deed))
                    return false;

                return deed.ShuttleUid == trans.GridUid && deed.ShuttleUid != null;

            })
            .Any();
    }
}
