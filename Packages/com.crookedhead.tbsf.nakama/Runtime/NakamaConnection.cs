using Nakama;
using Nakama.TinyJson;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using TurnBasedStrategyFramework.Common.Network;
using TurnBasedStrategyFramework.Unity.Network;
using UnityEngine;

namespace TbsfNakamaClient
{
    /// <summary>
    /// A concrete implementation of NetworkConnection for a custom Nakama server. This class provides functionalities
    /// specific to connecting, interacting, and managing multiplayer game sessions using a customized Nakama server as the backend.
    /// It encompasses operations such as server connection, match joining, room creation, match state management,
    /// and player presence event handling.
    /// </summary>
    /// <remarks>
    /// The NakamaConnection class is designed to work with a custom version of the Nakama server, tailored to support specific
    /// features and workflows required by the Turn Based Strategy Framework. It extends the abstract NetworkConnection class, implementing methods to
    /// interface with this custom server's unique functionalities. This includes handling specialized authentication, matchmaking,
    /// room management, and real-time communication processes.
    /// 
    /// For more details on deploying the custom Nakama server, refer to the GitHub repository at: https://github.com/mzetkowski/tbsf-nakama-server
    /// </remarks>
    public class NakamaConnection : NetworkConnection
    {
        /// <summary>
        /// The communication protocol scheme used for the Nakama server connection.
        /// Typically 'http' for development or 'https' for secure production environments.
        /// </summary>
        [SerializeField] private string _scheme = "http";

        /// <summary>
        /// The host address or IP of the Nakama server. This could be a local address (like 'localhost' or '127.0.0.1')
        /// for development, or a remote server address in production.
        /// </summary>
        [SerializeField] private string _host = "";

        /// <summary>
        /// The port number on which the Nakama server is listening. The default port for Nakama is 7350,
        /// but this might be different based on your server configuration.
        /// </summary>
        [SerializeField] private int _port = 7350;

        /// <summary>
        /// The server key for Nakama server authentication. This key is used to authenticate the client
        /// with the Nakama server and should match the key configured on the server side. 
        /// It's a crucial part of securing your game's server interactions.
        /// </summary>
        [SerializeField] private string _serverKey = "defaultkey";

        private IClient _client;
        private ISession _session;
        private ISocket _socket;

        private string _currentMatchId;
        private IMatch _currentMatch;
        private NetworkUser _localUser;
        private HashSet<IUserPresence> _presences;

        private TaskCompletionSource<bool> _roomJoinedTcs;

        public async override void ConnectToServer(string userName, Dictionary<string, string> customParams)
        {
            _client = new Client(_scheme, _host, _port, _serverKey);

            try
            {
                _session = await _client.AuthenticateDeviceAsync($"{SystemInfo.deviceUniqueIdentifier}_{Time.time}", userName);
                _localUser = new NetworkUser(_session.Username, _session.UserId, new Dictionary<string, string>());
            }
            catch (TaskCanceledException)
            {
                throw new NetworkException("Could not connect to server");
            }

            _socket = _client.NewSocket(true);
            _socket.Connected += () =>
            {
                _socket.ReceivedMatchmakerMatched += OnMatchFoundAsync;
                _socket.ReceivedMatchState += OnReceivedMatchState;
                _socket.ReceivedMatchPresence += OnReceivedMatchPresence;

                InvokeServerConnected();
            };
            await _socket.ConnectAsync(_session, true);
        }
        public async override void JoinQuickMatch(int maxPlayers, Dictionary<string, string> customParams)
        {
            var matchParams = new Dictionary<string, string> { { "maxPlayers", maxPlayers.ToString() } };
            await _socket.AddMatchmakerAsync("*", 2, maxPlayers, matchParams);
        }
        public override async void CreateRoom(string roomName, int maxPlayers, bool isPrivate, Dictionary<string, string> customParams)
        {
            var payload = new Dictionary<string, object>
        {
            { "roomName", roomName },
            { "maxPlayers", maxPlayers },
            { "isPrivate", isPrivate }
        };

            var response = await _socket.RpcAsync("rpcCreateCustomMatch", payload.ToJson());
            var deserializedResponse = response.Payload.FromJson<Dictionary<string, string>>();

            if (deserializedResponse.TryGetValue("error", out var errorMessage))
            {
                InvokeCreateRoomFailed(errorMessage);
                throw new NetworkException(errorMessage);
            }

            var matchId = deserializedResponse["matchId"];

            _roomJoinedTcs = new TaskCompletionSource<bool>();

            var match = await _socket.JoinMatchAsync(matchId);
            _currentMatchId = matchId;
            _currentMatch = match;

            var matchLabel = match.Label.FromJson<Dictionary<string, object>>();
            var seed = int.Parse(matchLabel["seed"].ToString());
            var hostID = matchLabel["host"].ToString();

            var properties = await GetUserPropertiesAsync(match.Id, match.Self.UserId);
            IsHost = true;

            _localUser = new NetworkUser(match.Self.Username, match.Self.UserId, properties, IsHost);
            _presences = new HashSet<IUserPresence>(match.Presences);
            var networkUsers = new List<NetworkUser>();

            foreach (var p in match.Presences)
            {
                payload = new Dictionary<string, object>
                {
                    { "matchId", _currentMatch.Id },
                    { "userId", p.UserId }
                };

                response = await _socket.RpcAsync("rpcGetUserProperties", payload.ToJson());
                properties = response.Payload.FromJson<Dictionary<string, string>>();

                networkUsers.Add(new NetworkUser(p.Username, p.UserId, properties, p.UserId.Equals(hostID)));
            }

            InitializeRng(seed);
            InvokeRoomJoined(new RoomData(_localUser, networkUsers, match.Presences.Count(), maxPlayers, roomName, match.Id));

            _roomJoinedTcs.SetResult(true);
        }
        public override async void JoinRoomByName(string roomName)
        {
            var payload = new Dictionary<string, string>
        {
            { "roomName", roomName }
        };

            var response = await _socket.RpcAsync("rpcFindCustomMatch", payload.ToJson());
            var deserializedResponse = response.Payload.FromJson<Dictionary<string, string>>();
            var matchId = deserializedResponse["matchId"];

            if (string.IsNullOrEmpty(matchId))
            {
                var message = "Room not found";
                InvokeJoinRoomFailed(message);
                throw new NetworkException(message);
            }

            JoinRoomByID(matchId);
        }
        public override async void JoinRoomByID(string roomID)
        {
            _roomJoinedTcs = new TaskCompletionSource<bool>();
            try
            {
                var match = await _socket.JoinMatchAsync(roomID);
                _currentMatchId = match.Id;
                _currentMatch = match;

                var matchLabel = match.Label.FromJson<Dictionary<string, object>>();
                var seed = int.Parse(matchLabel["seed"].ToString());
                var hostID = matchLabel["host"].ToString();
                var maxPlayers = int.Parse(matchLabel["maxPlayers"].ToString());
                var roomName = matchLabel["roomName"].ToString();

                var properties = await GetUserPropertiesAsync(match.Id, match.Self.UserId);
                IsHost = match.Self.UserId.Equals(hostID);
                _presences = new HashSet<IUserPresence>(match.Presences);
                _localUser = new NetworkUser(match.Self.Username, match.Self.UserId, properties, IsHost);

                var networkUsers = new List<NetworkUser>();
                foreach (var p in match.Presences)
                {
                    properties = await GetUserPropertiesAsync(match.Id, p.UserId);
                    networkUsers.Add(new NetworkUser(p.Username, p.UserId, properties, p.UserId.Equals(hostID)));
                }

                InitializeRng(seed);
                InvokeRoomJoined(new RoomData(_localUser, networkUsers, match.Presences.Count(), maxPlayers, roomName, match.Id));
                _roomJoinedTcs.SetResult(true);
            }
            catch (WebSocketException)
            {
                var message = "Could not join the room";
                InvokeJoinRoomFailed(message);
                throw new NetworkException(message);
            }
        }
        public async override void LeaveRoom()
        {
            await _socket.LeaveMatchAsync(_currentMatchId);
            _presences = new HashSet<IUserPresence>();
            InvokeRoomExited();
        }
        public override async Task<IEnumerable<RoomData>> GetRoomList()
        {
            var nRooms = 100;

            Dictionary<string, object> payload = new Dictionary<string, object>
        {
            { "limit", nRooms},
            { "authoritative", null },
            { "label", null },
            { "minSize", null },
            { "maxSize", null }
        };
            var response = await _client.RpcAsync(_session, "rpcListMatches", payload.ToJson());
            var matches = response.Payload.FromJson<List<Dictionary<string, object>>>().Select(match =>
            {
                var matchID = match["matchId"].ToString();
                var matchLabel = match["label"].ToString().FromJson<Dictionary<string, object>>();
                var currentPlayerCount = int.Parse(match["size"].ToString());
                var maxPlayerCount = int.Parse(matchLabel["maxPlayers"].ToString());
                var roomName = matchLabel["roomName"].ToString();

                return new RoomData(null, null, currentPlayerCount, maxPlayerCount, roomName, matchID);
            });

            return matches;
        }
        public async override void SendMatchState(long opCode, IDictionary<string, object> actionParams)
        {
            await _socket.SendMatchStateAsync(_currentMatchId, opCode, actionParams.ToJson());
        }

        private async void OnMatchFoundAsync(IMatchmakerMatched matchmakerMatched)
        {
            _roomJoinedTcs = new TaskCompletionSource<bool>();

            var match = await _socket.JoinMatchAsync(matchmakerMatched);
            _currentMatchId = match.Id;
            _currentMatch = match;

            _presences = new HashSet<IUserPresence>(matchmakerMatched.Users.Select(u => u.Presence));

            var matchLabel = match.Label.FromJson<Dictionary<string, object>>();
            var seed = int.Parse(matchLabel["seed"].ToString());
            var hostID = matchLabel["host"].ToString();
            var maxPlayers = int.Parse(matchLabel["maxPlayers"].ToString());
            var roomName = matchLabel["roomName"].ToString();

            IsHost = matchmakerMatched.Self.Presence.UserId.Equals(hostID);

            var properties = await GetUserPropertiesAsync(match.Id, matchmakerMatched.Self.Presence.UserId);
            _localUser = new NetworkUser(matchmakerMatched.Self.Presence.Username, matchmakerMatched.Self.Presence.UserId, properties, IsHost);

            var networkUsers = new List<NetworkUser>();
            foreach (var user in matchmakerMatched.Users)
            {
                properties = await GetUserPropertiesAsync(match.Id, user.Presence.UserId);
                networkUsers.Add(new NetworkUser(user.Presence.Username, user.Presence.UserId, properties, user.Presence.UserId.Equals(hostID)));
            }
            RoomData roomData = new RoomData(_localUser, networkUsers, matchmakerMatched.Users.Count(), maxPlayers, roomName, match.Id);

            InitializeRng(seed);
            InvokeRoomJoined(roomData);
        }
        private void OnReceivedMatchState(IMatchState matchState)
        {
            var actionParams = System.Text.Encoding.UTF8.GetString(matchState.State).FromJson<Dictionary<string, object>>();
            var actionHandler = Handlers[matchState.OpCode];
            actionHandler(actionParams);
        }
        private async void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
        {
            await _roomJoinedTcs.Task;

            var matchLabel = _currentMatch.Label.FromJson<Dictionary<string, object>>();
            var hostID = matchLabel["host"].ToString();

            foreach (var user in matchPresenceEvent.Joins)
            {
                if (_presences.Contains(user))
                {
                    continue;
                }
                _presences.Add(user);

                var properties = await GetUserPropertiesAsync(_currentMatch.Id, user.UserId);
                InvokePlayerEnteredRoom(new NetworkUser(user.Username, user.UserId, properties, user.UserId.Equals(hostID)));
            }
            foreach (var user in matchPresenceEvent.Leaves)
            {
                _presences.Remove(user);

                if (user.UserId.Equals(hostID))
                {
                    // If host left the room, terminate the game
                    InvokePlayerLeftRoom(new NetworkUser(user.Username, user.UserId, new Dictionary<string, string>(), true));
                    LeaveRoom();

                    break;
                }

                var properties = await GetUserPropertiesAsync(_currentMatch.Id, user.UserId);
                InvokePlayerLeftRoom(new NetworkUser(user.Username, user.UserId, properties, user.UserId.Equals(hostID)));
            }
        }
        private async Task<Dictionary<string, string>> GetUserPropertiesAsync(string matchId, string userId)
        {
            var payload = new Dictionary<string, object>
            {
                { "matchId", matchId },
                { "userId", userId }
            };

            var response = await _socket.RpcAsync("rpcGetUserProperties", payload.ToJson());
            return response.Payload != null ? response.Payload.FromJson<Dictionary<string, object>>().ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString()) : new Dictionary<string, string>();
        }
    }
}