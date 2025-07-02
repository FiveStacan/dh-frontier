using System.Numerics;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Spawners.EntitySystems
{
    [UsedImplicitly]
    public sealed class DHSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        // Для работы с таблицей сущностей
        [Dependency] private readonly EntityTableSystem _entityTable = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DHTimedSpawnerComponent, MapInitEvent>(OnMapInit);
            // Можно добавить подписки для других типов спавнеров, если нужно
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var curTime = _timing.CurTime;
            var query = EntityQueryEnumerator<DHTimedSpawnerComponent>();
            while (query.MoveNext(out var uid, out var timedSpawner))
            {
                if (timedSpawner.NextFire > curTime)
                    continue;

                OnTimerFired(uid, timedSpawner);

                timedSpawner.NextFire += timedSpawner.IntervalSeconds;
            }
        }

        private void OnMapInit(Entity<DHTimedSpawnerComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.NextFire = _timing.CurTime + ent.Comp.IntervalSeconds;
        }

        private void OnTimerFired(EntityUid uid, DHTimedSpawnerComponent component)
        {
            if (!_robustRandom.Prob(component.Chance))
                return;

            var number = _robustRandom.Next(component.MinimumEntitiesSpawned, component.MaximumEntitiesSpawned);
            var coordinates = Transform(uid).Coordinates;

            for (var i = 0; i < number; i++)
            {
                var entityProto = _robustRandom.Pick(component.Prototypes);
                SpawnAtPosition(entityProto, coordinates);
            }
        }

        // Новый метод для спавна из таблицы или списка прототипов
        public void SpawnFromTableOrPrototypes(EntityUid uid, DHTimedSpawnerComponent component)
        {
            if (component == null)
                return;

            var coords = Transform(uid).Coordinates;

            // Если есть таблица

            if (component.Table != null && component.Table.Count > 0)
            {
                var spawns = _entityTable.GetSpawns(component.Table);
                foreach (var proto in spawns)
                {
                    var xOffset = _robustRandom.NextFloat(-component.Offset, component.Offset);
                    var yOffset = _robustRandom.NextFloat(-component.Offset, component.Offset);
                    var trueCoords = coords.Offset(new Vector2(xOffset, yOffset));
                    SpawnAtPosition(proto, trueCoords);
                }
                return;
            }

            // Иначе спавнить из прототипов
            if (component.Prototypes == null || component.Prototypes.Count == 0)
                return;

            int count = 1;
            if (component.RandomCount)
                count = _robustRandom.Next(component.MinimumEntitiesSpawned, component.MaximumEntitiesSpawned);

            for (int i = 0; i < count; i++)
            {
                var proto = _robustRandom.Pick(component.Prototypes);
                //var xOffset = _robustRandom.NextFloat(-component.Offset, component.Offset);
                //var yOffset = _robustRandom.NextFloat(-component.Offset, component.Offset);
                var trueCoords = coords.Offset(new Vector2(xOffset, yOffset));
                SpawnAtPosition(proto, trueCoords);
            }
        }

        private void SpawnAtPosition(string prototypeId, in MapCoordinates coordinates)
        {
            EntityManager.SpawnEntity(prototypeId, coordinates);
        }
    }
}
