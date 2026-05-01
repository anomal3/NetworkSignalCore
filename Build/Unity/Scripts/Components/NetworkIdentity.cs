// Assigns a unique network ID to a GameObject.
// Required for SyncVar synchronization.
using UnityEngine;

namespace NetworkSignalCore.Unity.Components
{
    public sealed class NetworkIdentity : MonoBehaviour
    {
        [SerializeField] private string networkId = string.Empty;

        public string NetworkId
        {
            get
            {
                if (string.IsNullOrEmpty(networkId))
                    networkId = System.Guid.NewGuid().ToString("N");
                return networkId;
            }
        }

        private void Reset()
        {
            networkId = System.Guid.NewGuid().ToString("N");
        }
    }
}
