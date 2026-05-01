// Unity-side base class for networked game objects.
// Inherit from this instead of MonoBehaviour for objects that need networking.
using System.Threading.Tasks;
using NetworkSignalCore.Client.Rpc;
using UnityEngine;

namespace NetworkSignalCore.Unity
{
    public abstract class NetworkBehaviour : MonoBehaviour
    {
        private NetworkBehaviourBase? _base;

        protected NetworkClient Client => NetworkManager.Instance.Client;

        protected virtual string ControllerName => GetType().Name;

        protected virtual void OnNetworkStart() { }
        protected virtual void OnNetworkStop()  { }

        protected virtual void Start()
        {
            if (NetworkManager.Instance == null)
            {
                Debug.LogWarning($"[NetworkBehaviour] No NetworkManager found for {gameObject.name}");
                return;
            }
            _base = CreateNetworkBase();
            OnNetworkStart();
        }

        protected virtual void OnDestroy()
        {
            _base = null;
            OnNetworkStop();
        }

        // Override to provide custom NetworkBehaviourBase (for DI).
        // Default implementation creates a simple adapter.
        protected virtual NetworkBehaviourBase CreateNetworkBase()
            => new UnityNetworkBehaviourAdapter(Client, ControllerName, this);

        protected Task ServerRpcAsync(string method, object? payload = null)
            => Client.InvokeServerRpcAsync(ControllerName, method, payload);
    }
}
