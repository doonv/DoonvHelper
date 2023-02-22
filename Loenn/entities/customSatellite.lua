local drawableSprite = require("structs.drawable_sprite")
local utils = require("utils")

local function isValidListOfDashes(str)
    -- this function was coded by chatgpt
    -- thanks chatgpt
    if string.match(str, "^,") ~= nil or string.match(str, ",$") ~= nil then
        return false
    end
    local values = {}
    for val in string.upper(str):gmatch("[^,]+") do
      table.insert(values, val)
    end

    for _, val in ipairs(values) do
      if val ~= "L" and val ~= "R" and val ~= "U" and val ~= "D" and val ~= "UL" and val ~= "UR" and val ~= "DL" and val ~= "DR" then
        return false
      end
    end

    return true
end

local function isStringInTable(str, tbl)
    for i = 1, #tbl do
      if tbl[i] == str then
        return true
      end
    end
    return false
end
  

local customSatellite = {}

customSatellite.name = "DoonvHelper/CustomSatellite"
customSatellite.depth = 8999
customSatellite.nodeLineRenderType = "line"
customSatellite.nodeLimits = {2, 2}
customSatellite.fieldInformation = {
    dashSequence = {
        validator = isValidListOfDashes
    },
    volume = {
        minimumValue = 0.0
    }
}
customSatellite.placements = {
    name = "normal",
    data = {
        dashSequence = "U,L,DR,UR,L,UL",
        cosmetic = false,
        completeFlag = "unlocked_satellite",
        volume = 1.0,
        requireSight = true
    }
}

local birdTexture = "scenery/flutterbird/flap01"
local gemTexture = "collectables/heartGem/0/00"

local dishTexture = "objects/citysatellite/dish"
local lightTexture = "objects/citysatellite/light"
local computerTexture = "objects/citysatellite/computer"
local computerScreenTexture = "objects/citysatellite/computerscreen"

local computerOffsetX, computerOffsetY = 32, 24
local birdFlightDistance = 64

local codeColors = {
    L = {0.5686, 0.4431, 0.9490},
    R = {0.608, 1.000, 0.659},
    U = {0.9412, 0.9412, 0.9412},
    D = {0.310, 0.231, 0.357},
    UL = {1, 0.8039, 0.2157},
    UR = {0.7020, 0.1765, 0},
    DL = {0.145, 0.608, 0.561},
    DR = {0.0392, 0.2667, 0.8784},
}

function customSatellite.sprite(room, entity)
    local dishSprite = drawableSprite.fromTexture(dishTexture, entity)
    dishSprite:setJustification(0.5, 1.0)

    local lightSprite = drawableSprite.fromTexture(lightTexture, entity)
    lightSprite:setJustification(0.5, 1.0)

    local computerSprite = drawableSprite.fromTexture(computerTexture, entity)
    computerSprite:addPosition(computerOffsetX, computerOffsetY)

    local computerScreenSprite = drawableSprite.fromTexture(computerScreenTexture, entity)
    computerScreenSprite:addPosition(computerOffsetX, computerOffsetY)

    return {
        dishSprite, lightSprite, computerSprite, computerScreenSprite
    }
end

local function getBirdSprites(node, entity)
    local sprites = {}
    
    local possibleCodes = {}
    for word in string.gmatch(entity.dashSequence, "([^,]+)") do
        table.insert(possibleCodes, word)
    end

    for code, color in pairs(codeColors) do
        if isStringInTable(code, possibleCodes) then
            local sprite = drawableSprite.fromTexture(birdTexture, node)
        
            local directionX = string.find(code, "L") and -1 or string.find(code, "R") and 1 or 0
            local directionY = string.find(code, "U") and -1 or string.find(code, "D") and 1 or 0
            local offsetX = directionX * birdFlightDistance
            local offsetY = directionY * birdFlightDistance
            local magnitude = math.sqrt(offsetX^2 + offsetY^2)

            sprite:addPosition(offsetX / magnitude * birdFlightDistance, offsetY / magnitude * birdFlightDistance)
            sprite:setColor(color)

            if offsetX == -1 then
                sprite.scaleX = -1
            end

            table.insert(sprites, sprite)
        end
        
    end

    return sprites
end

function customSatellite.nodeSprite(room, entity, node, nodeIndex)
    if nodeIndex == 1 then
        return getBirdSprites(node, entity)

    else
        local gemSprite = drawableSprite.fromTexture(gemTexture, node)

        return gemSprite
    end
end

function customSatellite.nodeRectangle(room, entity, node, nodeIndex)
    if nodeIndex == 1 then
        return utils.rectangle(node.x - 8, node.y - 8, 16, 16)

    else
        local gemSprite = drawableSprite.fromTexture(gemTexture, node)

        return gemSprite:getRectangle()
    end
end

return customSatellite