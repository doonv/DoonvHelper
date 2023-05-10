local customNPC = {}

customNPC.name = "DoonvHelper/CustomNPC"
customNPC.fieldInformation = {
    aiType = {
        options = {
            { "Swim",         "Swim" },
            { "Fly",          "Fly" },
            { "Fly (Tied)",   "FlyTied" },
            { "Smart Fly",    "SmartFly" },
            { "Node Walk",    "NodeWalk" },
            { "Chase Walk",   "ChaseWalk" },
            { "Wander",       "Wander" },
            { "Walk & Climb", "WalkNClimb" },
            { "Chase Jump",   "ChaseJump" },
        },
        editable = false,
    },
    facing = {
        options = {
            { "Movement (Flip)",   "MovementFlip" },
            { "Movement (Rotate)", "MovementRotate" },
        },
        editable = false,
    },
    hitboxHeight = { minimumValue = 0.0 },
    hitboxWidth = { minimumValue = 0.0 },
}
customNPC.fieldOrder = {
    "x", "y",
    "XSpeed", "YSpeed",
    "acceleration", "jumpHeight",
    "aiType", "spriteID",
    "hitboxXOffset", "hitboxYOffset",
    "hitboxWidth", "hitboxHeight",
    "facing", "waitForMovement", "outlineEnabled"
}
customNPC.depth = 1
customNPC.justification = { 0.5, 1 }
customNPC.texture = "characters/badeline/sleep00"
customNPC.nodeLimits = { 0, -1 }
customNPC.nodeLineRenderType = "line"
customNPC.nodeVisibility = "selected"
customNPC.placements = {
    name = "normal",
    data = {
        XSpeed = 48.0,
        YSpeed = 240,
        acceleration = 240,
        jumpHeight = 200,
        aiType = "Wander",
        hitboxHeight = 16,
        hitboxWidth = 16,
        hitboxXOffset = 0,
        hitboxYOffset = 0,
        facing = "MovementFlip",
        spriteID = "DoonvHelper_CustomNPC_zombie",
        waitForMovement = true,
        outlineEnabled = false
    }
}

return customNPC
