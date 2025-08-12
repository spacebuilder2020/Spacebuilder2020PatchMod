using Assets.Scripts;
using HarmonyLib;
using StationeersMods.Interface;
using System.IO;
using System;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using UnityEngine;
using Util.Commands;

namespace NetworkingFixMod
{
    
    [StationeersMod("NetworkingFixMod", "NetworkingFixMod [StationeersMods]", "1.0.0")]
    class NetworkingFixMod : ModBehaviour
    {
        public override void OnLoaded(ContentHandler contentHandler)
        {
            //READ THE README FIRST! 
            
            //Config example
            // configBool = Config.Bind("Input",
            //     "Boolean",
            //     true,
            //     "Boolean description");
            
            Harmony harmony = new Harmony("NetworkingFix");
            harmony.PatchAll();
            ConsoleWindow.Print("NetworkingFix Loaded!");
        }
    }

    [HarmonyPatch]
    class NetworkServerPatch
    {
        [HarmonyPatch(typeof(NetworkServer), "HandleBlacklisting"), HarmonyPostfix]
        static void NetworkServer_HandleBlacklisting(ref NetworkMessages.VerifyPlayer msg, ref Client client) =>
            NetworkManager.CloseP2PConnectionServer(client);

        [HarmonyPatch(typeof(NetworkServer), "HandleIncorrectPassword"), HarmonyPostfix]
        static void NetworkServer_HandleIncorrectPassword(ref Client client) =>
            NetworkManager.CloseP2PConnectionServer(client);

        [HarmonyPatch(typeof(NetworkServer), "HandleIncorrectVersion"), HarmonyPostfix]
        static void NetworkServer_HandleIncorrectVersion(ref Client client, NetworkMessages.VerifyPlayer msg) => NetworkManager.CloseP2PConnectionServer(client);

        [HarmonyPatch(typeof(KickCommand), "Kick"), HarmonyPrefix]
        static bool KickCommand_Kick(ref string[] lineSplit)
        {
            if (!NetworkManager.IsActiveAsClient && (NetworkManager.IsClient || NetworkManager.IsServer) && lineSplit.Length == 1 && !ulong.TryParse(lineSplit[0], out var ignored))
            {
                string name = lineSplit[0];
                Client user = NetworkBase.Clients.Find(client => client.name == name);
                if (user != null)
                {
                    ConsoleWindow.PrintAction($"client '{user.name}' kicked from game");
                    user.Disconnect();
                }
                else
                {
                    ConsoleWindow.PrintError($"Unable to find client by name: {name}", true);
                    
                    var clients = NetworkBase.Clients.FindAll(client => client.name.ToLower().StartsWith(name.ToLower()) || client.name.ToLower().Contains(name.ToLower()));
                    if (clients.Count > 0)
                    {
                        ConsoleWindow.Print("Possible Options:");
                        clients.ForEach(client => ConsoleWindow.Print(client.name));
                    }
                }
                return false;
            }

            return true;
        }
    }
}