using Content.Server._Orion.Arousal;
using Content.Server._Orion.Arousal.Components;
using Content.Server.Jittering;
using Content.Shared._Orion.Vibrator;
using Content.Shared.Clothing;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Orion.Vibrator;

public sealed class VibratorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ArousalSystem _arousalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VibratorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<VibratorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<VibratorComponent, ItemToggledEvent>(OnItemToggled);
        SubscribeLocalEvent<VibratorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VibratorComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (component.User is null || !component.IsActive)
                continue;

            if (_entityManager.HasComponent<ArousalComponent>(component.User.Value))
                _arousalSystem.IncreaseArousal(component.User.Value, component.ActiveArousalAmount * frameTime);

            if (_random.Next(1, 101) <= component.JitterProbability)
                _jitter.DoJitter(component.User.Value, TimeSpan.FromSeconds(1), true, 2, 2);
        }
    }

    private void OnEquipped(EntityUid uid, VibratorComponent component, ref ClothingGotEquippedEvent args)
    {
        component.User = args.Wearer;

        if (_entityManager.HasComponent<ArousalComponent>(component.User))
            _arousalSystem.IncreaseArousal(component.User.Value, component.ArousalAmount);
    }

    private void OnUnequipped(EntityUid uid, VibratorComponent component, ref ClothingGotUnequippedEvent args)
    {
        var user = component.User;
        component.User = null;

        if (user is { } userId && _entityManager.HasComponent<ArousalComponent>(userId))
            _arousalSystem.IncreaseArousal(userId, component.ArousalAmount);
    }

    private void OnItemToggled(EntityUid uid, VibratorComponent component, ItemToggledEvent args)
    {
        component.IsActive = args.Activated;

        _audioSystem.Stop(component.Stream);

        if (args.Activated)
            component.Stream = _audioSystem.PlayPvs(component.VibrationSound, uid, component.AudioParams)?.Entity;
    }

    private void OnSignalReceived(EntityUid uid, VibratorComponent component, SignalReceivedEvent args)
    {
        switch (args.Port)
        {
            case "On":
                _itemToggleSystem.TryActivate(uid);
                break;
            case "Off":
                _itemToggleSystem.TryDeactivate(uid);
                break;
            case "Toggle":
                if (component.IsActive)
                {
                    _itemToggleSystem.TryDeactivate(uid);
                    break;
                }
                _itemToggleSystem.TryActivate(uid);
                break;
        }
    }
}
