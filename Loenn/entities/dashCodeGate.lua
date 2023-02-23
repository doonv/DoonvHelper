local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local enums = require("consts.celeste_enums")
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

local gate = {}

local textures = {
    "block", "mirror", "temple", "stars"
}
local textureOptions = {}

for _, texture in ipairs(textures) do
    textureOptions[utils.titleCase(texture)] = texture
end

gate.name = "DoonvHelper/DashCodeGate"
gate.depth = 0
gate.nodeLimits = {1, 1}
gate.nodeLineRenderType = "line"
gate.minimumSize = {16, 16}
gate.fieldInformation = {
    sprite = {
        options = textureOptions
    },
    code = {
        validator = isValidListOfDashes
    },
    iconDirection = {
        options = {"Horizontal", "Vertical", "Auto"},
        editable = false
    }
}

gate.placements = {
    name = "normal",
    data = {
        width = 16,
        height = 16,
        sprite = "stars",
        persistenceFlag = "",
        iconDirection = "Auto",
        code = "U,D,L,R"
    }
}

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

local frameTexture = "objects/switchgate/%s"
local middleTexture = "objects/switchgate/icon00"

function gate.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local blockSprite = entity.sprite or "block"
    local frame = string.format(frameTexture, blockSprite)

    local ninePatch = drawableNinePatch.fromTexture(frame, ninePatchOptions, x, y, width, height)
    -- local middleSprite = drawableSprite.fromTexture(middleTexture, entity)
    local sprites = ninePatch:getDrawableSprite()

    -- middleSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    -- table.insert(sprites, middleSprite)

    return sprites
end

function gate.selection(room, entity)
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local nodeX, nodeY = nodes[1].x or x, nodes[1].y or y
    local width, height = entity.width or 24, entity.height or 24

    return utils.rectangle(x, y, width, height), {utils.rectangle(nodeX, nodeY, width, height)}
end

return gate