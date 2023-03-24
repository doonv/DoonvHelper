local customNPC = {}

customNPC.name = "DoonvHelper/CustomNPC"
customNPC.fieldInformation = {
    aiType = {
        options = {"Swim", "Fly", "Smart Fly", "Node Walk", "Smart Walk", "Wander"},
        editable = false
    },
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
        maxSpeed = 32.0,
        acceleration = 6.0, 
        aiType = "Wander",
        hitboxHeight = 16,
        hitboxWidth = 16,
        hitboxXOffset = 0,
        hitboxYOffset = 0,
        fallSpeed = 69.420,
        faceMovement = false,
        spriteID = "DoonvHelper_CustomNPC_zombie",
        waitForMovement = true,
    }
}


return customNPC