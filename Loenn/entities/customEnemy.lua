local customEnemy = require("utils").deepcopy(require("mods").requireFromPlugin("entities.customNPC", "DoonvHelper"))

customEnemy.name = "DoonvHelper/CustomEnemy"
customEnemy.fieldOrder = {
    "x", "y",
    "XSpeed", "YSpeed",
    "acceleration", "jumpHeight",
    "aiType", "spriteID",
    "hitboxWidth", "hitboxHeight",
    "hitboxXOffset", "hitboxYOffset",
    "facing", "waitForMovement", "outlineEnabled",
    "bounceboxXOffset", "bounceboxYOffset",
    "bounceboxWidth", "bounceboxHeight",
    "bulletRecharge", "bulletSpeed",
    "bulletSafeTime", "bulletFacing",
    "bulletSpriteID",
    "health", "deathSound", "dashable"
}
for k, v in pairs({
    health = { fieldType = "integer", minimumValue = 0 },
    bulletRecharge = { minimumValue = 0.0 },
    bulletSafeTime = { minimumValue = 0.0 },
    bounceboxHeight = { minimumValue = 0.0 },
    bounceboxWidth = { minimumValue = 0.0 },
    bulletFacing = customEnemy.fieldInformation["facing"],
}) do customEnemy.fieldInformation[k] = v end
for k, v in pairs({
    health = 1,
    deathSound = "event:/-",
    bulletSpriteID = "badeline_projectile",
    bulletRecharge = 0.0,
    bulletSpeed = 100.0,
    bulletSafeTime = 0.25,
    bulletFacing = "None",
    dashable = false,
    bounceboxHeight = 0.0,
    bounceboxWidth = 0.0,
    bounceboxXOffset = 0.0,
    bounceboxYOffset = 0.0,
}) do customEnemy.placements.data[k] = v end

return customEnemy
