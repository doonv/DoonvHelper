module DoonvHelperSampleTrigger

using ..Ahorn, Maple

@mapdef Trigger "DoonvHelper/SampleTrigger" SampleTrigger(
    x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight,
    sampleProperty::Integer=0
)

const placements = Ahorn.PlacementDict(
    "Sample Trigger (DoonvHelper)" => Ahorn.EntityPlacement(
        SampleTrigger,
        "rectangle",
    )
)

end