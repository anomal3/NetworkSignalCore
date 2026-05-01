// Internal adapter — bridges Unity's MonoBehaviour lifecycle with NetworkBehaviourBase.
using NetworkSignalCore.Client;
using NetworkSignalCore.Client.Rpc;
using NetworkSignalCore.Core.Attributes;

namespace NetworkSignalCore.Unity
{
    internal sealed class UnityNetworkBehaviourAdapter : NetworkBehaviourBase
    {
        private readonly NetworkBehaviour _mono;

        public UnityNetworkBehaviourAdapter(
            NetworkClient client,
            string controllerName,
            NetworkBehaviour mono)
            : base(client, controllerName)
        {
            _mono = mono;
        }

        // Forward ClientRpc dispatch to the MonoBehaviour via reflection.
        // NetworkBehaviourBase already handles the dispatch — this override
        // routes it to the concrete MonoBehaviour class that may define [ClientRpc] methods.
        protected override void DispatchToTarget(string method, object?[] args)
        {
            var type = _mono.GetType();
            foreach (var m in type.GetMethods(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public   |
                System.Reflection.BindingFlags.NonPublic))
            {
                var attr = m.GetCustomAttributes(typeof(ClientRpcAttribute), false);
                if (attr.Length == 0) continue;

                var rpcAttr = (ClientRpcAttribute)attr[0];
                var name = rpcAttr.MethodName ?? m.Name;
                if (name != method) continue;

                m.Invoke(_mono, args);
                return;
            }
        }
    }
}
