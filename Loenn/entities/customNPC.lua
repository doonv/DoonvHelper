local customNPC = {}

customNPC.name = "DoonvHelper/CustomNPC"
customNPC.fieldInformation = {
    aiType = {
        options = {"Swim", "Fly", "Smart Fly", "Node Walk", "Chase Walk", "Wander", "Walk & Climb", "Chase Jump"},
        editable = false
    },
}
customNPC.fieldOrder = {
    "x", "y", 
    "XSpeed", "YSpeed",
    "acceleration", "jumpHeight",
    "aiType", "spriteID",
    "hitboxXOffset", "hitboxYOffset",
    "hitboxWidth", "hitboxHeight",
    "faceMovement", "waitForMovement", "outlineEnabled"
}
customNPC.depth = 1
customNPC.justification = {0.5, 1}
customNPC.texture = "characters/badeline/sleep00"
customNPC.nodeLimits = {0, -1}
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
        faceMovement = false,
        spriteID = "DoonvHelper_CustomNPC_zombie",
        waitForMovement = true,
        outlineEnabled = true,
        invincible = false
    }
}

return customNPC