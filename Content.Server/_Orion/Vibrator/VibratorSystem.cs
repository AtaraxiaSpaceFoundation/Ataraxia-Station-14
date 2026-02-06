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
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ArousalSystem _arousalSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VibratorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<VibratorComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<VibratorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<VibratorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<VibratorComponent, ItemToggledEvent>(OnItemToggled);
        SubscribeLocalEvent<VibratorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, VibratorComponent component, ComponentInit args)
    {
        UpdateVisuals(uid, component);
    }

    private void OnRemove(EntityUid uid, VibratorComponent component, ComponentRemove args)
    {
        if (component.Stream != null)
            _audioSystem.Stop(component.Stream.Value);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VibratorComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (component.User is null || !component.IsActive)
                continue;

            if (EntityManager.HasComponent<ArousalComponent>(component.User.Value))
            {
                var arousalRate = GetArousalRate(component.Intensity);
                _arousalSystem.IncreaseArousal(component.User.Value, arousalRate * frameTime);
            }

            var jitterChancePerSecond = GetJitterChance(component.Intensity) / 100f;
            var jitterChancePerFrame = 1f - MathF.Pow(1f - jitterChancePerSecond, frameTime);
            if (_random.Prob(jitterChancePerFrame))
                _jitter.DoJitter(component.User.Value, TimeSpan.FromSeconds(1), true, 2, 2);
        }
    }

    private float GetArousalRate(VibratorIntensity intensity)
    {
        return intensity switch
        {
            VibratorIntensity.Low => 2f,
            VibratorIntensity.Medium => 5f,
            VibratorIntensity.High => 10f,
            _ => 0f,
        };
    }

    private int GetJitterChance(VibratorIntensity intensity)
    {
        return intensity switch
        {
            VibratorIntensity.Low => 10,
            VibratorIntensity.Medium => 25,
            VibratorIntensity.High => 50,
            _ => 0,
        };
    }

    private void OnEquipped(EntityUid uid, VibratorComponent component, ref ClothingGotEquippedEvent args)
    {
        component.User = args.Wearer;

        if (EntityManager.HasComponent<ArousalComponent>(component.User.Value))
            _arousalSystem.IncreaseArousal(component.User.Value, component.ArousalAmount);

        UpdateVisuals(uid, component);
    }

    private void OnUnequipped(EntityUid uid, VibratorComponent component, ref ClothingGotUnequippedEvent args)
    {
        var user = component.User;
        component.User = null;

        if (user is { } userId && EntityManager.HasComponent<ArousalComponent>(userId))
            _arousalSystem.IncreaseArousal(userId, component.ArousalAmount);

        UpdateVisuals(uid, component);
    }

    private void OnItemToggled(EntityUid uid, VibratorComponent component, ItemToggledEvent args)
    {
        component.IsActive = args.Activated;
        if (!args.Activated)
            component.Intensity = VibratorIntensity.Off;

        _audioSystem.Stop(component.Stream);

        if (args.Activated && component.Intensity != VibratorIntensity.Off)
            component.Stream = _audioSystem.PlayPvs(component.VibrationSound, uid, component.AudioParams)?.Entity;

        UpdateVisuals(uid, component);
    }

    private void OnSignalReceived(EntityUid uid, VibratorComponent component, SignalReceivedEvent args)
    {
        switch (args.Port)
        {
            case "On":
                Activate(uid);
                if (component.Intensity == VibratorIntensity.Off)
                    SetIntensity(uid, component, VibratorIntensity.Low);
                break;
            case "Off":
                Deactivate(uid, component);
                break;
            case "Toggle":
                if (component.IsActive)
                    Deactivate(uid, component);
                else
                    Activate(uid);
                break;
            case "SetLow":
                SetIntensity(uid, component, VibratorIntensity.Low);
                break;
            case "SetMedium":
                SetIntensity(uid, component, VibratorIntensity.Medium);
                break;
            case "SetHigh":
                SetIntensity(uid, component, VibratorIntensity.High);
                break;
            case "SetIntensity":
                if (args.Data != null &&
                    args.Data.TryGetValue("intensity", out var intensityObj) &&
                    Enum.TryParse<VibratorIntensity>(intensityObj?.ToString(), out var intensity))
                {
                    if (intensity == VibratorIntensity.Off)
                        Deactivate(uid, component);
                    else
                        SetIntensity(uid, component, intensity);
                }
                break;
        }
    }

    private void Activate(EntityUid uid)
    {
        _itemToggleSystem.TryActivate(uid);
    }

    private void Deactivate(EntityUid uid, VibratorComponent component)
    {
        _itemToggleSystem.TryDeactivate(uid);
        SetIntensity(uid, component, VibratorIntensity.Off);
    }

    private void SetIntensity(EntityUid uid, VibratorComponent component, VibratorIntensity intensity)
    {
        if (!component.IsActive && intensity != VibratorIntensity.Off)
            Activate(uid);

        if (intensity != VibratorIntensity.Off && component.Intensity == VibratorIntensity.Off)
            intensity = intensity;

        component.Intensity = intensity;

        _audioSystem.Stop(component.Stream);
        if (component.IsActive && intensity != VibratorIntensity.Off)
            component.Stream = _audioSystem.PlayPvs(component.VibrationSound, uid, component.AudioParams)?.Entity;

        UpdateVisuals(uid, component);
    }

    private void UpdateVisuals(EntityUid uid, VibratorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _appearance.SetData(uid, VibratorVisuals.Intensity, component.Intensity);
    }
}
