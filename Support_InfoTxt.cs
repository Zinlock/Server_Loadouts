// Its like JSON, but worse! :D

function newInfoGroup(%obj, %path)
{
	if(%path !$= "" && isFile(%path))
	{
		%grp = readInfoFile(%path).getGroup();

		if(isObject(%grp))
		{
			if(isObject(%obj))
				%obj.delete();

			%grp.sourcePath = %path;

			return %grp;
		}
	}

	if(!%obj.isInfoObject)
		%obj = "";

	%group = new ScriptGroup(InfoGroup) { className = "InfoGroup"; isInfoGroup = true; mainObject = %obj; sourcePath = %path; };

	if(isObject(%obj))
		%group.add(%obj);
	
	if(isObject(MissionCleanup) && MissionCleanup.isMember(%group))
		MissionCleanup.remove(%group);

	return %group;
}

function newInfoObject()
{
	%obj = new ScriptObject(InfoObject) { className = "InfoObject"; isInfoObject = true; };

	if(isObject(MissionCleanup) && MissionCleanup.isMember(%obj))
		MissionCleanup.remove(%obj);

	return %obj;
}

function readInfoFile(%path)
{
	if(!isFile(%path))
		return -1;

	%file = new FileObject();
	if(!%file.openForRead(%path))
	{
		error("readInfoFile() - failed to read file " @ %path);

		%file.close();
		%file.delete();
		return -1;
	}

	%mainObj = newInfoObject();
	%group = newInfoGroup(%mainObj);

	%listName = "";
	%listIdx = 0;
	%currObj = %mainObj;
	%currTab = 0;

	%cts = 0;

	while(!%file.isEOF())
	{
		%cts++;
		%line = getLineNoComment(%file.readLine());

		%tabs = firstWord(%line);
		%line = restWords(%line);

		if(%line $= "")
			continue;

		%line = strReplace(%line, "%NL%", "\n");

		%key = firstWord(%line);

		if(%key $= ":")
		{
			warn("readInfoFile() - line " @ %cts @ ": found empty key");
			continue;
		}

		if(strlen(%key) > 0)
		{
			%keyStart = getSubStr(%key, 0, strlen(%key) - 1);
			%keyEnd = getSubStr(%key, strlen(%key)-1, 1);
		}
		else
		{
			%keyStart = %key;
			%keyEnd = "";
		}

		%val = restWords(%line);

		if(%listName !$= "")
		{
			if(%tabs < %currTab)
			{
				%listName = "";
				%listIdx = 0;

				if(%tabs == 0)
					%currObj = %mainObj;
				else
					%currObj = %tabObj[%tabs];

				%currTab = %tabs;
			}
			else if (%tabs == %currTab)
			{
				%currObj.list[%listName, %listIdx] = %line;
				%currObj.num[%listName]++;
				%listIdx++;
			}
			else
			{
				warn("readInfoFile() - line " @ %cts @ ": invalid indentation");
				continue;
			}
		}
		else
		{
			if(%tabs < %currTab)
			{
				if(%tabs == 0)
					%currObj = %mainObj;
				else
					%currObj = %tabObj[%tabs];

				%currTab = %tabs;
			}
			else if (%tabs > %currTab)
			{
				warn("readInfoFile() - line " @ %cts @ ": invalid indentation");
				continue;
			}
		}

		if(%listName $= "")
		{
			if(%keyEnd $= ":")
			{
				%currTab = %tabs + 1;
				if(%val $= "list")
				{
					%listName = %keyStart;
					%listIdx = 0;
					%currObj.num[%listName] = 0;
				}
				else
				{
					%obj = new ScriptObject(InfoObject) { isInfoObject = true; };
					%group.add(%obj);
					%currObj.field[%keyStart] = %obj;
					%currObj.isObj[%keyStart] = true;
					%tabObj[%currTab] = %obj;

					%currObj = %obj;
				}
			}
			else
			{
				if(%currObj.field[%key] !$= "")
					warn("readInfoFile() - line " @ %cts @ ": duplicate field " @ %key);

				%currObj.field[%key] = %val;
				%currObj.isObj[%keyStart] = false;
			}
		}
	}

	%file.close();
	%file.delete();
	return %mainObj;
}

function writeInfoFile(%obj, %path)
{
	if(%obj.isInfoGroup)
	{
		%group = %obj;
		%obj = %group.mainObject;
	}
	else if(%obj.isInfoObject)
	{
		%group = %obj.getGroup();
		%obj = %obj.getRoot();
	}
	else
	{
		error("writeInfoFile() - object " @ %obj @ " is not a valid info object or group");
		return false;
	}

	%file = new FileObject();
	if(!%file.openForWrite(%path))
	{
		error("writeInfoFile() - failed to write to file " @ %path);

		%file.close();
		%file.delete();
		return false;
	}

	%obj.write(%file, 0);
	
	%file.close();
	%file.delete();
	return true;
}

function InfoObject::write(%obj, %file, %tabs)
{
	if(%tabs > 0)
		%pad = makePadString("\t", %tabs);

	%padList = makePadString("\t", %tabs + 1);

	for(%i = 0; (%field = %obj.getTaggedField(%i)) !$= ""; %i++)
	{
		%name = getField(%field, 0);
		%value = getFields(%field, 1);

		if(getSubStr(%name, 0, 5) $= "field")
		{
			if(isObject(%value) && %value.isInfoObject && %obj.isObj[getSubStr(%name, 5, strlen(%name))])
			{
				%file.writeLine(%pad @ getSubStr(%name, 5, strlen(%name)) @ ":");
				%value.write(%file, %tabs + 1);
			}
			else
				%file.writeLine(%pad @ getSubStr(%name, 5, strlen(%name)) SPC strReplace(%value, "\n", "%NL%"));
		}
		else if(getSubStr(%name, 0, 3) $= "num")
		{
			%list = getSubStr(%name, 3, strlen(%name));
			%file.writeLine(%pad @ %list @ ": list");

			for(%o = 0; %o < %value; %o++)
				%file.writeLine(%padList @ strReplace(%obj.list[%list, %o], "\n", "%NL%"));
		}
	}
}

function InfoObject::getRoot(%obj)
{
	if(isObject(%grp = %obj.getGroup()))
		return %grp.mainObject;
}

function InfoObject::get(%obj, %key, %default, %auto, %isObj)
{
	%val = %obj.field[%key];

	if(%val $= "")
	{
		if(%auto)
		{
			if(%default $= "")
			{
				%default = newInfoObject();
				%isObj = true;
			}

			%obj.set(%key, %default, %isObj);
		}

		return %default;
	}

	return %val;
}

function InfoObject::set(%obj, %key, %val, %isObj)
{
	%obj.field[%key] = %val;

	if(isObject(%val))
		%obj.isObj[%key] = %isObj;
	else
		%obj.isObj[%key] = false;
}

function InfoObject::listNum(%obj, %key)
{
	return %obj.num[%key];
}

function InfoObject::listGet(%obj, %key, %idx)
{
	if(%obj.num[%key] > 0)
	{
		if(%idx >= %obj.num[%key])
			return "";
		else
			return %obj.list[%key, %idx];
	}

	return "";
}

function InfoObject::listReplace(%obj, %key, %list)
{
	%obj.num[%key] = getRecordCount(%list);

	%cts = %obj.num[%key];
	for(%i = 0; %i < %cts; %i++)
		%obj.list[%key, %i] = getRecord(%list, %i);
}

function InfoObject::listSet(%obj, %key, %idx, %val)
{
	if(%val $= "")
	{
		%obj.listRemove(%key, %idx);
		return;
	}

	%obj.list[%key, %idx] = %val;
}

function InfoObject::listRemove(%obj, %key, %idx)
{
	%cts = %obj.listNum(%key);

	for(%i = %idx; %i < %cts; %i++)
		%obj.list[%key, %i] = %obj.list[%key, %i+1];
	
	%obj.num[%key]--;
}

function InfoObject::listAdd(%obj, %key, %val)
{
	if(%obj.num[%key] $= "")
		%obj.num[%key] = 0;

	%obj.list[%key, %obj.num[%key]] = %val;
	%obj.num[%key]++;
}

function InfoObject::listDump(%obj, %key)
{
	%str = "";
	%cts = %obj.listNum(%key);

	for(%i = 0; %i < %cts; %i++)
		%str = %str NL %obj.listGet(%key, %i);

	return trim(%str);
}

function InfoObject::listFind(%obj, %key, %val)
{
	for(%i = 0; (%str = %obj.listGet(%key, %i)) !$= ""; %i++)
	{
		if(%str $= %val)
			return %i;
	}

	return -1;
}

function InfoGroup::onRemove(%grp)
{
	%grp.inRemoval = true;

	if(%grp.sourcePath !$= "" && isObject(%obj = %grp.mainObject))
		writeInfoFile(%obj, %grp.sourcePath);
}

function getLineNoComment(%line)
{
	if(trim(%line) $= "")
		return 0;

	%tabs = 0;

	for(%i = 0; %i < strlen(%line); %i++)
	{
		%chr = getSubStr(%line, %i, 1);

		if(%chr $= "\t")
			%tabs++;
		else
			break;
	}

	%line = trim(%line);

	%cts = strlen(%line);
	%idx = 0;
	while(%idx < %cts)
	{
		%pos = strpos(%line, "#", %idx);

		if(%pos >= 1)
		{
			if(getSubStr(%line, %pos-1, 1) $= "\\")
				%idx = %pos+1;
			else
			{
				%line = getSubStr(%line, 0, %pos);
				break;
			}
		}
		else if(%pos == 0)
		{
			%line = "";
			break;
		}
		else if(%pos == -1)
			break;
	}

	return trim(%tabs SPC %line);
}