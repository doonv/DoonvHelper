local customEnemy = require("utils").deepcopy(require("mods").requireFromPlugin('entities.customNPC', 'DoonvHelper'))

customEnemy.name = "DoonvHelper/CustomEnemy"
for k, v in pairs({
    health = { fieldType = "integer" },
    bulletRecharge = { minimumValue = 0.0 },
    bulletSafeTime = { minimumValue = 0.0 },
}) do customEnemy.fieldInformation[k] = v end
for k, v in pairs({
    health = 1,
    bulletSpriteID = "badeline_projectile",
    bulletRecharge = 0.0,
    bulletSpeed = 300.0,
    bulletSafeTime = 0.25
}) do customEnemy.placements.data[k] = v end

return customEnemy