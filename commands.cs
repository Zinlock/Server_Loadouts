// * === Setup commands ===

function serverCmdLoHelp(%cl, %cat)
{
	if(!%cl.isSuperAdmin)
	{
		messageClient(%cl, '', "\c5You are not a super admin.");
		return;
	}

	%cat = trim(%cat);

	if(%cat $= "")
	{
		messageClient(%cl, '', "\c6Server_Loadouts by \c2Oxy (260031)\c6: help menus");
		messageClient(%cl, '', "  \c3/loHelp \c2general\c6 - explains what sets, types and randomizers do.");
		messageClient(%cl, '', "  \c3/loHelp \c2sets\c6 - explains how to create sets.");
		messageClient(%cl, '', "  \c3/loHelp \c2cmds\c6 - displays all setup commands.");
	}
	else if(%cat $= "general")
	{
		messageClient(%cl, '', "\c6Loadouts: \c2general");
		messageClient(%cl, '', "  \c6Loadout sets control what loadout options and classes are available to players.");
		messageClient(%cl, '', "  \c6Types control how players pick and choose their weapons.");
		messageClient(%cl, '', "    \c6A class set allows players to pick between different classes with entirely predefined loadouts.");
		messageClient(%cl, '', "    \c6A loadout set allows players to pick a class and then choose which weapons go in which slots freely.");
		messageClient(%cl, '', "  \c6Sets can also be randomized, so players get a random class/loadout combo when spawning.");
		messageClient(%cl, '', "  \c6For asymmetrical gamemodes, sets don't have to apply to the entire minigame. Specific teams may have their own loadout sets.");
		messageClient(%cl, '', "  \c6You can also make players spawn without their selected loadout and provide a way for them to acquire it through events."); // * or through code...
	}
	else if(%cat $= "sets")
	{
		messageClient(%cl, '', "\c6Loadouts: \c2creating sets");
		messageClient(%cl, '', "  \c6Loadout sets are created by going into \c2config/server/loadouts/sets/\c6 and creating your own .set text file.");
		messageClient(%cl, '', "  \c6Two example sets should also be there as references.");
		messageClient(%cl, '', "  \c6Once created or updated, run \c3/loReloadSets\c6 to update the set list.");
		messageClient(%cl, '', "  \c6Your new set can then be used right away.");
	}
	else if(%cat $= "cmds")
	{
		messageClient(%cl, '', "\c6Loadouts: \c2commands");
		messageClient(%cl, '', "  \c3/loReloadSets\c6 - reloads all sets.");
		messageClient(%cl, '', "  \c3/loSetList\c6 - dumps a list of all sets that currently exist.");
		// messageClient(%cl, '', "  \c3/loSetInfo \c2[name]\c6 - dumps info about a set.");
		// messageClient(%cl, '', "  \c3/loSetEdit \c2[name]\c6 - brings up the editor menu for a set.");
		// messageClient(%cl, '', "  \c3/loSetDelete \c2[name]\c6 - deletes a set. must be called twice to confirm");
		// messageClient(%cl, '', "  \c3/loSetCreate \c2[name] [type]\c6 - creates a new set. type can be class or loadout");
	}
}

function serverCmdLoReloadSets(%cl)
{
	if(!%cl.isSuperAdmin)
	{
		messageClient(%cl, '', "\c5You are not a super admin.");
		return;
	}

	messageClient(%cl, '', "\c5Reloading all sets...");
	LOReloadSets();
}

function serverCmdLoSetList(%cl)
{
	if(!%cl.isSuperAdmin)
	{
		messageClient(%cl, '', "\c5You are not a super admin.");
		return;
	}

	messageClient(%cl, '', "\c2" @ $LOSetCount @ " sets:");
	for(%i = 0; %i < $LOSetCount; %i++)
	{
		%set = $LOSet[%i];

		%mode = %set.get(randomizer);
		if(%mode $= "on")
			%rand = ", randomizer";
		else if(%mode $= "static")
			%rand = ", randomizer (static)";
		else if(%mode $= "staticTeams")
			%rand = ", randomizer (static teams)";

		messageClient(%cl, '', '\c2%1\c6: %2 type, %3 classes%4', $LOSetName[%i], %set.get(type), %set.listNum(classIds), %rand);
	}
}

// * === Player commands ===

function serverCmdLoadout(%cl)
{
	if(!isObject(%set = %cl.LOGetActiveSet()) || %set.get(randomizer) !$= "off")
		return;

	%type = %set.get(type);

	if(%type $= "loadout")
	{
		if(!isObject(%cl.loadoutMenu))
			%cl.LOUpdateLoadoutMenu();

		%cl.loadoutMenu.open(%cl);
	}
	else if(%type $= "class")
	{
		%cl.LOValidateLoadout();

		if(isObject(%menu = $LOSetMenu[%set.loadoutIdx]))
			%menu.open(%cl);
	}
}

function serverCmdLoadouts(%cl) { serverCmdLoadout(%cl); }
function serverCmdClass(%cl) { serverCmdLoadout(%cl); }

if($ServerCusBindCount $= "") $ServerCusBindCount = 0;

$ServerCusBind[$ServerCusBindCount] = "Loadouts" TAB "Loadout Menu" TAB "" TAB "loadouts";
$ServerCusBindDefault[$ServerCusBindCount] = "keyboard" TAB "f4";
$ServerCusBindCount++;