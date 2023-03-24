local mods = require("mods")
local customEnemy = mods.requireFromPlugin('entities.customNPC', 'DoonvHelper')

customEnemy.name = "DoonvHelper/CustomEnemy"
for k, v in pairs({
    health = { fieldType = "integer" },
}) do customEnemy.fieldInformation[k] = v end
for k, v in pairs({
    health = 1,
}) do customEnemy.placements.data[k] = v end


return customEnemy