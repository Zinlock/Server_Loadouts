// function quickRegisterPref(%name, %cat, %pref, %type, %default)
// {
// 	%pref = trim(%pref);

// 	if(getSubStr(%pref, 0, 1) !$= "$")
// 		%pref = "$" @ %pref;

// 	eval("if(" @ %pref @ " $= \"\") " @ %pref @ " = \"" @ %default @ "\";");

// 	RTB_registerPref(%name, %cat, %pref, %type, fileBase(filePath($Con::File)), %default, false, false, "");
// }

// quickRegisterPref("Enable in Non-Slayer Minigames", "Loadouts", "$Pref::Loadouts::defaultMiniSupport", "bool", false);
// quickRegisterPref("Default Set", "Loadouts", "$Pref::Loadouts::defaultSet", "string 32", "");

if($AddOn__GameMode_Slayer && isFile("Add-Ons/GameMode_Slayer/server.cs"))
{
	forceRequiredAddon("GameMode_Slayer");

	new ScriptObject(Slayer_PrefSO : Slayer_DefaultPrefSO)
	{
		category = "Loadouts";
		title = "Default Loadout Set";
		guiTag = "advanced";

		variable = "%mini.LODefaultSet";
		type = "string";
		string_maxLength = 64;
		defaultValue = "";

		permissionLevel = $Slayer::PermissionLevel["Owner"];
		notifyPlayersOnChange = true;
		requiresMiniGameReset = false;
		requiresServerRestart = false;
		priority = 1000;
	};

	new ScriptObject(Slayer_PrefSO : Slayer_DefaultPrefSO)
	{
		category = "Loadouts";
		title = "Apply Loadouts on Spawn";
		guiTag = "advanced";

		variable = "%mini.LOApplyOnSpawn";
		type = "bool";
		defaultValue = true;

		permissionLevel = $Slayer::PermissionLevel["Owner"];
		notifyPlayersOnChange = true;
		requiresMiniGameReset = false;
		requiresServerRestart = false;
		priority = 1000;
	};

	new ScriptObject(Slayer_TeamPrefSO : Slayer_DefaultTeamPrefSO)
	{
		category = "Loadouts";
		title = "Override Minigame Loadout Set";

		variable = "LOSetOverride";
		type = "string";
		string_maxLength = 64;
		defaultValue = "";

		permissionLevel = $Slayer::PermissionLevel["Owner"];
		guiTag = "advanced";
	};
}