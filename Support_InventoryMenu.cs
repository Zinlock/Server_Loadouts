// * Support_InventoryMenu by Oxy (260031)
// * Adds a ScriptObject class to easily make custom menus in the player inventory

// * === Functions ===

// * Opens this inventory menu for a player
function InvMenuSO::Open(%obj, %cl)
{
	if(!%cl.hasSpawnedOnce)
		return;

	if(isObject(%old = %cl.inventoryMenu))
	{
		if(%obj.getId() == %old.getId())
			return;

		%old.close(%cl);
		%obj.schedule(0, open, %cl);
		return;
	}

	%cl.inventoryMenu = %obj;
	%cl.inventoryOffset = 0;
	%cl.inventoryWaiting = 0;
	%cl.inventorySelect = 0;
	%cl.inventoryReady = false;
	%obj.onOpen(%cl);
	%obj.setSelectedId(%cl, 0);

	cancel(%cl.inventorySchedule);
	%cl.inventorySchedule = %cl.schedule(32, IMResetInventory);
}

function InvMenuSO::Close(%obj, %cl)
{
	if(!%cl.hasSpawnedOnce)
		return;

	%cl.inventoryMenu = -1;
	%cl.inventoryOffset = 0;
	%cl.inventoryWaiting = -1;
	%cl.inventorySelect = 0;
	%cl.inventoryReady = false;
	%obj.onClose(%cl);

	cancel(%cl.inventorySchedule);
	%cl.inventorySchedule = %cl.schedule(32, IMResetInventory);
}

// * === Callbacks ===

// * Called when a client opens this inventory menu
function InvMenuSO::onOpen(%obj, %cl)
{
	// %cl.ChatMessage("opened " @ %obj);
}

// * Called when a client closes this inventory menu
function InvMenuSO::onClose(%obj, %cl)
{
	// %cl.centerPrint("closed " @ %obj, 2);
}

// * Called when a client scrolls to this inventory menu entry
function InvMenuSO::onSelect(%obj, %cl, %idx)
{
	// %cl.centerPrint("selected " @ %obj @ " idx " @ %idx);
}

// * Called when a client clicks with an empty hand or presses E
function InvMenuSO::onClick(%obj, %cl, %idx)
{
	// %cl.centerPrint("clicked " @ %obj @ " idx " @ %idx);
}

// * Called when a client tries dropping an item
function InvMenuSO::onDrop(%obj, %cl, %idx)
{
	// %cl.centerPrint("dropped " @ %obj @ " idx " @ %idx);
}

// * === Package ===

$IMInventorySize = 12;

package InventoryMenuPkg
{
	function serverCmdUseTool(%cl, %slot)
	{
		if(isObject(%obj = %cl.inventoryMenu))
		{
			if(%cl.inventoryWaiting >= 0)
			{
				if(%slot != %cl.inventoryWaiting)
					return;
				else
					%cl.inventoryWaiting = -1;
			}

			if(%obj.numEntry > $IMInventorySize)
			{
				%max = %obj.numEntry - $IMInventorySize;

				if(%slot == $IMInventorySize - 1 && %cl.inventoryOffset < %max)
				{
					%cl.inventoryOffset += $IMInventorySize - 2;
					if(%cl.inventoryOffset > %max)
					{
						%off = %cl.inventoryOffset - %max;
						%cl.inventoryOffset = %max;
					}

					%newSlot = 1 + %off;
					%cl.IMSelectSlot(%newSlot);

					%cl.IMResetInventory();
				}
				else if(%slot == 0 && %cl.inventoryOffset > 0)
				{
					%off = 0;

					%cl.inventoryOffset -= $IMInventorySize - 2;
					if(%cl.inventoryOffset < 0)
					{
						%off = mAbs(%cl.inventoryOffset);
						%cl.inventoryOffset = 0;
					}

					%newSlot = $IMInventorySize - 2 - %off;
					%cl.IMSelectSlot(%newSlot);

					%cl.IMResetInventory();
				}
			}

			%idx = %slot + %cl.inventoryOffset;

			if(%cl.inventorySelect != %idx)
				%obj.setSelectedId(%cl, %idx);

			return;
		}
	
		Parent::serverCmdUseTool(%cl, %slot);
	}

	function serverCmdUnUseTool(%cl)
	{
		if(isObject(%obj = %cl.inventoryMenu))
		{
			if(getSimTime() - %cl.inventoryReadyTime > 200 + %cl.getPing() * 4)
				%obj.Close(%cl);
			else
				commandToClient(%cl, 'SetActiveTool', %cl.inventorySelect);
		}
		else
			Parent::serverCmdUnUseTool(%cl);
	}

	// not using Player::activateStuff for this cause clients may click without a player
	// player activatestuff would also overlap with the command
	function Armor::onTrigger(%db, %pl, %trig, %val)
	{
		%cl = %pl.getControllingClient();
		if(isObject(%obj = %cl.inventoryMenu) && !isObject(%pl.getMountedImage(0)) && %trig == 0 && %val)
			%obj.onClick(%cl, %cl.inventorySelect);

		Parent::onTrigger(%db, %pl, %trig, %val);
	}

	function Observer::onTrigger(%db, %cam, %trig, %val)
	{
		%cl = %cam.getControllingClient();
		if(isObject(%obj = %cl.inventoryMenu) && %trig == 0 && %val)
			%obj.onClick(%cl, %cl.inventorySelect);

		Parent::onTrigger(%db, %cam, %trig, %val);
	}

	function serverCmdActivateStuff(%cl)
	{
		if(isObject(%obj = %cl.inventoryMenu))
		{
			%obj.onClick(%cl, %cl.inventorySelect);
			return;
		}

		Parent::serverCmdActivateStuff(%cl);
	}

	function serverCmdDropTool(%cl, %slot)
	{
		if(isObject(%obj = %cl.inventoryMenu))
		{
			%obj.onDrop(%cl, %cl.inventorySelect);
			return;
		}

		Parent::serverCmdDropTool(%cl, %slot);
	}
};
activatePackage(InventoryMenuPkg);

// * === Other functions ===

// * Generates placeholder letter item datablocks for use as inventory menu icons
function IMGenerateLetterIcons()
{
	%pat = "Add-Ons/Print_Letters_Default/icons/*.png";

	%file = findFirstFile(%pat);
	while(isFile(%file))
	{
		%name = strReplace(fileBase(%file), "-", "");
		%name = strReplace(%name, "_", "");

		%path = filePath(%file) @ "/" @ fileBase(%file);

		eval("datablock ItemData(" @ getSafeVariableName("imIcon" @ %name) @ ") { shapeFile = \"base/data/shapes/empty.dts\"; iconName = \"" @ %path @ "\"; uiName = \"\"; };");

		%file = findNextFile(%pat);
	}
}

IMGenerateLetterIcons();

// * Selects an inventory menu entry
function InvMenuSO::setSelectedId(%obj, %cl, %idx)
{
	%cl.inventorySelect = %idx;
	%obj.onSelect(%cl, %idx);
}

function InvMenuSO::onRemove(%obj)
{
	%cts = ClientGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		if(isObject(%cl.inventoryMenu) && %cl.inventoryMenu.getId() == %obj.getId())
			%obj.close(%cl);
	}
}

function GameConnection::IMSelectSlot(%cl, %idx)
{
	%cl.inventorySelect = -1;
	%cl.inventoryWaiting = %idx;
	commandToClient(%cl, 'SetActiveTool', %idx);
}

function GameConnection::IMResetInventory(%cl)
{
	if(isObject(%obj = %cl.inventoryMenu))
	{
		for(%i = 0; %i < $IMInventorySize; %i++)
		{
			%idx = %i + %cl.inventoryOffset;
			if(%i == 0 && %cl.inventoryOffset > 0)
				messageClient(%cl, 'MsgItemPickup', "", %i, imIconLessThan.getId());
			else if(%i == $IMInventorySize - 1 && %obj.numEntry - %cl.inventoryOffset > $IMInventorySize)
				messageClient(%cl, 'MsgItemPickup', "", %i, imIconGreaterThan.getId());
			else
			{
				if(isObject(%obj.entry[%idx]))
					messageClient(%cl, 'MsgItemPickup', "", %i, %obj.entry[%idx].getId());
				else
					messageClient(%cl, 'MsgItemPickup', "", %i, 0);
			}
		}

		if(!%cl.inventoryReady || %cl.lastInventorySize != mClamp(%obj.numEntry, 0, $IMInventorySize))
		{
			%cl.inventoryReady = true;
			%cl.inventoryReadyTime = getSimTime();

			commandToClient(%cl, 'SetPaintingDisabled', 1);
			commandToClient(%cl, 'SetBuildingDisabled', 1);

			if(%obj.numEntry >= $IMInventorySize)
			{
				commandToClient(%cl, 'PlayGui_CreateToolHud', $IMInventorySize);
				%cl.lastInventorySize = $IMInventorySize;
			}
			else
			{
				commandToClient(%cl, 'PlayGui_CreateToolHud', %obj.numEntry);
				%cl.lastInventorySize = %obj.numEntry;
			}

			commandToClient(%cl, 'SetScrollMode', 2);
			commandToClient(%cl, 'SetActiveTool', 0);
		}
	}
	else 
	{
		%cl.lastInventorySize = "";

		commandToClient(%cl, 'SetPaintingDisabled', (isObject(%mg = %cl.minigame) ? !%mg.enablePainting : false));
		commandToClient(%cl, 'SetBuildingDisabled', (isObject(%mg = %cl.minigame) ? !%mg.enableBuilding : false));

		if(isObject(%pl = %cl.Player))
		{
			if(isObject(%pl.usingVehicleGuns))
				%pl.setVehicleGunInventory(%pl.usingVehicleGuns);
			else
			{
				commandToClient(%cl, 'PlayGui_CreateToolHud', %pl.getDataBlock().maxTools);

				for(%i = 0; %i < %pl.getDataBlock().maxTools; %i++)
					messageClient(%cl, 'MsgItemPickup', "", %i, %pl.tool[%i]);

				if(%pl.currTool >= 0)
					commandToClient(%cl, 'SetActiveTool', %pl.currTool);
				else
					commandToClient(%cl, 'SetScrollMode', 0);
			}
		}
		else
			commandToClient(%cl, 'PlayGui_CreateToolHud', 5);
	}
}