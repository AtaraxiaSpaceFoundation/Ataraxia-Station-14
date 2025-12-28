using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server._Orion.Arousal.Components;

//
// License-Identifier: AGPL-3.0-or-later
//

[RegisterComponent]
public sealed partial class ArousalComponent : Component
{
    [DataField]
    public float ArousalCurrent { get; set; }

    /// <summary>
    /// Sensitivity modifier (ranged from 0.0 to 1.0).
    /// Higher values mean slower arousal decay.
    /// </summary>
    [DataField]
    public float ArousalModifier { get; set; } = 0.5f;

    /// <summary>
    /// Arousal decay rate - decreases over time.
    /// This value is modified by ArousalModifier.
    /// </summary>
    [DataField]
    public float ArousalDecayRate { get; set; } = 0.5f;

    public float MinArousal = 0.0f;

    public float MaxArousal = 100.0f;

    [DataField]
    public float HighArousalThreshold = 70.0f;

    [DataField]
    public bool IsHighArousal { get; set; }

    public float LastUpdateTime { get; set; } = -1.0f;

    /// <summary>
    /// The alert to show to owners of this component.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> ArousalAlert = "Arousal";
}
