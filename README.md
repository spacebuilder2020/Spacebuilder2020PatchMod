# [Spacebuilder2020's Patch Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3552686002)

    Multiple Patches including:

    Patches connections to prevent a timeout when a client is kicked at initial connection. (Invalid password, blacklisting, invalid version).
    
    Extends Kick command so that a player name can be specified instead of a steamid.
      Requires exact name, if name is not exact, will try to find possible options and print them out.

    Fix the decay time method to use the max damage value instead of hardcoding 100.
      Reason:  by default, the max damage value is set to 200 when items are initialized.  Thus, when an items time to decay is shown, it is inaccurate.