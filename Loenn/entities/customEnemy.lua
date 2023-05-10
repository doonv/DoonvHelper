local customEnemy = require("utils").deepcopy(require("mods").requireFromPlugin("entities.customNPC", "DoonvHelper"))

customEnemy.name = "DoonvHelper/CustomEnemy"
for k, v in pairs({
    health = { fieldType = "integer", minimumValue = 0 },
    bulletRecharge = { minimumValue = 0.0 },
    bulletSafeTime = { minimumValue = 0.0 },
    bounceboxHeight = { minimumValue = 0.0 },
    bounceboxWidth = { minimumValue = 0.0 },
}) do customEnemy.fieldInformation[k] = v end
for k, v in pairs({
    health = 1,
    bulletSpriteID = "badeline_projectile",
    bulletRecharge = 0.0,
    bulletSpeed = 300.0,
    bulletSafeTime = 0.25,
    bulletFaceMove = false,
    dashable = false,
    bounceboxHeight = 0,
    bounceboxWidth = 0,
    bounceboxXOffset = 0,
    bounceboxYOffset = 0,
}) do customEnemy.placements.data[k] = v end

return customEnemy
