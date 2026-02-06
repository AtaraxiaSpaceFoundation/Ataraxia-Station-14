using Robust.Shared.Audio;

namespace Content.Shared._Orion.Vibrator;

[RegisterComponent]
public sealed partial class VibratorComponent : Component
{
    [ViewVariables]
    public EntityUid? User = null;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive = false;

    [DataField]
    public int JitterProbability = 40;

    [DataField]
    public bool IsTogglable;

    [DataField]
    public SoundSpecifier? VibrationSound;

    [DataField]
    public AudioParams AudioParams = AudioParams.Default.WithVolume(-8f).WithVariation(0.25f).WithLoop(true).WithMaxDistance(1);

    public EntityUid? Stream;

    /// <summary>
    ///     Active vibration arousal amount.
    /// </summary>
    [DataField]
    public float ActiveArousalAmount = 15f;

    /// <summary>
    ///     Equip/Unequip arousal amount.
    /// </summary>
    [DataField]
    public float ArousalAmount = 10f;

    [DataField]
    public VibratorIntensity Intensity = VibratorIntensity.Off;
}

public enum VibratorIntensity : byte
{
    Off = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

public enum VibratorPort : byte
{
    On,
    Off,
    Toggle,
    SetLow,
    SetMedium,
    SetHigh,
    SetIntensity,
}

public enum VibratorVisuals : byte
{
    Intensity,
}
