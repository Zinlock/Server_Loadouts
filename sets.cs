$LOConfig = "config/server/loadouts";
$LOConfigSets = $LOConfig @ "/sets";
$LOConfigPlayers = $LOConfig @ "/players";

registerInputEvent(fxDtsBrick, "onLOEvent", "KillerPlayer Player" TAB "KillerClient GameConnection" TAB "Player Player" TAB "Client GameConnection" TAB "Minigame Minigame");

$InputDescription_["onLOEvent"] = "Used by Server_Loadouts. Never triggered on a normal brick.";

// * Reloads the list of available sets
function LOReloadSets()
{
	for(%i = 0; %i < $LOSetCount; %i++)
	{
		$LOSet[%i].getGroup().delete();

		if(isObject($LOSetMenu[%i]))
			$LOSetMenu[%i].delete();

		$LOSetIdx[%name] = "";
		$LOSetName[%i] = "";
	}

	$LOSetCount = 0;

	if(isFile(expandFilename("./exampleClassSet.txt")))
		fileCopyH(expandFilename("./exampleClassSet.txt"), $LOConfigSets @ "/exampleClassSet.set");

	if(isFile(expandFilename("./exampleLoadoutSet.txt")))
		fileCopyH(expandFilename("./exampleLoadoutSet.txt"), $LOConfigSets @ "/exampleLoadoutSet.set");

	%pat = $LOConfigSets @ "/*.set";
	%file = findFirstFile(%pat);
	while(isFile(%file))
	{
		%name = strreplace(fileBase(%file), "_", " ");
		%set = readInfoFile(%file);

		%file = findNextFile(%pat);

		if((%type = %set.get(type)) !$= "loadout" && %type !$= "class")
		{
			warn("LOReloadSets() - invalid type \"" @ %type @ "\" in set \"" @ %name @ "\" (must be either loadout or class)");
			continue;
		}
		else if((%cts = %set.listNum(classIds)) <= 0)
		{
			warn("LOReloadSets() - no class ids defined in set \"" @ %name @ "\"");
			continue;
		}

		%set.loadoutIdx = $LOSetCount;

		if(%type $= "loadout")
		{
			%baseRarity = restWords(%set.listGet(rarities, 0));
			%classes = %set.get(classes);

			for(%i = 0; %i < %cts; %i++)
			{
				%classId = %set.listGet(classIds, %i);
				%class = %classes.get(%classId);

				if(!isObject(%class))
				{
					warn("LOReloadSets() - set " @ %name @ " does not contain data for class " @ %class @ "");
					continue;
				}

				%slots = %class.get(slots);
				%order = %slots.get(order);

				%ct2 = getWordCount(%order);
				for(%w = 0; %w < %ct2; %w++)
				{
					%slot = getWord(%order, %w);

					if(%done[%class, %slot])
						continue;

					if(isFile(%list = expandLocalPath(%file, %slots.get(%slot))))
					{
						%str = fileRead(%list);
						%slots.listReplace(%slot, %str);
						%done[%class, %slot] = true;
					}

					%ct3 = %slots.listNum(%slot);
					for(%s = 0; %s < %ct3; %s++) // this code sucks
					{
						%entry = %slots.listGet(%slot, %s);

						%item = getField(%entry, 0);
						%tier = trim(getField(%entry, 1));

						lotalk("Entry " @ %entry @ ": " @ %item @ ", tier " @ %tier);

						if(%tier $= "")
							%tier = %baseRarity;

						if(%slots.tierItems[%slot, %tier] $= "")
							%slots.tierItems[%slot, %tier] = 0;

						%slots.tierItem[%slot, %tier, %slots.tierItems[%slot, %tier]] = %item;

						loTalk("Slot " @ %slot @ " tier " @ %tier @ " item " @ %slots.tierItems[%slot, %tier] @ ": " @ %slots.tierItem[%slot, %tier, %slots.tierItems[%slot, %tier]] @ "\n");

						%slots.tierItems[%slot, %tier]++;

						%slots.listSet(%slot, %s, %item);
					}
				}
			}
		}

		$LOSet[$LOSetCount] = %set;
		$LOSetMenu[$LOSetCount] = LOCreateSetMenu(%set);
		$LOSetIdx[%name] = $LOSetCount;
		$LOSetName[$LOSetCount] = %name;
		$LOSetSafeName[$LOSetCount] = getSafeVariableName(%name);
		$LOSetCount++;
	}

	%cts = ClientGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		%cl.LOImportAllLoadouts();
	}
}

schedule(0, 0, LOReloadSets);

function LOExportWeaponList(%path)
{
	%file = new FileObject();
	if(!%file.openForWrite(%path))
	{
		error("LOExportWeaponList() - could not write to file " @ %path);

		%file.close();
		%file.delete();
		return;
	}

	%cts = DataBlockGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%db = DataBlockGroup.getObject(%i);

		if(%db.getClassName() $= "ItemData" && %db.category $= "Weapon" && %db.uiName !$= "")
			%file.writeLine(%db.uiName);
	}

	%file.close();
	%file.delete();
}

// * Returns true if this weapon is used by any of this set's classes
function LOIsInSet(%weapon, %set)
{
	if(!isObject(%set) || %set.loadoutIdx $= "")
		return false;
	
	%weapon = trim(%weapon);

	if(%set.weaponInSet[%weapon] !$= "") // sets are deleted when updated, so this doesn't persist through reloads
		return %set.weaponInSet[%weapon];

	%found = false;

	%classes = %set.get(classes);
	%cts = %set.listNum(classIds);
	for(%i = 0; %i < %cts; %i++)
	{
		%class = %classes.get(%set.listGet(classIds, %i));
		%slots = %class.get(slots);

		if(isObject(%slots))
		{
			%order = %slots.get(order);

			%ct2 = getWordCount(%order);
		}
		else
			%ct2 = %class.listNum(slots);

		for(%o = 0; %o < %ct2; %o++)
		{
			if(LOIsInSlot(%weapon, %o, %class))
			{
				%found = true;
				break;
			}
		}

		if(%found)
			break;
	}

	%set.weaponInSet[%weapon] = %found;
	return %found;
}

// * Returns true if this class can hold this weapon in this slot
function LOIsInSlot(%weapon, %idx, %class)
{
	%slots = %class.get(slots);

	if(isObject(%slots))
	{
		%order = %slots.get(order);

		%slotId = getWord(%order, %idx);

		if(%slots.listFind(%slotId, trim(%weapon)) >= 0)
			return true;
	}
	else
	{
		if(%class.listGet(slots, %idx) $= trim(%weapon))
			return true;
	}

	return false;
}

// * Returns the list of slots this item fits in, formatted for prints
function LOGetMatchingSlotTitle(%item, %class)
{
	%matches = 0;

	%slots = %class.get(slots);

	if(isObject(%slots))
	{
		%order = %slots.get(order);
		%cts = getWordCount(%order);
		
		for(%i = 0; %i < %cts; %i++)
		{
			%slot = getWord(%order, %i);

			for(%s = 0; (%itemName = %slots.listGet(%slot, %s)) !$= ""; %s++)
			{
				if(trim(%itemName) $= trim(%item))
				{
					%match[%matches] = %slot;
					if(%matched[%slot])
						%matchMult[%slot] = true;

					%matched[%slot] = true;
					%matches++;
					break;
				}
			}
		}
	}
	else
	{
		%cts = %class.listNum(slots);

		for(%i = 0; (%itemName = %class.listGet(slots, %i)) !$= ""; %i++)
		{
			if(trim(%itemName) $= trim(%item))
			{
				%match[%matches] = %itemName;
				if(%matched[%i])
					%matchMult[%i] = true;

				%matched[%i] = true;
				%matches++;
				break;
			}
		}
	}

	if(%matches <= 0)
		return 0;
	
	%mult = false;

	%str = "";

	for(%i = 0; %i < %matches; %i++)
	{
		%slot = %match[%i];

		if(%done[%slot])
			continue;
		
		if(%matchMult[%slot])
			%mult = true;
		
		if(%i == 0)
			%str = %slot;
		else if(%i == %matches - 1)
			%str = %str @ " and " @ %slot;
		else
			%str = %str @ ", " @ %slot;
		
		%done[%slot] = true;
	}

	return (%mult ? 2 : 1) SPC trim(%str);
}

// * Get the loadout set currently in use by this client
function GameConnection::LOGetActiveSet(%cl)
{
	if((%idx = $LOSetIdx[%cl.LOSetOverride]) !$= "" ||
	   (%idx = $LOSetIdx[%cl.slyrTeam.LOSetOverride]) !$= "" ||
		 (%idx = $LOSetIdx[%cl.minigame.LODefaultSet]) !$= "")
		return $LOSet[%idx];

	return -1;
}

// * Get this client's loadout as a string
function GameConnection::LOGetLoadoutString(%cl)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set))
		return "";

	%str = %cl.LOClass;
	%class = %set.get(classes).get(%cl.LOClass);

	if(%set.get(type) $= "loadout" && isObject(%class))
	{
		%slots = %class.get(slots);
		%order = %slots.get(order);

		%cts = getWordCount(%order);
		for(%i = 0; %i < %cts; %i++)
		{
			%slot = getWord(%order, %i);

			if(%slot $= "ANY")
				%str = %str TAB "ANY";
			else
				%str = %str TAB %cl.LOItem[%i];
		}
	}

	return trim(%str);
}

// * Sets this client's loadout from a string
function GameConnection::LOSetLoadoutString(%cl, %str)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set))
		return -1;

	%cl.LOClass = getField(%str, 0);

	if(%set.get(type) $= "loadout")
	{
		%cts = getFieldCount(%str);
		for(%i = 1; %i < %cts; %i++)
		{
			%item = getField(%str, %i);

			%cl.LOItem[%i - 1] = %item;
		}
	}

	return 1;
}

// * Export this client's currently saved loadout
// ? unused
function GameConnection::LOExportLoadout(%cl, %path)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set))
		return -1;

	if(%set.get(type) $= "loadout")
	{
		%str = %cl.LOGetLoadoutString();

		%file = new FileObject();

		if(!%file.openForWrite(%path))
		{
			error("GameConnection::LOExportLoadout() - couldn't write to file " @ %path);
			return -3;
		}

		%file.writeLine(%str);

		%file.close();
		%file.delete();
	}
	else return -2;

	return 1;
}

// * Import a saved loadout for this client
// ? unused
function GameConnection::LOImportLoadout(%cl, %path)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set))
		return -1;

	if(%set.get(type) $= "loadout")
	{
		%file = new FileObject();

		if(!%file.openForRead(%path))
		{
			error("GameConnection::LOImportLoadout() - couldn't read file " @ %path);
			return -3;
		}

		%str = %file.readLine();

		%cl.LOSetLoadoutString(%str);

		%file.close();
		%file.delete();
	}
	else return -2; // can't save or load classes since they're already presets

	return 1;
}

// * Exports all of this client's saved loadouts
function GameConnection::LOExportAllLoadouts(%cl)
{
	if(!%cl.hasSpawnedOnce)
		return -2;

	loTalk(%cl @ " exporting loadouts");
	if(!isObject(%group = %cl.LOSaveGroup))
		return -1;

	%group.mainObject.set(placeholder, 1);

	if(isObject(%set = %cl.LOGetActiveSet()))
	{
		%cl.LOValidateLoadout();

		%obj = %group.mainObject;
		%name = $LOSetSafeName[%set.loadoutIdx];
		%save = %obj.get(%name, "", true);

		%save.set(last, %cl.LOGetLoadoutString());
	}

	writeInfoFile(%group, %group.sourcePath);

	return 1;
}

// * Imports all of this client's saved loadouts
function GameConnection::LOImportAllLoadouts(%cl)
{
	loTalk(%cl @ " importing loadouts");
	if(isObject(%cl.LOSaveGroup))
	{
		%cl.LOSaveGroup.mainObject.set(placeholder, 1);
		%cl.LOSaveGroup.delete();
	}

	%group = newInfoGroup(newInfoObject(), $LOConfigPlayers @ "/" @ %cl.getBLID() @ "_loadouts.txt");
	%cl.LOSaveGroup = %group;
}

// * Returns the input targets for this client
function GameConnection::LOGetInputTarget(%cl)
{
	return "Player " @ %cl.Player TAB "Client " @ %cl TAB "Minigame " @ %cl.Minigame;
}

// * Randomizes this player's loadout
function GameConnection::LORandomizeLoadout(%cl, %seed)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set))
		return -1;

	if(%seed !$= "" && %seed != 0)
		setRandomSeed(%seed);

	%classId = %set.listGet(classIds, getRandom(0, %set.listNum(classIds) - 1));

	%totalWeight = 0;
	%tierWeight[0] = 0;
	%numTiers = 1;

	%cts = %set.listNum(rarities);
	for(%i = 0; %i < %cts; %i++)
	{
		%rarity = %set.listGet(rarities, %i);
		%tierWeight[%i] = mAbs(firstWord(%rarity));
		%tierName[%i] = restWords(%rarity);
		%tierIdx[%tierName[%i]] = %i;
		%totalWeight += %tierWeight[%i];
	}

	if(%cts > 0)
		%numTiers = %cts;

	%classes = %set.get(classes);
	%class = %classes.get(%classId);

	%cl.LOClass = %classId;

	%slots = %class.get(slots);
	%order = %slots.get(order);

	%cts = getWordCount(%order);
	for(%i = 0; %i < %cts; %i++)
	{
		%slot = getWord(%order, %i);

		loTalk("Slot " @ %slot @ ":");
		loTalk("  Tiers " @ %numTiers);

		if(%numTiers <= 1)
			%cl.LOItem[%i] = %slots.listGet(%slot, getRandom(0, %slots.listNum(%slot) - 1));
		else
		{
			%rand = getRandom() * %totalWeight;
			loTalk("  Rand " @ %rand @ " / total " @ %totalWeight);

			%pick = -1;
			%weight = 0;
			for(%t = 0; %t < %numTiers; %t++)
			{
				%weight += %tierWeight[%t];
				loTalk("  Weight " @ %weight @ " (" @ %tierWeight[%t] @ ")");

				if(%rand <= %weight)
				{
					%pick = %t;
					break;	
				}
			}

			if(%pick == -1)
				%pick = 0;

			%tier = %tierName[%pick];

			loTalk("  Picked " @ %pick @ " (" @ %tier @ ")");

			%idx = getRandom(0, %slots.tierItems[%slot, %tier] - 1);
			%item = %slots.tierItem[%slot, %tier, %idx];

			loTalk("  Item " @ %item @ " idx " @ %idx);

			%cl.LOItem[%i] = %item;
		}
	}

	return 1;
}

// * Grants this player their active loadout
function GameConnection::LOApplyLoadout(%cl, %resupply, %force, %silent)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set))
		return -1;

	if(!isObject(%pl = %cl.Player))
		return -2;

	%cl.LOValidateLoadout();

	if(!%resupply && !%force && %pl.LOLastLoadout $= %cl.LOGetLoadoutString())
		return -3;

	if(%resupply && %pl.LOLastLoadout $= "")
		%resupply = false;

	if(%resupply && !%force && (%new = %cl.LOGetLoadoutString()) !$= %pl.LOLastLoadout)
		%cl.LOSetLoadoutString(%pl.LOLastLoadout);

	%class = %set.get(classes).get(%cl.LOClass);

	if(%class.get(armor) !$= "" && isObject(%db = SEFindDatablock("PlayerData", %class.get(armor))))
		%pl.ChangeDataBlock(%db);

	if(%class.get(scale) !$= "")
		%pl.setScale(%class.get(scale));

	%type = %set.get(type);
	if(%type $= "loadout")
	{
		%slots = %class.get(slots);
		%order = %slots.get(order);

		%cts = getWordCount(%order);
		for(%i = 0; %i < %cts; %i++)
		{
			%slot = getWord(%order, %i);

			if(%slot $= "ANY")
				continue;

			%pl.LOSetItem(%cl.LOItem[%i], %i);
		}
	}
	else if(%type $= "class")
	{
		%cts = %class.listNum(slots);
		for(%i = 0; %i < %cts; %i++)
		{
			%item = %class.listGet(slots, %i);

			if(%slot $= "ANY")
				continue;

			%pl.LOSetItem(%item, %i);
		}
	}

	if(!%silent && %set.get(randomizer) $= "off" && %set.listNum(classIds) > 1)
		%cl.longCenterPrint($loBigFont @ $loActiveColor @ %class.get(name) @ "<br>" @ $loSmallFont @ $loNeutralColor @ (!%resupply ? "applied" : "resupplied") @ "<br>" @ $loInactiveColor @ "/loadouts to change class", 3);

	if(!%resupply)
	{
		if(%pl.LOLastLoadout !$= "" && isObject(%old = %set.get(classes).get(getField(%pl.LOLastLoadout, 0))))
			SETriggerEvent(%cl.LOGetInputTarget(), "onLOEvent", %old.listDump(onLOSwitchOff));

		SETriggerEvent(%cl.LOGetInputTarget(), "onLOEvent", %class.listDump(onLOSwitchTo));
	}
	else
		SETriggerEvent(%cl.LOGetInputTarget(), "onLOEvent", %class.listDump(onLOResupply));

	%pl.LOLastLoadout = %cl.LOGetLoadoutString();

	if(%new !$= "")
		%cl.LOSetLoadoutString(%new);
	
	return 1;
}

function Player::LOSetItem(%pl, %name, %idx)
{
	if(!isObject(%cl = %pl.Client))
		return;

	if(!isObject(%item = SEFindDatablock("ItemData", %name, true)))
	{
		warn("GameConnection::LOSetItem() - item " @ %name @ " does not exist");

		if(%pl.currTool == %idx)
			ServerCmdUnUseTool(%cl);
	}
	else
	{
		%pl.tool[%idx] = %item;
		messageClient(%cl, 'MsgItemPickup', "", %idx, %item);

		if(%pl.currTool == %idx)
			ServerCmdUseTool(%cl, %idx);
	}
}

// * Make sure this client's current loadout is valid
function GameConnection::LOValidateLoadout(%cl)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set))
		return -1;

	if(isObject(%group = %cl.LOSaveGroup) && %cl.LOGetLoadoutString() $= "")
	{
		%obj = %group.mainObject;
		%name = $LOSetSafeName[%set.loadoutIdx];
		%save = %obj.get(%name, "", true);

		if((%load = %save.get(last)) !$= "")
			%cl.LOSetLoadoutString(%load);
		else
			%cl.LOSetLoadoutString(strreplace(%set.listDump("default"), "\n", "\t"));
	}

	if(%set.listFind(classIds, %cl.LOClass) < 0)
		%cl.LOClass = %set.listGet(classIds, 0);

	if(%set.get(type) $= "loadout")
	{
		%class = %set.get(classes).get(%cl.LOClass);
		%slots = %class.get(slots);
		%order = %slots.get(order);

		%cts = getWordCount(%order);
		for(%i = 0; %i < %cts; %i++)
		{
			%slot = getWord(%order, %i);
			loTalk("Validating " @ %slot);

			if(%slot $= "ANY")
			{
				%cl.LOItem[%i] = "";
				continue;
			}

			if(%slots.listNum(%slot) <= 0) // slot does not have any weapons defined
			{
				warn("GameConnection::LOValidateLoadout() - couldn't validate slot " @ %i @ " for client " @ %cl @ ": class " @ %cl.LOClass @ " does not have any weapons listed for " @ %slot @ " slots");
				%cl.LOItem[%i] = "";
				continue;
			}

			%replace = false;

			loTalk("Item " @ %cl.LOItem[%i] @ ": " @ SEFindDatablock("ItemData", %cl.LOItem[%i], true) @ ", " @ %slots.listFind(%slot, %cl.LOItem[%i]));

			if(%slots.listFind(%slot, %cl.LOItem[%i]) >= 0 && isObject(SEFindDatablock("ItemData", %cl.LOItem[%i], true))) // item exists and is allowed
			{
				if(%set.get(allowDuplicateWeapons)) // duplicates allowed; don't check for dupes
					continue;

				if(%itemInUse[%slot, %cl.LOItem[%i]]) // item in use, replace it
					%replace = true;
				else
					%itemInUse[%slot, %cl.LOItem[%i]] = true;
			}
			else %replace = true; // item doesn't exist or is not allowed, replace it

			loTalk(%replace ? ("Replacing slot " @ %i) : ("Slot " @ %i @ " OK"));

			if(%replace)
			{
				%done = false;
				%ct2 = %slots.listNum(%slot);
				for(%d = 0; %d < %ct2; %d++)
				{
					%item = %slots.listGet(%slot, %d);
					if(!%itemInUse[%slot, %item])
					{
						%cl.LOItem[%i] = %item;
						%itemInUse[%slot, %item] = true;
						%done = true;
						break;
					}
					else loTalk("Item " @ %item @ " in use in slot " @ %slot);
				}

				if(!%done)
				{
					warn("GameConnection::LOValidateLoadout() - couldn't validate slot " @ %i @ " for client " @ %cl @ ": class " @ %cl.LOClass @ " does not have enough weapons to fit all " @ %slot @ " slots");
					%cl.LOItem[%i] = "";
					continue;
				}
			}
		}
	}

	return 1;
}