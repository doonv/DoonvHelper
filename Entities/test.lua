function onBegin()
	local guide = getEntity("DialogNPC", "Celeste.Mod.DoonvHelper.Entities.")
	disableMovement()
	guide.CutsceneModeEnabled = true
	say("CC_KoseiDiamond_guide_greetings")
	if choice("CC_KoseiDiamond_guide_dialogue1_1", "CC_KoseiDiamond_guide_dialogue1_2") == 1 then
		say("CC_KoseiDiamond_guide_dialogue2_1")
	else
		say("CC_KoseiDiamond_guide_dialogue2_2")
	end
	guide.CutsceneModeEnabled = false
	enableMovement()
end

function onEnd()
	local guide = getEntity("DialogNPC", "Celeste.Mod.DoonvHelper.Entities.")
	guide.CutsceneModeEnabled = false
	enableMovement()
end