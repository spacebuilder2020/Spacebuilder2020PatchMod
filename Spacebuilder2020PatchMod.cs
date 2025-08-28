using Assets.Scripts;
using HarmonyLib;
using StationeersMods.Interface;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using Steamworks;
using UnityEngine;
using Util.Commands;

namespace Spacebuilder2020PatchMod
{
    
    [StationeersMod("Spacebuilder2020PatchMod", "Spacebuilder2020PatchMod [StationeersMods]", "1.2")]
    class Spacebuilder2020PatchMod : ModBehaviour
    {
        public override void OnLoaded(ContentHandler contentHandler)
        {
            ConsoleWindow.Print("Loading Patches for Spacebuilder2020's PatchMod");
            Harmony harmony = new Harmony("Spacebuilder2020PatchMod");
            harmony.PatchAll();
            ConsoleWindow.Print("Patches Loaded!");
        }
    }

    [HarmonyPatch]
    class Spacebuilder2020Patches
    {
        [HarmonyPatch(typeof(NetworkServer), "HandleBlacklisting"), HarmonyPostfix]
        static void NetworkServer_HandleBlacklisting(ref NetworkMessages.VerifyPlayer msg, ref Client client) => DelayedClose(client);

        [HarmonyPatch(typeof(NetworkServer), "HandleIncorrectPassword"), HarmonyPostfix]
        static void NetworkServer_HandleIncorrectPassword(ref Client client) => DelayedClose(client);

        [HarmonyPatch(typeof(NetworkServer), "HandleIncorrectVersion"), HarmonyPostfix]
        static void NetworkServer_HandleIncorrectVersion(ref Client client, NetworkMessages.VerifyPlayer msg) => DelayedClose(client);

        static void DelayedClose(Client client)
        {
            Task.Run(() =>
            {
                Thread.Sleep(500);
                NetworkManager.CloseP2PConnectionServer(client);
            });
        }
        [HarmonyPatch(typeof(KickCommand), "Kick"), HarmonyPrefix]
        static bool KickCommand_Kick(ref string[] lineSplit)
        {
            if (!NetworkManager.IsActiveAsClient && (NetworkManager.IsClient || NetworkManager.IsServer) && lineSplit.Length == 1)
            {
                string nameOrID = lineSplit[0];
                bool parsed = ulong.TryParse(lineSplit[0], out var clientId); 
                Client user = parsed ? Client.Find(clientId) : NetworkBase.Clients.Find(client => client.name == nameOrID);

                if (user != null)
                {
                    ConsoleWindow.PrintAction($"client '{user.name}' kicked from game");
                    user.Disconnect();
                }
                else
                {
                    ConsoleWindow.PrintError($"Unable to find client by {(parsed ? "id" :"name")}: '{nameOrID}' in list", true);
                    
                    if (!parsed)
                    {
                        var clients = NetworkBase.Clients.FindAll(client =>
                            client.name.ToLower().StartsWith(nameOrID.ToLower()) ||
                            client.name.ToLower().Contains(nameOrID.ToLower()));
                        if (clients.Count > 0)
                        {
                            ConsoleWindow.Print("Possible Options:");
                            clients.ForEach(client => ConsoleWindow.Print(client.name));
                        }
                    }
                    else
                    {
                        ConsoleWindow.PrintAction("Trying a kick by exact steam id");
                        
                        if (SteamNetworking.CloseP2PSessionWithUser(clientId))
                        {
                            ConsoleWindow.Print("Successfully kicked by Steam!");
                        }
                        else
                        {
                            ConsoleWindow.PrintError("Error kicking by id, user may not be connected!", true);
                        }
                        
                    }
                }
                return false;
            }

            return true;
        }
        
        [HarmonyPatch(typeof(Item), "UpdateDecayTimes"), HarmonyPrefix]
        static bool Item_UpdateDecayTimes(ref Item __instance) {
            Traverse.Create(__instance).Method("set_TimeToDecayFromNowInSeconds", 
                    (int) ((__instance.DamageState.MaxDamage - (double) __instance.DamageState.Decay) / ( __instance.CurrentDecayRate * 60.0)))
                .GetValue();
            return false;
        }
    }
}