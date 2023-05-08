local setFlagOnEntityCount = {}
setFlagOnEntityCount.name = "DoonvHelper/SetFlagOnEntityCount"
setFlagOnEntityCount.fieldInformation = {
    operator = { options = {
        {"Equal to", "EqualTo"},
        {"Greater than", "GreaterThan"},
        {"Less than", "LessThan"},
    }, editable = false },
    check = { options = {
        {"Always", "Always"},
        {"On Enter", "OnEnter"},
        {"On Stay", "OnStay"},
        {"On Leave", "OnLeave"},
    }, editable = false },
    count = { fieldType = "integer" },
}
setFlagOnEntityCount.placements = {
    name = "normal",
    data = {
        width = 8,
        height = 8,
        entityIDs = "",
        operator = "EqualTo",
        count = 0,
        flag = "",
        check = "OnEnter"
    }
}

return setFlagOnEntityCount