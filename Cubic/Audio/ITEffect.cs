namespace Cubic.Audio;

// Represents impulse tracker effects - all other module effects are internally converted to these effects.
public enum Effect
{
    None,
    SetSpeed,
    PositionJump,
    PatternBreak,
    VolumeSlide,
    PortamentoDown,
    PortamentoUp,
    TonePortamento,
    Vibrato,
    Tremor,
    Arpreggio,
    VolumeSlideAndVibrato,
    VolumeSlideAndTonePortamento,
    SetChannelVolume,
    ChannelVolumeSlide,
    SampleOffset,
    PanningSlide,
    Retrigger,
    Tremolo,
    Special,
    SetTempo,
    FineVibrato,
    SetGlobalVolume,
    GlobalVolumeSlide,
    SetPanning,
    Panbrello,
}