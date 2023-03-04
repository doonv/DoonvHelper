local utils = require("utils")
local loadedState = require("loaded_state")
local logging = require("logging")

return {
    name = "DoonvHelper/CustomBadelineBoost",
    depth = -1000000,
    nodeLineRenderType = "line",
    texture = "objects/badelineboost/idle00",
    nodeLimits = {0, -1},
    fieldInformation = {
        ambientParticle1 = {
            fieldType = "color"
        },
        ambientParticle2 = {
            fieldType = "color"
        },
        moveParticleColor = {
            fieldType = "color"
        },
        moveColor = {
            fieldType = "color"
        },
        cutsceneTeleport = {
            fieldType = "DoonvHelper.room_name_or_empty"
        },
        goldenTeleport = {
            fieldType = "DoonvHelper.room_name_or_empty"
        }
    },
    fieldOrder = {
        "x", "y",
        "cutsceneTeleport", 
        "goldenTeleport", 
        "preLaunchDialog", 
        "cutsceneBird",
        "lockCamera",
        "ambientParticle1",
        "ambientParticle2",
        "moveColor",
        "moveParticleColor",
        "moveImage",
        "canSkip",
    },
    placements = {
        name = "normal",
        data = {
            lockCamera = false,
            canSkip = false,
            preLaunchDialog = "",
            cutsceneTeleport = "",
            goldenTeleport = "",
            cutsceneBird = true,
            ambientParticle1 = "f78ae7",
            ambientParticle2 = "ffccf7",
            moveParticleColor = "e0a8d8",
            moveColor = "ff6def",
            moveImage = ""
        }
    }
}


