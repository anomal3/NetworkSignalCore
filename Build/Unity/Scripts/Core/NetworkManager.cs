// Drop this file into your Unity project Assets folder.
// Add NetworkSignalCore.Core.dll and NetworkSignalCore.Client.dll to Assets/Plugins.
using System;
using System.Threading;
using System.Threading.Tasks;
using NetworkSignalCore.Client;
using NetworkSignalCore.Core.Messages;
using NetworkSignalCore.Core.Models;
using UnityEngine;

namespace NetworkSignalCore.Unity
{
    public sealed class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; } = null!;

        [Header("Connection")]
        [SerializeField] private string serverUrl = "http://localhost:5000/network";
        [SerializeField] private bool autoConnect = false;

        public NetworkClient Client { get; private set; } = null!;

        public bool IsConnected     => Client?.IsConnected     ?? false;
        public bool IsAuthenticated => Client?.IsAuthenticated ?? false;
        public PlayerInfo? LocalPlayer => Client?.LocalPlayer;

        public event Action?             OnConnected;
        public event Action<Exception?>? OnDisconnected;

        private CancellationTokenSource _cts = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Client = new NetworkClient(opt =>
            {
                opt.ServerUrl = serverUrl;
            });

            Client.OnConnected    += () => OnConnected?.Invoke();
            Client.OnDisconnected += ex => OnDisconnected?.Invoke(ex);
        }

        private void Start()
        {
            if (autoConnect)
                _ = ConnectAsync();
        }

        public Task ConnectAsync() => Client.ConnectAsync(_cts.Token);

        public Task<bool> AuthenticateAsync(string token)
            => Client.AuthenticateAsync(token);

        private void OnDestroy()
        {
            _cts.Cancel();
            Client?.Dispose();
        }
    }
}
