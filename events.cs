registerOutputEvent(GameConnection, LOOverrideSet, "string 200 200");

$OutputDescription_["GameConnection", "LOOverrideSet"] = "[name]" NL
                                                         "Overrides this client's default loadout set." NL
                                                         "Removed upon resetting or leaving a minigame." NL
                                                         "name: Loadout set name to use";

function GameConnection::LOOverrideSet(%cl, %name)
{
	if(%cl.LOSetOverride !$= %name)
	{
		%cl.LOSetOverride = %name;
		%cl.LOValidateLoadout();
	}
}

registerOutputEvent(GameConnection, LORandomizeLoadout, "int -999 999 0");

$OutputDescription_["GameConnection", "LORandomizeLoadout"] = "[seed]" NL
                                                              "Randomizes this client's current loadout set." NL
                                                              "seed: Random seed to use, same seed is always the same loadout (0 for true random)";

registerOutputEvent(Player, LOApplyLoadout, "bool" TAB "bool");

$OutputDescription_["Player", "LOApplyLoadout"] = "[force] [silent]" NL
                                                  "Applies this player's active loadout." NL
                                                  "force: Applies their loadout even if it is already equipped" NL
                                                  "silent: Hides the centerprint message";

function Player::LOApplyLoadout(%pl, %force, %silent)
{
	if(isObject(%cl = %pl.Client))
		%cl.LOApplyLoadout(false, %force, %silent);
}

registerOutputEvent(Player, LOResupplyLoadout, "bool" TAB "bool");

$OutputDescription_["Player", "LOResupplyLoadout"] = "[force] [silent]" NL
                                                     "Resupplies this player's last loadout." NL
                                                     "force: Applies their loadout instead if it was changed" NL
                                                     "silent: Hides the centerprint message";

function Player::LOResupplyLoadout(%pl, %force, %silent)
{
	if(isObject(%cl = %pl.Client))
		%cl.LOApplyLoadout(true, %force, %silent);
}