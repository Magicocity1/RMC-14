using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Fluids;
using Content.Shared._RMC14.Line;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.OnCollide;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Weapons.Common;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Containers;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared._RMC14.Weapons.Common;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Weapons.Ranged.RifleGrenade;

public abstract class SharedRMCRifleGrenadeSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedOnCollideSystem _onCollide = default!;
    [Dependency] private readonly LineSystem _line = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _rmcFlammable = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedRMCSpraySystem _rmcSpray = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SolutionTransferSystem _solutionTransfer = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly RMCProjectileSystem _projectile = default!;

    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    public const string MagazineSlot = "gun_magazine";
    public override void Initialize()
    {

        SubscribeLocalEvent<RMCRifleGrenadeComponent, TakeAmmoEvent>(OnTakeAmmo);
        SubscribeLocalEvent<RMCRifleGrenadeComponent, AttachableAlteredEvent>(InitRG);
        SubscribeLocalEvent<RMCRifleGrenadeComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<RMCRifleGrenadeComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<RMCRifleGrenadeComponent, GetAmmoCountEvent>(GetAmmoCount);
        SubscribeLocalEvent<RMCRifleGrenadeComponent, AttemptShootEvent>(AttemptShoot);


    }
    /*
      My General Design Pattern here should be
      1. Try to shoot the rifle RifleGrenade
      2. If no grenade to shoot then shoot the gun

      Notes:
      Allow reloading the grenade
      I don't need to care about balance now
      Actual polish can wait -- this thing just needs to work for a demo
    */
    private void InitRG(Entity<RMCRifleGrenadeComponent> ent, ref AttachableAlteredEvent args)
    {
      // I'm reusing this pattern from my vesg attempt -- no clue if it's good or not -- I just know it works enough
      switch (args.Alteration)
        {
            case AttachableAlteredType.Attached:
                ent.Comp.Holder = args.Holder;
                Dirty(ent, ent.Comp);
                break;
            case AttachableAlteredType.Detached:
                ent.Comp.Holder = null;
                Dirty(ent, ent.Comp);
                break;
        }
    }


    private void OnTakeAmmo(Entity<RMCRifleGrenadeComponent> ent, ref TakeAmmoEvent args)
    { 
        if (ent.Comp.Holder == null)
          return;
        
        if (!Containers.TryGetContainer(ent.Comp.Holder.GetValueOrDefault(), MagazineSlot, out var container) ||
            container is not ContainerSlot slot)
        { 
          if(!TryComp(ent, out BallisticAmmoProviderComponent? grenade))
            return;
          Entity<BallisticAmmoProviderComponent> wrapper = (ent.Comp.Holder.GetValueOrDefault(),grenade);
          RaiseLocalEvent<BallisticAmmoProviderComponent>(wrapper);
          return;
        }
        if(!TryComp(slot.ContainedEntity.GetValueOrDefault(), out BallisticAmmoProviderComponent? mag))
            return;
          RaiseLocalEvent<BallisticAmmoProviderComponent>(ent.Comp.Holder.GetValueOrDefault(), mag);

    }

    private void OnInsertedIntoContainer(Entity<RMCRifleGrenadeComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
      if ( ! (ent.Comp.Holder != null))
        return;
      if(!TryComp(ent.Comp.Holder.GetValueOrDefault(), out ItemSlotsComponent? slots))
        return;
      
      RaiseLocalEvent<>(wrapper);
    }

    private void OnRemovedFromContainer(Entity<RMCRifleGrenadeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
      if (ent.Comp.Holder == null)
        return;
      if(!TryComp(ent.Comp.Holder, out ItemSlotsComponent? slots))
        return;
      
      RaiseLocalEvent<>(wrapper);
    }


    private void GetAmmoCount(Entity<RMCRifleGrenadeComponent> ent, ref GetAmmoCountEvent args)
    {
      if ( ! (ent.Comp.Holder != null))
            return;
        if(!TryComp(ent.Comp.Holder, out ItemSlotsComponent? slots))
            return;
        if(!TryComp(ent, out BallisticAmmoProviderComponent? grenade))
          return;
        if (!Containers.TryGetContainer(ent.Comp.Holder.GetValueOrDefault(), MagazineSlot, out var container) ||
            container is not ContainerSlot slot)
        {
            args.Count = grenade.Count;
            args.Capacity = grenade.Capacity;
            return;
        }
        if(!TryComp(slot.ContainedEntity, out BallisticAmmoProviderComponent? mag))
          return;
        args.Count = mag.Count + grenade.Count;
        args.Capacity = mag.Capacity + grenade.Count; //grenade capacity might allow a normal bullet to get loaded
        return;
        }

    private void AttemptShoot(Entity<RMCRifleGrenadeComponent> ent, ref AttemptShootEvent args)
    {
      if ( ! (ent.Comp.Holder != null))
            return;
        if(!TryComp(ent.Comp.Holder, out GunComponent? hold))
            return;
        if(!TryComp(ent, out GunComponent? grenade))
          return;
        if(!TryComp(ent.Comp.Holder, out ItemSlotsComponent? slots))
            return;
        if(!TryComp(ent, out BallisticAmmoProviderComponent? grenadeAmmo))
          return;
        if(grenadeAmmo.Count > 0)
        {
          Entity<GunComponent> wrap = (ent, grenade);
          RaiseLocalEvent<GunComponent>(wrap); 
          return;
        }
        // Even though shooting the gun is "default" behavior I needed the trygetcontainer working I just used this space and my linter
        if (!Containers.TryGetContainer(ent.Comp.Holder.GetValueOrDefault(), MagazineSlot, out var container) ||
            container is not ContainerSlot slot)
        {
            return;
        }
        Entity<GunComponent> wrapper = (ent.Comp.Holder.GetValueOrDefault(),hold);
        RaiseLocalEvent<GunComponent>(wrapper);
        //RaiseLocalEvent<RMCFlamerAmmoProviderComponent>(wrapper);
    }


}
