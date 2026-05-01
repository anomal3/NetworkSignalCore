namespace NetworkSignalCore.Client.Sync;

public interface ISyncStateReceiver
{
    void OnSyncStateReceived(string field, string valueJson);
}
