local comf = {}

comf.name = "DoonvHelper/ComfySpot"
comf.fieldInformation = {
    comfLevel = {
        fieldType = "integer",
        minimumValue = 0
    }
}
comf.placements = {
    name = "normal",
    data = {
        comfLevel = 1
    }
}

return comf