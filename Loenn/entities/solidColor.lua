local utils = require("utils")

local solidColor = {}
solidColor.name = "DoonvHelper/SolidColor"
solidColor.fieldInformation = {
    depth = {
        fieldType = "integer"
    },
    color = {
        fieldType = "color"
    },
    alpha = {
        minimumValue = 0.0,
        maximumValue = 1.0
    }
}
solidColor.placements = {
    name = "normal",
    data = {
        width = 8,
        height = 8,
        depth = 5000,
        color = "6969ee",
        alpha = 1.0
    }
}

function solidColor.depth(room, entity, viewport)
    if entity.depth then
        return entity.depth
    end
    return 5000
end

function solidColor.color(room, entity)
    local color = {0.411, 0.411, 0.933}
    if entity.color then
        local success, r, g, b = utils.parseHexColor(entity.color)
        if success then
            color = {r, g, b}
        end
    end
    return color
end

return solidColor