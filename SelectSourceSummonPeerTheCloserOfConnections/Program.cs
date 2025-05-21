using Desktop.DBus;
using Tmds.DBus.Protocol;

var connection = new Connection(Address.Session);
await connection.ConnectAsync().ConfigureAwait(false);

var service = new DesktopService(connection, "org.freedesktop.portal.Desktop");
var screenCast = service.CreateScreenCast("/org/freedesktop/portal/desktop");
const string SomeRandomToken = "u1";

var options = new Dictionary<string, VariantValue>
{
    ["session_handle_token"] = SomeRandomToken,
    ["handle_token"] = SomeRandomToken,
};

const bool iWantToCrash = true;
var sessionHandle = "";

if (iWantToCrash)
{
    var tcs = new TaskCompletionSource<PortalResponse>();
    await connection.AddMatchAsync(
        new MatchRule()
        {
            Type = MessageType.Signal, Member = "Response", Interface = "org.freedesktop.portal.Request",
        },
        reader: (Message message, object? o) =>
        {
            Console.WriteLine("Reading message");
            var reader = message.GetBodyReader();
            var arg0 = reader.ReadUInt32();
            var arg1 = reader.ReadDictionaryOfStringToVariantValue();
            return new PortalResponse() { RequestPath = message.PathAsString, Results = arg1 };
        },
        handler: (Exception? ex, PortalResponse? response, object? _, object? _) =>
        {
            if (ex is null)
            {
                System.Console.WriteLine($"Received response for {response.RequestPath}");
                tcs.SetResult(response);
            }
            else
            {
                System.Console.WriteLine($"!!!!ERROR!!!!! {ex.Message}");
            }
        },
        ObserverFlags.None);
    var sessPath = await screenCast.CreateSessionAsync(options);
    sessionHandle = (await tcs.Task).Results["session_handle"].ToString();
}
else
{
    var sessPath = await screenCast.CreateSessionAsync(options);
    sessionHandle = sessPath.ToString().Replace("/request/", "/session/");
}

var screenCfg = new Dictionary<string, VariantValue>();

Console.WriteLine(await screenCast.SelectSourcesAsync(sessionHandle, screenCfg));

class PortalResponse
{
    public required string RequestPath { get; init; }
    public required Dictionary<string, VariantValue> Results { get; init; }
}