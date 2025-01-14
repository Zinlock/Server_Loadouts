// * Customizable loadouts and stuff
// * By Oxy (260031)

// this code gets worse by the day

function lo(%str)
{
	if(%str $= "")
		exec("./server.cs");
	else
		exec("./" @ %str @ ".cs");
}

exec("./Support_InfoTxt.cs");
exec("./Support_StandaloneEvents.cs");
exec("./Support_InventoryMenu.cs");

$LODebug = 0;

function loTalk(%str)
{
	if($LODebug == 2)
		talk(%str);
	else if($LODebug == 1)
		echo(%str);
}

// Taken from Blockland Glass
function fileCopyH(%source, %destination) {
  %fo_source = new FileObject();
  %fo_dest = new FileObject();
  %fo_source.openForRead(%source);
  %fo_dest.openForWrite(%destination);
  while(!%fo_source.isEOF()) {
    %fo_dest.writeLine(%fo_source.readLine());
  }
  %fo_source.close();
  %fo_dest.close();
  %fo_source.delete();
  %fo_dest.delete();
}

function fileRead(%path)
{
	if(!isFile(%path))
		return;
	
	%file = new FileObject();
	if(!%file.openForRead(%path))
	{
		error("fileRead() - could not read file " @ %path);

		%file.close();
		%file.delete();
		return;
	}

	%str = "";

	while(!%file.isEOF())
	{
		if(%str $= "")
			%str = %file.readLine();
		else
			%str = %str NL %file.readLine();
	}

	%file.close();
	%file.delete();

	return %str;
}

function expandLocalPath(%path, %local)
{
	%path = trim(%path);

	if(isFile(%path))
		%path = trim(filePath(%path));

	if(strlen(%path) > 0 && getSubStr(%path, strlen(%path) - 1, 1) $= "/")
		%path = trim(filePath(%path));

	if(getSubStr(%local, 0, 2) $= "./")
		return %path @ "/" @ getSubStr(%local, 2, strlen(%local));
	else if(getSubStr(%local, 0, 3) $= "../")
		return filePath(%path) @ "/" @ getSubStr(%local, 3, strlen(%local));
}

function GameConnection::longCenterPrint(%cl, %str, %time)
{
	if(strlen(%str) < 255)
		commandToClient(%cl, 'centerPrint', %str, %time);
	else
		commandToClient(%cl, 'centerPrint', '%2%3%4%5%6', %time, getSubStr(%str, 0, 255), getSubStr(%str, 255, 255), getSubStr(%str, 510, 255), getSubStr(%str, 765, 255), getSubStr(%str, 1020, 255));
}

$loBigFont = "<font:impact:28>";
$loSmallFont = "<font:impact:19>";

$loActiveColor = "<color:44FF44>";
$loInactiveColor = "<color:AAAAAA>";
$loNeutralColor = "<color:FFFFFF>";

exec("./menu.cs");
exec("./sets.cs");
exec("./commands.cs");
exec("./package.cs");
exec("./events.cs");
exec("./prefs.cs");