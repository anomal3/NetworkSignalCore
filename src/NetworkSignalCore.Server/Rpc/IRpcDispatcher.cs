namespace NetworkSignalCore.Server.Rpc;

public interface IRpcDispatcher
{
    Task DispatchAsync(string connectionId, string controller, string method, string payloadJson);
    void RegisterController(NetworkController controller);
}
