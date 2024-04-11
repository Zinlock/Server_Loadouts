function LOGetStringIcon(%str)
{
	%db = (imIcon @ getSubStr(%str, 0, 1));

	if(!isObject(%db))
		%db = imIconQmark;

	return %db;
}

function LOGetClassIcon(%class)
{
	if(!isObject(%class))
		return imIconQmark;

	%db = SEFindDatablock("ItemData", %class.get(item));

	if(!isObject(%db))
		%db = LOGetStringIcon(%class.get(name));

	return %db;
}

// * Creates the inventory menu for a set
function LOCreateSetMenu(%set)
{
	%type = %set.get(type);
	
	%obj = new ScriptObject(LoadoutIM)
	{
		superClass = InvMenuSO;
		class = LoadoutMenuSO;

		set = %set;
		type = %type;
	};

	if(%type $= "loadout")
	{
		%obj.delete();
		return -1;
	}
	else if(%type $= "class")
	{
		%cts = %set.listNum(classIds);

		%obj.numEntry = %cts;

		%classes = %set.get(classes);

		for(%i = 0; %i < %cts; %i++)
		{
			%classId = %set.listGet(classIds, %i);

			if(!isObject(%class = %classes.get(%classId)))
			{
				warn("LOCreateSetMenu() - set " @ $LOSetName[%set.loadoutIdx] @ " does not contain data for class " @ %classId);
				continue;
			}

			loTalk("set " @ %set.loadoutIdx @ " class " @ %i @ " " @ %classId @ " " @ %class);
			%obj.entry[%i] = LOGetClassIcon(%class);
			%obj.classObj[%i] = %class;
			%obj.classId[%i] = %classId;
		}
	}
	else
	{
		if(!isObject(%class = %classes.get(%classId)))
		{
			error("LOCreateSetMenu() - invalid type \"" @ %type @ "\" for set " @ $LOSetName[%set.loadoutIdx]);
			%obj.delete();
			return -1;
		}
	}

	return %obj;
}

function GameConnection::LOUpdateLoadoutMenu(%cl)
{
	%set = %cl.LOGetActiveSet();

	if(!isObject(%set) || %set.get(type) !$= "loadout")
		return -1;

	%cl.LOValidateLoadout();

	// * main editor menu

	if(!isObject(%obj = %cl.loadoutMenu))
	{
		%obj = new ScriptObject(LoadoutEdIM)
		{
			superClass = InvMenuSO;
			class = LoadoutEditorSO;
		};

		%cl.loadoutMenu = %obj;
	}

	%obj.set = %set;

	%classId = %cl.LOClass;
	if(!isObject(%class = %set.get(classes).get(%classId)))
	{
		error("GameConnection::LOUpdateLoadoutMenu() - class " @ %classId @ " does not exist in set " @ $LOSetName[%set.loadoutIdx]);
		return %obj;
	}

	%slots = %class.get(slots);
	%order = %slots.get(order);

	%cts = getWordCount(%order);

	%obj.entry[0] = LOGetClassIcon(%class);
	%obj.classObj = %class;
	%obj.classId = %classId;

	%numSlots = 0;
	for(%i = 0; %i < %cts; %i++)
	{
		if(getWord(%order, %i) $= "ANY")
			continue;

		%item = SEFindDatablock("ItemData", %cl.LOItem[%i], true);

		if(!isObject(%item))
			%obj.entry[%numSlots + 1] = imIconQmark;
		else
			%obj.entry[%numSlots + 1] = %item;

		%obj.slot[%numSlots + 1] = %numSlots;
		%numSlots++;
	}

	%obj.entry[%numSlots + 1] = imIconPlus;

	%obj.numEntry = %numSlots + 2;

	if(!isObject(%obj = %cl.loadoutPicker))
	{
		%obj = new ScriptObject(LoadoutPickIM)
		{
			superClass = InvMenuSO;
			class = LoadoutPickerSO;
		};

		%cl.loadoutPicker = %obj;
	}

	%obj.set = %set;
}

// * === Menu functions ===

// * Class picker menu

function LoadoutMenuSO::onSelect(%obj, %cl, %idx)
{
	if(%obj.type $= "class")
	{
		%name = %obj.classObj[%idx].get(name);

		if(%obj.classId[%idx] !$= %cl.LOGetLoadoutString())
			%cl.longCenterPrint($loBigFont @ $loInactiveColor @ %name @ "<br>" @ $loSmallFont @ $loNeutralColor @ "select class >>");
		else
			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %name @ "<br>" @ $loSmallFont @ $loNeutralColor @ "selected");
	}
}

function LoadoutMenuSO::onClick(%obj, %cl, %idx)
{
	if(%obj.type $= "class")
	{
		%name = %obj.classObj[%idx].get(name);

		%cl.LOSetLoadoutString(%obj.classId[%idx]);

		%obj.close(%cl);

		%cl.longCenterPrint($loBigFont @ $loActiveColor @ %name @ "<br>" @ $loSmallFont @ $loNeutralColor @ "selected", 2);
	}
}

function LoadoutMenuSO::onDrop(%obj, %cl, %idx)
{
	if(%obj.type $= "class")
		LoadoutMenuSO::onClick(%obj, %cl, %idx);
}

function LoadoutMenuSO::onClose(%obj, %cl)
{
	%cl.longCenterPrint("", 1);
	%cl.LOExportAllLoadouts();
}

// * Loadout editor menu

function LoadoutEditorSO::onOpen(%obj, %cl)
{
	%cl.LOUpdateLoadoutMenu();
}

function LoadoutEditorSO::onSelect(%obj, %cl, %idx)
{
	%str = $loBigFont @ $loActiveColor @ %obj.classObj.get(name) @ "<br>" @ $loSmallFont @ $loNeutralColor;
	if(%idx == 0)
		%str = %str @ "change class >>";
	else if(%idx == %obj.numEntry - 1)
		%str = %str @ "saved presets >>";
	else
	{
		%slot = %obj.slot[%idx];
		%slotId = getWord(%obj.classObj.get(slots).get(order), %slot);
		%str = %str @ %slotId @ " " @ %slot + 1 @ ": " @ %cl.LOItem[%slot] @ "<br>change weapon >>";
	}

	%cl.longCenterPrint(%str);
}

function LoadoutEditorSO::onClick(%obj, %cl, %idx)
{
	if(%idx == 0)
	{
		%cl.loPicking = "class";
		%cl.loadoutPicker.open(%cl);
	}
	else if(%idx == %obj.numEntry - 1)
	{
		%cl.loPicking = "preset";
		%cl.loadoutPicker.open(%cl);
	}
	else
	{
		%cl.loPicking = "weapon " @ %obj.slot[%idx];
		%cl.loadoutPicker.open(%cl);
	}
}

function LoadoutEditorSO::onDrop(%obj, %cl, %idx)
{
	LoadoutEditorSO::onClick(%obj, %cl, %idx);
}

function LoadoutEditorSO::onClose(%obj, %cl)
{
	%cl.longCenterPrint("", 1);
	%cl.LOExportAllLoadouts();
}

// * Loadout picker menu

function LoadoutPickerSO::onOpen(%obj, %cl)
{
	%pick = firstWord(%cl.loPicking);
	%obj.picking = %pick;

	if(%pick $= "class")
	{
		%set = %obj.set;
		loTalk("picking class: set " @ %set.loadoutIdx);

		%classes = %set.get(classes);

		%cts = %set.listNum(classIds);
		loTalk("  " @ %cts @ " classes");

		for(%i = 0; %i < %cts; %i++)
		{
			%classId = %set.listGet(classIds, %i);
			%class = %classes.get(%classId);
			%obj.entry[%i] = LOGetClassIcon(%class);
			%obj.classObj[%i] = %class;
			%obj.classId[%i] = %classId;
			loTalk("  class " @ %i @ ": " @ %classId @ ", " @ %class @ ", " @ %obj.entry[%i]);
		}

		%obj.numEntry = %cts;
	}
	else if(%pick $= "preset")
	{
		%set = %obj.set;

		if(!isObject(%group = %cl.LOSaveGroup))
		{
			error("LoadoutPickerSO::onOpen() - client " @ %cl @ " has no save group");
			%obj.numEntry = 0;
			return;
		}

		%name = $LOSetSafeName[%set.loadoutIdx];
		%save = %group.mainObject.get(%name, "", true);

		%obj.save = %save;

		%cts = %save.listNum(presets) * 1;
		for(%i = 0; %i < %cts; %i++)
		{
			%preset = %save.listGet(presets, %i);
			%presetName = getField(%preset, 0);
			%obj.entry[%i] = LOGetStringIcon(%presetName);
		}

		%obj.entry[%cts] = imIconPlus;
		%obj.entry[%cts + 1] = imIconEquals;
		%obj.numEntry = %cts + 2;
	}
	else if(%pick $= "weapon")
	{
		%idx = getWord(%cl.loPicking, 1);

		%class = %cl.loadoutMenu.classObj;
		%slots = %class.get(slots);
		%slot = getWord(%slots.get(order), %idx);

		%cts = %slots.listNum(%slot);
		for(%i = 0; %i < %cts; %i++)
		{
			%item = %slots.listGet(%slot, %i);

			%obj.entry[%i] = SEFindDatablock("ItemData", %item);
			%obj.item[%i] = %item;
		}

		%obj.numEntry = %cts;
		%obj.pickingSlot = %idx;
	}
}

function LoadoutPickerSO::onSelect(%obj, %cl, %idx)
{
	%cl.loRenaming = false;

	%pick = firstWord(%cl.loPicking);

	if(%pick $= "class")
	{
		%cl.longCenterPrint($loBigFont @ $loActiveColor @ %obj.classObj[%idx].get(name) @ $loSmallFont @ $loNeutralColor @ "<br>select class >>");
	}
	else if(%pick $= "preset")
	{
		if(%idx == %obj.numEntry - 2)
			%cl.longCenterPrint($loBigFont @ $loInactiveColor @ "New" @ $loSmallFont @ $loNeutralColor @ "<br>create preset >>");
		else if(%idx == %obj.numEntry - 1)
		{
			if(%obj.reordering)
				%cl.longCenterPrint($loBigFont @ $loInactiveColor @ "Reorder" @ $loSmallFont @ $loNeutralColor @ "<br>stop reordering presets >>");
			else
				%cl.longCenterPrint($loBigFont @ $loInactiveColor @ "Reorder" @ $loSmallFont @ $loNeutralColor @ "<br>reorder presets >>");
		}
		else
		{
			%save = %obj.save;

			%preset = %save.listGet(presets, %idx);
			%presetName = getField(%preset, 0);

			if(%obj.reordering)
				%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>move preset >>");
			else
			{
				%str = $loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>select preset >><br>";

				%presetValue = getFields(%preset, 1, getFieldCount(%preset));

				%set = %obj.set;
				%class = %set.get(classes).get(getField(%presetValue, 0));

				if(!isObject(%class))
				{
					%str = %str @ "class missing!";
				}
				else
				{
					%str = %str @ "class: <spush>" @ $loActiveColor @ %class.get(name) @ "<spop><br>";

					%cts = getFieldCount(%presetValue);
					for(%i = 1; %i < %cts; %i++)
					{
						%item = getField(%presetValue, %i);

						%str = %str @ %item @ "<br>";
					}
				}

				%cl.longCenterPrint(%str);
			}
		}
	}
	else if(%pick $= "preset_confirm")
	{
		%save = %obj.save;
		%select = %obj.selectedPreset;

		%preset = %save.listGet(presets, %select);
		%presetName = getField(%preset, 0);

		if(%idx == 0) // load
			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>load preset >>");
		else if(%idx == 1) // save
			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>overwrite preset >>");
		else if(%idx == 2) // rename
			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>rename preset >>");
		else if(%idx == 3) // delete
			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>delete preset >>");
	}
	else if(%pick $= "weapon")
	{
		%slot = getWord(%cl.loPicking, 1);
		%slotId = getWord(%cl.loadoutMenu.classObj.get(slots).get(order), %slot);
		%cl.longCenterPrint($loBigFont @ $loActiveColor @ %slotId @ " " @ %slot + 1 @ "<br>" @ $loSmallFont @ $loNeutralColor @ %obj.item[%idx] @ "<br>select weapon >>");
	}
}

function LoadoutPickerSO::onClick(%obj, %cl, %idx)
{
	%pick = firstWord(%cl.loPicking);

	if(%pick $= "class")
	{
		%cl.LOClass = %obj.classId[%idx];
		%obj.close(%cl);
	}
	else if(%pick $= "preset")
	{
		%save = %obj.save;

		if(%idx == %obj.numEntry - 2)
		{
			%save.listAdd(presets, "New Preset" TAB %cl.LOGetLoadoutString());

			%cl.loPicking = "preset_confirm";

			%obj.entry[0] = imIconL;
			%obj.entry[1] = imIconS;
			%obj.entry[2] = imIconR;
			%obj.entry[3] = imIconMinus;
			%obj.numEntry = 4;

			%obj.selectedPreset = %idx;

			%cl.inventoryOffset = 0;
			%cl.IMSelectSlot(0);
			%cl.IMResetInventory();
		}
		else if(%idx == %obj.numEntry - 1)
		{
			if(%obj.reordering)
			{
				%obj.reordering = false;
				%cl.longCenterPrint($loBigFont @ $loInactiveColor @ "Reorder" @ $loSmallFont @ $loNeutralColor @ "<br>reorder presets >>");
			}
			else
			{
				%obj.reordering = true;
				%obj.reorderIdx = -1;
				%cl.longCenterPrint($loBigFont @ $loInactiveColor @ "Reorder" @ $loSmallFont @ $loNeutralColor @ "<br>select a preset to move...");
			}
		}
		else
		{
			%preset = %save.listGet(presets, %idx);
			%presetName = getField(%preset, 0);

			if(%obj.reordering)
			{
				if(%obj.reorderIdx >= 0)
				{
					if(%obj.reorderIdx == %idx)
					{
						%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>move preset >>");
						%obj.reorderIdx = -1;
					}
					else
					{
						%old = %save.listGet(presets, %obj.reorderIdx);
						%save.listSet(presets, %idx, %old);
						%save.listSet(presets, %obj.reorderIdx, %preset);
						%presetName = getField(%old, 0);

						%oldEntry = %obj.entry[%obj.reorderIdx];
						%obj.entry[%obj.reorderIdx] = %obj.entry[%idx];
						%obj.entry[%idx] = %oldEntry;

						%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>moved here");

						%obj.reordering = false;
						%obj.reorderIdx = -1;

						%cl.IMResetInventory();
					}
				}
				else
				{
					%cl.longCenterPrint($loBigFont @ $loInactiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>select a preset to swap with...");
					%obj.reorderIdx = %idx;
				}
			}
			else
			{
				%cl.loPicking = "preset_confirm";

				%obj.entry[0] = imIconL;
				%obj.entry[1] = imIconS;
				%obj.entry[2] = imIconR;
				%obj.entry[3] = imIconMinus;
				%obj.numEntry = 4;

				%obj.selectedPreset = %idx;

				%cl.inventoryOffset = 0;
				%cl.IMSelectSlot(0);
				%cl.IMResetInventory();
			}
		}		
	}
	else if(%pick $= "preset_confirm")
	{
		%save = %obj.save;
		%select = %obj.selectedPreset;

		%preset = %save.listGet(presets, %select);
		%presetName = getField(%preset, 0);

		if(%idx == 0) // load
		{
			%presetValue = getFields(%preset, 1, getFieldCount(%preset));
			%cl.LOSetLoadoutString(%presetValue);
			%obj.close(%cl);
		}
		else if(%idx == 1) // save
		{
			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>overwrite preset >>");
			%save.listSet(presets, %select, %presetName TAB %cl.LOGetLoadoutString());
			%obj.close(%cl);
		}
		else if(%idx == 2) // rename
		{
			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>send a message to rename...");
			%cl.loRenaming = true;
		}
		else if(%idx == 3) // delete
		{
			if(getSimTime() - %cl.loDeleteTime > 500)
			{
				%cl.longCenterPrint($loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>double click to delete!");
				%cl.loDeleteTime = getSimTime();

				%cl.loWarnS = %cl.schedule(500, centerPrint, $loBigFont @ $loActiveColor @ %presetName @ $loSmallFont @ $loNeutralColor @ "<br>delete preset >>");
			}
			else
			{
				cancel(%cl.loWarnS);
				%save.listRemove(presets, %select);
				%obj.close(%cl);
			}
		}
	}
	else if(%pick $= "weapon")
	{
		%slotIdx = getWord(%cl.loPicking, 1);

		%class = %cl.loadoutMenu.classObj;
		%slots = %class.get(slots);
		%order = %slots.get(order);

		%slot = getWord(%order, %slotIdx);

		%cts = getWordCount(%order);
		for(%i = 0; %i < %cts; %i++)
		{
			%slot2 = getWord(%order, %i);

			if(%slot !$= %slot2)
				continue;

			%itemInUse[%cl.LOItem[%i]] = true;
			%itemSlot[%cl.LOItem[%i]] = %i;
		}

		%item = %obj.item[%idx];
		if(!%obj.set.get(allowDuplicateWeapons) && %itemInUse[%item])
		{
			%oldSlot = %itemSlot[%item];
			%cl.LOItem[%slotIdx] = %item;
			%cl.LOItem[%oldSlot] = "";
		}
		else
			%cl.LOItem[%slotIdx] = %item;

		%obj.close(%cl);
	}
}

function LoadoutPickerSO::onDrop(%obj, %cl, %idx)
{
	LoadoutPickerSO::onClick(%obj, %cl, %idx);
}

function LoadoutPickerSO::onClose(%obj, %cl)
{
	cancel(%cl.loWarnS);
	%obj.reordering = false;
	%obj.reorderIdx = -1;
	%cl.loRenaming = false;
	%cl.loadoutMenu.open(%cl);
}