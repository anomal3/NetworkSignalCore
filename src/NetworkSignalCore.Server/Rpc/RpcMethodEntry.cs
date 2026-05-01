using System.Reflection;
using NetworkSignalCore.Core.Attributes;

namespace NetworkSignalCore.Server.Rpc;

internal sealed class RpcMethodEntry
{
    public NetworkController Controller { get; init; } = null!;
    public MethodInfo         Method    { get; init; } = null!;
    public ServerRpcAttribute Attribute { get; init; } = null!;
    public ParameterInfo[]    Parameters { get; init; } = null!;
    public ValidatedAttribute[] Validators { get; init; } = null!;
}
