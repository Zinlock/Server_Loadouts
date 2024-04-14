// * Support_StandaloneEvents by Oxy (260031)
// * Adds helper functions to create and trigger brick events without actually planting bricks

// * Directly triggers a set of events on a set of targets, using a specific input event
// * %targets is a tab-separated list of output targets (ex: "Client " @ %client TAB "Player " @ %player)
// * %input is the input event to use for every line
// * %lines is a line separated list of events formatted like this:
	// delay OutputTarget OutputEvent "arg1" "arg2" "arg3" "arg4"
	// ex: 500 Client CenterPrint "Test Message" 2
function SETriggerEvent(%targets, %input, %lines)
{
	if(isObject(%brk = $SEEventBrick[%input, %lines]) && %brk.sequence != $SESequence)
		%brk.delete();

	if(!isObject(%brk = $SEEventBrick[%input, %lines]))
	{
		%brk = new FxDTSBrick(see)
		{
			datablock = Brick1x1fData;
			position = "0 0 -8192";
			isSEBrick = true;
			sequence = $SESequence;
		};

		MissionCleanup.add(%brk);
		$SEEventBrick[%input, %lines] = %brk;

		%cts = getRecordCount(%lines);
		for(%i = 0; %i < %cts; %i++)
		{
			%line = getRecord(%lines, %i);
			%brk.SEAddEvent(%line, true, %input);
		}
	}

	%cl = -1;

	%cts = getFieldCount(%targets);
	for(%i = 0; %i < %cts; %i++)
	{
		%field = getField(%targets, %i);
		%name = getWord(%field, 0);
		%target = getWord(%field, 1);

		$InputTarget_[%name] = %target;

		if(%name $= "Client")
			%cl = %target;
	}

	%brk.processInputEvent(%input, %cl);
}

// * === Other functions ===

// * Goes through all existing input and output events and registers extra info about them here
function SESetup()
{
	$SESequence++;

	if(isObject($SEClient))
		$SEClient.delete();

	$SEClient = new ScriptObject(sec)
	{
		name = "SE";
		bl_id = 888888;
		brickGroup = BrickGroup_888888;
		isAdmin = true;
		isSuperAdmin = true;
	};

	deleteVariables("$SEInput*");
	deleteVariables("$SEOutput*");

	// why can you register input events for any class type if events can only be triggered on bricks?
	// why is the input event function defined on SimObject if it checks if the object is a brick anyway?
	%iclass = "FxDTSBrick";
	for(%i = 0; %i < $InputEvent_Count[%iclass]; %i++)
	{
		%name = $InputEvent_Name[%iclass, %i];
		%targets = $InputEvent_TargetList[%iclass, %i];

		$SEInputEventId[%name] = %i;
		$SEInputEventName[%i] = %name;
		$SEInputTargets[%i] = getFieldCount(%targets);
		for(%t = 0; %t < $SEInputTargets[%i]; %t++)
		{
			%target = getField(%targets, %t);
			%targName = getWord(%target, 0);
			%targClass = getWord(%target, 1);
			$SEInputTargetName[%i, %t] = %targName;
			$SEInputTargetClass[%i, %t] = %targClass;
			$SEInputTargetIdxName[%i, %targName] = %t;
			$SEInputTargetIdxClass[%i, %targClass] = %t;
		}
	}
	$SEInputEvents = $InputEvent_Count[%iclass];

	$SEOutputClasses = 0;
	%cts = getWordCount($OutputEvent_ClassList);
	for(%o = 0; %o < %cts; %o++)
	{
		%oclass = getWord($OutputEvent_ClassList, %o);

		// for whatever reason, the class list is case sensitive so it contains duplicates
		if(%odone[%oclass])
			continue;

		for(%i = 0; %i < $OutputEvent_Count[%oclass]; %i++)
		{
			%name = $OutputEvent_Name[%oclass, %i];
			%params = $OutputEvent_parameterList[%oclass, %i];
			
			$SEOutputEventId[%oclass, %name] = %i;
			$SEOutputEventName[%oclass, %i] = %name;
			$SEOutputParams[%oclass, %i] = getFieldCount(%params);
			for(%p = 0; %p < $SEOutputParams[%oclass, %i]; %p++)
			{
				%param = getField(%params, %p);
				%type = firstWord(%param);
				%settings = restWords(%param);
				$SEOutputParamType[%oclass, %i, %p] = %type;
				$SEOutputParamSettings[%oclass, %i, %p] = %settings;
			}
		}

		$SEOutputEvents[%oclass] = $OutputEvent_Count[%oclass];
		$SEOutputClass[$SEOutputClasses] = %oclass;
		$SEOutputClasses++;

		%odone[%oclass] = true;
	}

	if($Support_MultiSourceEvents::Version !$= "")
	{
		// this is stupid, but slayer's "Adding event." spam is stupider
		eval("package Support_MultiSourceEvents{function serverCmdAddEvent(%client, %enabled, %inputEventIdx, %delay, %targetIdx, %namedTargetNameIdx, %outputEventIdx, %par1, %par2, %par3, %par4)" NL 
		"{%obj = %client.wrenchBrick;%class = %obj.getClassName();parent::serverCmdAddEvent(%client, %enabled, %inputEventIdx, %delay, %targetIdx, %namedTargetNameIdx, %outputEventIdx, %par1, %par2, %par3, %par4);" NL
		"if($InputEvent_MultiSource[%class, %inputEventIdx]){%group = \"multiSourceEventGroup\" @ %inputEventIdx;if(!isObject(%group)){new SimSet(%group);missionCleanup.add(%group);}echo(%group);%group.add(%obj);}}};");
	}
}

package StandaloneEventsPkg
{
	function GameConnection::onClientEnterGame(%cl)
	{
		if($SESequence $= "")
			schedule(0, 0, SESetup);

		return Parent::onClientEnterGame(%cl);
	}
};
activatePackage(StandaloneEventsPkg);

function serverCmdSEReload(%cl)
{
	if(!%cl.isAdmin)
		return;

	SESetup();
	messageClient(%cl, '', "\c5Reloaded SE events.");
}

// * Parses a single line string, separating words into fields and keeping words in quotes together
function SEParseQuotes(%str)
{
	%len = strLen(%str);

	%strings = 0;
	%inQuote = false;
	%startIdx = 0;
	%quoteIdx = -1;
	%qi = 0;

	while(%qi != -1)
	{
		// find the next quote
		%qi = stripos(%str, "\"", %qi);

		if(%qi == -1) // no quote found
		{
			if(%inQuote)
			{
				warn("SEParseQuotes() - missing closing quote for string: " @ %str);

				%sub = trim(getSubStr(%str, %quoteIdx, %len)); // add the entire quote to the list in one piece (the one piece is real)
				%sub = strReplace(%sub, "\\\"", "\""); // collapse escaped quotes
				%string[%strings] = %sub;
				%strings++;
			}
			else
			{
				%sub = trim(getSubStr(%str, %startIdx, %len));

				%cts = getWordCount(%sub); // add all remaining words to the list
				for(%i = 0; %i < %cts; %i++)
				{
					%string[%strings] = getWord(%sub, %i);
					%strings++;
				}
			}
			break;
		}

		%last = getSubStr(%str, %qi - 1, 1); // check if we're escaping this quote
		if(%last $= "\\")
		{
			%qi++; // quote escaped, increase index so we don't get stuck on the same quote
			continue;
		}
		else
		{
			if(%inQuote)
			{
				%inQuote = false; // we're done reading a quote; dump it all at once
				%startIdx = %qi + 2;

				%sub = trim(getSubStr(%str, %quoteIdx, %qi - %quoteIdx)); // add the quote to the list
				%sub = strReplace(%sub, "\\\"", "\"");
				%string[%strings] = %sub;
				%strings++;
			}
			else
			{
				%inQuote = true; // we are now reading a quote; dump the previous text word by word
				%quoteIdx = %qi + 1;

				%sub = trim(getSubStr(%str, %startIdx, %quoteIdx - %startIdx - 1));

				%cts = getWordCount(%sub); // add the words between the last and next quote to the list
				for(%i = 0; %i < %cts; %i++)
				{
					%string[%strings] = getWord(%sub, %i);
					%strings++;
				}
			}

			%qi++;
		}
	}

	for(%i = 0; %i < %strings; %i++) // take all quotes in the list and concat them together into a list of fields
	{
		if(%st2 $= "")
			%st2 = %string[%i];
		else
			%st2 = %st2 TAB %string[%i];
	}

	return %st2;
}

// * Adds an event to a brick
function FxDTSBrick::SEAddEvent(%brk, %line, %direct, %dInput)
{
	// example event line: 1 500 OnActivate Client Centerprint "test message" 3

	%str = SEParseQuotes(%line);
	%word = -1;

	if(!%direct)
		%active = getField(%str, %word++) >= 1;
	else
		%active = true;

	%delay = getField(%str, %word++) * 1;

	if(!%direct)
		%input = firstWord(getField(%str, %word++));
	else
		%input = %dInput;

	%inputId = $SEInputEventId[%input];

	%target = firstWord(getField(%str, %word++));
	%targetId = $SEInputTargetIdxName[%inputId, %target];
	%targetClass = $SEInputTargetClass[%inputId, %targetId];

	if(%direct && %targetClass $= "FxDTSBrick")
	{
		error("FxDTSBrick::SEAddEvent() - direct events can not target bricks (event line " @ %line @ ")");
		return;
	}

	%output = firstWord(getField(%str, %word++));
	%outputId = $SEOutputEventId[%targetClass, %output];

	%arg0 = getField(%str, %word++);
	%arg1 = getField(%str, %word++);
	%arg2 = getField(%str, %word++);
	%arg3 = getField(%str, %word++);

	for(%i = 0; %i < 4; %i++)
	{
		%type = $SEOutputParamType[%targetClass, %outputId, %i];
		%setting = $SEOutputParamSettings[%targetClass, %outputId, %i];

		if(%type $= "datablock")
			%arg[%i] = SEFindDatablock(%setting, %arg[%i]);
		else if(%type $= "list")
			%arg[%i] = SEGetListValue(%setting, %arg[%i]);
		else if(%type $= "paintColor")
			%arg[%i] = SEGetClosestColorId(%arg[%i]);
	}

	%llb = $LastLoadedBrick;
	$SEClient.wrenchBrick = %brk;
	$LastLoadedBrick = %brk;
	serverCmdAddEvent($SEClient, %active, %inputId, %delay, %targetId, -1, %outputId, %arg0, %arg1, %arg2, %arg3);
	$LastLoadedBrick = %llb;
}

// * Searches for a datablock by type and name
function SEFindDatablock(%type, %name, %exact)
{
	%name = trim(%name);
	if(%name $= "")
		return -1;

	%match = -1;

	%cts = DataBlockGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%db = DataBlockGroup.getObject(%i);
		%class = %db.getClassName();

		%uin = trim(%db.uiName);

		if(%type $= "Sound")
		{
			if(%class !$= "AudioProfile" || %db.description.isLooping || !%db.description.is3D)
				continue;
			else
				%uin = fileName(%db.fileName);
		}
		else if(%type $= "Music")
		{
			if(%class !$= "AudioProfile" || !%db.description.isLooping)
				continue;
		}
		else if(%type $= "Vehicle")
		{
			if(%class !$= "WheeledVehicleData" && %class !$= "FlyingVehicleData" && (%class !$= "PlayerData" || !%db.rideAble))
				continue;
		}
		else if(%class !$= %type)
			continue;

		if(%uin $= %name)
			return %db.getId();

		if(stripos(%uin, %name) != -1)
			%match = %db.getId();
	}

	if(!%exact)
		return %match;
	else
		return -1;
}

// * Gets a value from a key value pair list
function SEGetListValue(%list, %name)
{
	%match = "";

	%cts = getWordCount(%list);
	for(%i = 0; %i < %cts; %i += 2)
	{
		%key = getWord(%list, %i);
		%value = getWord(%list, %i + 1);

		if(%key $= %name)
			return %value;
		else if(stripos(%key, %name) != -1)
			%match = %value;
	}

	return %match;
}

// * Gets the closest colorset id to this color
function SEGetClosestColorId(%color)
{
	%closest = -1;
	%dist = 999;

	for(%i = 0; %i < 64; %i++)
	{
		%col = getColorIDTable(%i);
		%cAlpha = getWord(%col, 3);
		if(%cAlpha < 0.001)
			break; // end of colorset

		%alpha = getWord(%color, 3);

		// if we're looking for an opaque color, ignore transparent ones (and vice versa)
		if(%cAlpha >= 1.0 && %alpha < 1.0 || %cAlpha < 1.0 && %alpha >= 1.0)
			continue;

		// compare rgb, not alpha
		%cDist = VectorDist(%color, %col);
		if(%cDist < %dist)
		{
			%dist = %cDist;
			%closest = %i;
		}
	}

	return %closest;
}