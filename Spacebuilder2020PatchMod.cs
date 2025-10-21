using Assets.Scripts;
using HarmonyLib;
using StationeersMods.Interface;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
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
            var gameAssembly = typeof(GameManager).Assembly;
            
            Debug.Log($"Game Version: {gameAssembly.GetName().Version}");
            var assembly = typeof(Spacebuilder2020PatchMod).Assembly;
            AccessTools.GetTypesFromAssembly(assembly).Do(type =>
            {

                var ver = type.GetCustomAttributes(true).OfType<GameVersion>().FirstOrDefault();
                var version = gameAssembly.GetName().Version;
                
                if (ver != null && (ver.MinVersion > version || ver.MaxVersion < version) )
                {
                    Debug.Log($"Patch for {type.Name} ignored because game version does not match!");
                    Debug.Log($"Type: {type.Name} Build: {version} Min: {ver?.MinVersion} Max: {ver?.MaxVersion}");
                    return;
                }
                var processor = harmony.CreateClassProcessor(type);
                
                processor.Patch();
            });
            ConsoleWindow.Print("Patches Loaded!");
        }
    }
    [AttributeUsage(AttributeTargets.Class)]
    class GameVersion : Attribute
    {
        public Version MinVersion;
        public Version MaxVersion;

        public GameVersion(string minVersion, string maxVersion)
        {
            MinVersion = new Version(minVersion);
            MaxVersion = new Version(maxVersion);
        }
    }
    
    [HarmonyPatch, GameVersion("0.0.0.0","0.2.5959.26190")]
    class Spacebuilder2020PatchesLegacy
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
    
    [HarmonyPatch, GameVersion("0.2.5958.26183","0.2.9999.99999")]
    class Spacebuilder2020PatchesPost26183
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
    }
}