using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Carbon.Plugins
{
    [Info(_PluginName, _PluginAuthor, _PluginVersion)]
    [Description(_PluginDescription)]
    public class DeathGroups : CarbonPlugin
    {
        // Plugin Metadata
        private const string _PluginName = "DeathGroups";
        private const string _PluginAuthor = "BippyMiester";
        private const string _PluginVersion = "1.0.0";
        private const string _PluginDescription = "A knockoff of Gun Game that adds/removes a group from a user if they die.";
        
        // Class Variables
        private PluginConfig _config;

        private void Init()
        {
            ConsoleLog($"{_PluginName} has been initialized...");
            _config = Config.ReadObject<PluginConfig>();
            CreateGroups();
        }
        
        private void OnServerInitialized()
        {
            
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            CheckIfPlayerDataExists(player);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if(_config.Debug) { ConsoleLog("OnEntityDeath() Called"); }
            // Check if the entity is a player
            BasePlayer victim = entity as BasePlayer;
            if (victim != null)
            {
                if(_config.Debug) { ConsoleLog("Victim has the type of BasePlayer"); }
                // Check if the killer is a player
                BasePlayer killer = info.InitiatorPlayer;
                if (killer != null)
                {
                    if(_config.Debug) { ConsoleLog("Killer has the type of BasePlayer"); }
                    // Check if the killer has a valid Steam ID
                    string killerSteamID = killer.UserIDString;
                    if(_config.Debug) { ConsoleLog("Checking if the killer Steam ID is null or Empty"); }
                    if(_config.Debug) { ConsoleLog("Checking if the killer Steam ID is 16 digits long"); }
                    if(_config.Debug) { ConsoleLog("Checking if the killer and victim ID are the same"); }
                    if (!string.IsNullOrEmpty(killerSteamID) && killerSteamID.Length > 16 && victim.UserIDString != killerSteamID)
                    {
                        if(_config.Debug) { ConsoleLog("Check passed..."); }
                        // Do something with the killer's Steam ID
                        HandlePlayerKill(killerSteamID);
                    }
                }
            }
        }
        
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if(_config.Debug) { ConsoleLog($"OnPlayerDeath() Called"); }
            if(_config.Debug) { ConsoleLog($"Checking if player user ID is a valid steam ID"); }
            if(!player.userID.IsSteamId()) { return null; }
            if(_config.Debug) { ConsoleLog($"Steam ID is valid!"); }
            HandlePlayerDeath(player.UserIDString);
            HandlePlayerDeathGroup(player.UserIDString);
            return null;
        }

        #region HelperFunctions

        void HandlePlayerKill(string steamID)
        {
            if(_config.Debug) { ConsoleLog($"HandlePlayerKill() Called"); }
            HandlePlayerKillGroup(steamID);
            if(_config.Debug) { ConsoleLog($"Getting player deaths..."); }
            var playerDeaths = (int) DataFile[steamID];
            if(_config.Debug) { ConsoleLog($"Player has {playerDeaths.ToString()} deaths in the data file."); }
            if(_config.Debug) { ConsoleLog($"Subtracting 1 from player deaths..."); }
            playerDeaths -= 1;
            if(_config.Debug) { ConsoleLog($"Checking if player deaths is less then 0..."); }
            if (playerDeaths < 0)
            {
                if(_config.Debug) { ConsoleLog($"Player deaths is less then 0. Resetting player deaths to 0."); }
                playerDeaths = 0;
            }
            if(_config.Debug) { ConsoleLog($"Saving players data..."); }
            DataFile[steamID] = playerDeaths;
            SaveDataFile(DataFile);
            if(_config.Debug) { ConsoleLog($"Data has been saved."); }
            HandlePlayerDeathGroup(steamID);
        }

        void HandlePlayerKillGroup(string steamID)
        {
            if(_config.Debug) { ConsoleLog($"HandlePlayerKillGroup() Called"); }
            if(_config.Debug) { ConsoleLog($"Getting Player Deaths..."); }
            var playerDeaths = (int) DataFile[steamID];
            if(_config.Debug) { ConsoleLog($"Looping through all death groups"); }
            foreach (var deathGroup in _config.DeathGroups)
            {
                if(_config.Debug) { ConsoleLog($"Death Group: {deathGroup.Key}"); }
                if(_config.Debug) { ConsoleLog($"Death Group Lives: {deathGroup.Value}"); }
                if(_config.Debug) { ConsoleLog($"Checking if player deaths is equal to the death group lives"); }
                if(_config.Debug) { ConsoleLog($"Checking if the death group is not default"); }
                if (deathGroup.Value == playerDeaths && deathGroup.Key != "default")
                {
                    if(_config.Debug) { ConsoleLog($"Check passed."); }
                    if(_config.Debug) { ConsoleLog($"Removing user from group: {deathGroup.Key}"); }
                    permission.RemoveUserGroup(steamID, deathGroup.Key);
                }
            }
        }
        
        void HandlePlayerDeathGroup(string steamID)
        {
            if(_config.Debug) { ConsoleLog($"HandlePlayerDeathGroup() Called"); }
            if(_config.Debug) { ConsoleLog($"Getting Player Deaths"); }
            var playerDeaths = (int) DataFile[steamID];
            if(_config.Debug) { ConsoleLog($"Looping through all death groups"); }
            foreach (var deathGroup in _config.DeathGroups)
            {
                if(_config.Debug) { ConsoleLog($"Death Group: {deathGroup.Key}"); }
                if(_config.Debug) { ConsoleLog($"Death Group Lives: {deathGroup.Value}"); }
                if(_config.Debug) { ConsoleLog($"Checking if player deaths is equal to the death group lives"); }
                if(_config.Debug) { ConsoleLog($"Checking if the death group is not default"); }
                if (playerDeaths == deathGroup.Value && deathGroup.Key != "default")
                {
                    if(_config.Debug) { ConsoleLog($"Check passed."); }
                    if(_config.Debug) { ConsoleLog($"Adding user from group: {deathGroup.Key}"); }
                    permission.AddUserGroup(steamID, deathGroup.Key);
                }
            }
        }
        
        void HandlePlayerDeath(string steamID)
        {
            if(_config.Debug) { ConsoleLog($"HandlePlayerDeath() Called"); }
            if(_config.Debug) { ConsoleLog($"Getting Player Deaths"); }
            var playerDeaths = (int) DataFile[steamID];
            if(_config.Debug) { ConsoleLog($"Adding 1 to player deaths"); }
            playerDeaths += 1;
            if(_config.Debug) { ConsoleLog($"Saving player data..."); }
            DataFile[steamID] = playerDeaths;
            SaveDataFile(DataFile);
            if(_config.Debug) { ConsoleLog($"Player data saved!"); }
        }
        
        void CheckIfPlayerDataExists(BasePlayer player)
        {
            if(_config.Debug) { ConsoleLog($"{player._name} has connected... Checking if data exists."); }
            // If the player data entry is null then we need to create a new entry
            if (DataFile[player.userID.ToString()] == null)
            {
                if(_config.Debug) { ConsoleLog($"{player._name} data does not exist. Creating new entry now."); }
                DataFile[player.userID.ToString()] = 0;
                SaveDataFile(DataFile);
                if(_config.Debug) { ConsoleLog($"{player._name} Data has been created."); }
            }
        }

        void CreateGroups()
        {
            if(_config.Debug) { ConsoleLog($"CreateGroups() Called"); }
            var rank = 100;
            if(_config.Debug) { ConsoleLog($"Looping through all death groups"); }
            foreach (var deathGroup in _config.DeathGroups)
            {
                if(_config.Debug) { ConsoleLog($"Creating group {deathGroup.Key}"); }
                if (permission.GroupExists(deathGroup.Key))
                {
                    if(_config.Debug) { ConsoleWarn($"Group {deathGroup.Key} already exists!"); }
                    continue;
                }
                if(_config.Debug) { ConsoleLog($"Creating Group: {deathGroup.Key}"); }
                permission.CreateGroup(deathGroup.Key, deathGroup.Key.TitleCase(), rank);
                if(_config.Debug) { ConsoleLog($"Group Created!"); }
                rank -= 1;
            }
        }

        #endregion
        #region ConsoleHelpers

        protected void ConsoleLog(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(message);
        }

        protected void ConsoleError(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"Error: {message}");
        }

        protected void ConsoleWarn(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"Warning: {message}");
        }
        
        #endregion

        #region ConsoleCommands

        [Command("deathgroups.reset")]
        private void ResetLivesCommand(BasePlayer commandPlayer, string command, string[] args)
        {
            if(args.Length < 1) { ConsoleError("Command Usage: deathgroups.reset <playerID>"); return; }
            // var player = Player.FindById(76561198053544529);
            var player = Player.FindById(args[0]);
            var playerLives = (int) DataFile[player.UserIDString];
            if (player == null || playerLives == null) { ConsoleError($"Can not find the player with the ID of: {args[0]}"); }
            
            playerLives = 0;
            DataFile[player.UserIDString] = playerLives;
            SaveDataFile(DataFile);
            ConsoleLog($"Player data for {player._name} has been reset!");
        }

        #endregion
        
        #region Config

        protected override void LoadDefaultConfig()
        {
            ConsoleWarn("Creating a new configuration file");
            _config = new PluginConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                    SaveConfig();
                }
            }
            catch
            {
                ConsoleError("The configuration file is corrupted. Please delete the config file and reload the plugin.");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Enable Debug?")]
            public bool Debug { get; set; }

            [JsonProperty(PropertyName = "Death Groups")]
            public Dictionary<string, int> DeathGroups = new Dictionary<string, int>()
            {
                { "default", 0 },
                { "amateur", 1 },
                { "noob", 2 }
            };
        }

        #endregion

        #region Data

        protected DynamicConfigFile DataFile = Interface.Oxide.DataFileSystem.GetDatafile(_PluginName);

        private void SaveDataFile(DynamicConfigFile data)
        {
            data.Save();
            ConsoleLog("Data file has been updated.");
        }

        #endregion
    }
}