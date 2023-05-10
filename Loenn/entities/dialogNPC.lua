local dialogNPC = require("utils").deepcopy(require("mods").requireFromPlugin("entities.customNPC", "DoonvHelper"))

dialogNPC.name = "DoonvHelper/DialogNPC"
for k, v in pairs({
    talkBoundsWidth = { fieldType = "integer" },
    talkBoundsHeight = { fieldType = "integer" },
}) do dialogNPC.fieldInformation[k] = v end
for k, v in pairs({
    talkBoundsWidth = 80,
    talkBoundsHeight = 40,
    talkIndicatorX = 0,
    talkIndicatorY = 0,
    basicDialogID = "",
    luaCutscene = "",
    csEventID = ""
}) do dialogNPC.placements.data[k] = v end

return dialogNPC
