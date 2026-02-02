using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AktualizatorEME
{
    public class SettingsModel
    {
        [JsonProperty("username")]
        public string Username { get; set; } = "";

        [JsonProperty("password")]
        public string Password { get; set; } = "";

        [JsonProperty("ip")]
        public string Ip { get; set; } = "server.endore.pl";

        [JsonProperty("port")]
        public int Port { get; set; } = 4003;

        [JsonProperty("ultimaonlinedirectory")]
        public string UltimaOnlineDirectory { get; set; } = "";

        [JsonProperty("clientversion")]
        public string ClientVersion { get; set; } = "7.0.40.0";

        [JsonProperty("autologin")]
        public bool Autologin { get; set; }

        [JsonProperty("reconnect")]
        public bool Reconnect { get; set; }

        [JsonProperty("login_music")]
        public bool LoginMusic { get; set; }

        [JsonProperty("login_music_volume")]
        public int LoginMusicVolume { get; set; } = 50;

        [JsonProperty("plugins")]
        public List<string> Plugins { get; set; } = new List<string>();

        // NOWE: Flaga określająca czy profil był tworzony w trybie dewelopera
        [JsonProperty("is_dev")]
        public bool IsDev { get; set; } = false;
    }
}