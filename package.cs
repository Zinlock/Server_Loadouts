package LoadoutPkg
{
	function GameConnection::onClientEnterGame(%cl)
	{
		%cl.LOImportAllLoadouts();

		Parent::onClientEnterGame(%cl);
	}

	function GameConnection::onClientLeaveGame(%cl)
	{
		%cl.LOExportAllLoadouts();

		if(isObject(%cl.loadoutMenu))
			%cl.loadoutMenu.delete();

		if(isObject(%cl.loadoutPicker))
			%cl.loadoutPicker.delete();

		Parent::onClientLeaveGame(%cl);
	}

	function serverCmdMessageSent(%cl, %msg)
	{
		if(%cl.loRenaming)
		{
			%cl.loRenaming = false;

			%save = %cl.loadoutPicker.save;
			%select = %cl.loadoutPicker.selectedPreset;

			%preset = %save.listGet(presets, %select);
			%presetValue = getFields(%preset, 1, getFieldCount(%preset));

			%newName = trim(StripMLControlChars(strreplace(strreplace(%msg, "\t", ""), "\n", "")));

			%save.listSet(presets, %select, %newName TAB %presetValue);

			%cl.longCenterPrint($loBigFont @ $loActiveColor @ %newName @ $loSmallFont @ $loNeutralColor @ "<br>renamed preset!");
			return;
		}

		Parent::serverCmdMessageSent(%cl, %msg);
	}

	function GameConnection::spawnPlayer(%cl)
	{
		%p = Parent::spawnPlayer(%cl);

		if(isObject(%pl = %cl.Player) && isObject(%mg = %cl.minigame) && isObject(%set = %cl.LOGetActiveSet()))
		{
			%pl.currTool = -1;

			%random = %set.get(randomizer);
			if(%random $= "on")
				%cl.LORandomizeLoadout();
			else if(%random $= "static")
				%cl.LORandomizeLoadout(%mg.loRandomSeed);
			else if(%random $= "staticTeams")
			{
				if(isObject(%cl.slyrTeam))
					%cl.LORandomizeLoadout(%cl.slyrTeam.loRandomSeed);
				else
					%cl.LORandomizeLoadout(%mg.loRandomSeed);
			}

			if(%mg.LOApplyOnSpawn)
				%cl.LOApplyLoadout(false, false, %random !$= "on");
			else
			{
				%old = %cl.LOGetLoadoutString();
				if(%set.get(type) $= "loadout")
					%cl.LOSetLoadoutString(strreplace(%set.listDump("default"), "\n", "\t"));
				else
					%cl.LOSetLoadoutString(%set.get("default"));
				%cl.LOApplyLoadout(false, false, true);
				%cl.LOSetLoadoutString(%old);
			}
		}

		return %p;
	}

	function GameConnection::onDeath(%cl, %src, %srcCl, %type, %pos)
	{
		if(isObject(%set = %cl.LOGetActiveSet()))
		{
			%class = %set.get(classes).get(%cl.LOClass);

			if(isObject(%class))
			{
				if(isObject(%srcCl) && %srcCl != %cl)
				{
					if(isObject(%src))
					{
						%className = %src.getClassName();
						if(%className $= "Player" || %className $= "AIPlayer")
							%kPl = %src;
						else if(%className $= "WheeledVehicle" || %className $= "FlyingVehicle" || %className $= "HoverVehicle")
							%kPl = %src.getMountedObject(0);
						else if(isObject(%sObj = %src.sourceObject) && ((%sClass = %sObj.getClassName()) $= "Player" || %sClass $= "AIPlayer"))
							%kPl = %sObj;
						else
							%kPl = %srcCl.player;
					}
					else %kPl = %srcCl.player;

					SETriggerEvent(%cl.LOGetInputTarget() TAB "KillerPlayer " @ %kPl TAB "KillerClient " @ %srcCl, "onLOEvent", %class.listDump(onLOKilled));
				}
				else
					SETriggerEvent(%cl.LOGetInputTarget(), "onLOEvent", %class.listDump(onLODeath));
			}
		}

		return Parent::onDeath(%cl, %src, %srcCl, %type, %pos);
	}

	function MiniGameSO::addMember(%mg, %cl)
	{
		%cl.LOOverrideSet("");

		return Parent::addMember(%mg, %cl);
	}

	function MiniGameSO::removeMember(%mg, %cl)
	{
		%cl.LOOverrideSet("");

		return Parent::removeMember(%mg, %cl);
	}

	function MiniGameSO::reset(%mg, %cl)
	{
		for(%i = 0; %i < %mg.numMembers; %i++)
		{
			%cc = %mg.member[%i];

			%cc.LOOverrideSet("");
		}

		getRandom();
		%mg.loRandomSeed = getRandomSeed();

		if(isObject(%teams = %mg.teams))
		{
			%cts = %teams.getCount();
			for(%i = 0; %i < %cts; %i++)
			{
				%team = %teams.getObject(%i);
				getRandom();
				%team.loRandomSeed = getRandomSeed();
			}
		}

		return Parent::reset(%mg, %cl);
	}

	function Player::Pickup(%pl, %item, %amt)
	{
		%skip = false;
		%fixempty = false;

		%db = %pl.getDataBlock();

		if(isObject(%cl = %pl.Client) && isObject(%set = %cl.LOGetActiveSet()) && isObject(%class = %set.get(classes).get(firstWord(getField(%pl.LOLastLoadout, 0)))))
		{
			loTalk(%pl @ " pickup " @ %item);
			if(%item.getClassName() $= "Item" && %item.canPickup)
			{
				%name = %item.getDataBlock().uiName;
				loTalk("item " @ %name SPC (LOIsInSet(%name, %set) ? "in set" : "not in set"));
				if(LOIsInSet(%name, %set))
				{
					%pickup = false;
					%full = true;
					%fixempty = true;
					for(%i = 0; %i < %db.maxTools; %i++)
					{
						loTalk("slot " @ %i @ ":");
						%canPickup = LOIsInSlot(%name, %i, %class);

						if(%canPickup)
							%pickup = true;

						if(!isObject(%pl.tool[%i]))
						{
							if(%canPickup)
							{
								loTalk("  open; can pickup");
								%full = false;
								break;
							}
							else
							{
								loTalk("  open; can not pickup");
								%pl.tool[%i] = -2;
							}
						}
					}

					if(!%pickup)
					{
						%cl.centerPrint("\c5Your class can not use this item.", 1);
						%skip = true;
					}
					else if(%full)
					{
						%slots = LOGetMatchingSlotTitle(%name, %class);
						%cl.centerPrint("\c5Your \c3" @ restWords(%slots) @ "\c5 slot" @ (firstWord(%slots) == 1 ? " is full" : "s are full") @ ".", 1);
						%skip = true;
					}
				}
			}
		}

		if(!%skip)
			%p = Parent::Pickup(%pl, %item, %amt);

		if(%fixempty)
		{
			for(%i = 0; %i < %db.maxTools; %i++)
			{
				if(%pl.tool[%i] == -2)
					%pl.tool[%i] = 0;
			}
		}

		return %p;
	}
};
activatePackage(LoadoutPkg);