using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.RifleGrenade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCRifleGrenadeSystem))]
public sealed partial class RMCRifleGrenadeComponent : Component, IShootable
  {
    [DataField, AutoNetworkedField]
    public EntityUid? Holder = null;
  }
