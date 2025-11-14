using Content.Server.Actions;
using Content.Server.Bed.Components;
using Content.Server.Body.Systems;
using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Bed;
using Content.Shared.Bed.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Bed
{
    public sealed class BedSystem : EntitySystem
    {
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly ActionsSystem _actionsSystem = default!;
        [Dependency] private readonly EmagSystem _emag = default!;
        [Dependency] private readonly SleepingSystem _sleepingSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        public override void Initialize()
        {
            base.Initialize();
            //SubscribeLocalEvent<HealOnBuckleComponent, StrappedEvent>(OnStrapped);
            //SubscribeLocalEvent<HealOnBuckleComponent, UnstrappedEvent>(OnUnstrapped);
            SubscribeLocalEvent<StasisBedComponent, StrappedEvent>(OnStasisStrapped);
            SubscribeLocalEvent<StasisBedComponent, UnstrappedEvent>(OnStasisUnstrapped);
            SubscribeLocalEvent<StasisBedComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<StasisBedComponent, GotEmaggedEvent>(OnEmagged);
            SubscribeLocalEvent<StasisBedComponent, GotUnEmaggedEvent>(OnUnemagged); // Frontier
            SubscribeLocalEvent<StasisBedComponent, RefreshPartsEvent>(OnRefreshParts); // Frontier
            SubscribeLocalEvent<StasisBedComponent, UpgradeExamineEvent>(OnUpgradeExamine); // Frontier
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<HealOnBuckleHealingComponent, HealOnBuckleComponent, StrapComponent>();
            while (query.MoveNext(out var uid, out _, out var bedComponent, out var strapComponent))
            {
                if (_timing.CurTime < bedComponent.NextHealTime)
                    continue;

                bedComponent.NextHealTime += TimeSpan.FromSeconds(bedComponent.HealTime);

                if (strapComponent.BuckledEntities.Count == 0)
                    continue;

                foreach (var healedEntity in strapComponent.BuckledEntities)
                {
                    if (_mobStateSystem.IsDead(healedEntity))
                        continue;

                    var damage = bedComponent.Damage;

                    if (HasComp<SleepingComponent>(healedEntity))
                        damage *= bedComponent.SleepMultiplier;

                    _damageableSystem.TryChangeDamage(healedEntity, damage, true, origin: uid);
                }
            }
        }
    }
}
