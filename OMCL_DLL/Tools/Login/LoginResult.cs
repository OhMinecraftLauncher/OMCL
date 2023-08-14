namespace OMCL_DLL.Tools.Login.Result
{
    public class MicrosoftLoginResult
    {
        public string refresh_token { get; internal set; }
        public string access_token { get; internal set; }
        public string uuid { get; internal set; }
        public string name { get; internal set; }
        public bool IsNew { get; internal set; }
    }
    public class MojangLoginResult
    {
        public string access_token { get; internal set; }
        public string uuid { get; internal set; }
        public string name { get; internal set; }
    }
    public class UnifiedPassLoginResult
    {
        public string serverid { get; internal set; }
        public string client_token { get; internal set; }
        public string access_token { get; internal set; }
        public string uuid { get; internal set; }
        public string name { get; internal set; }
    }
    public class AuthlibInjectorLoginResult
    {
        public string server { get; internal set; }
        public string access_token { get; internal set; }
        public string uuid { get; internal set; }
        public string name { get; internal set; }
        public AuthlibInjectorUser[] users { get; internal set; }
    }
    public class AuthlibInjectorUser
    {
        public string uuid { get; internal set; }
        public string name { get; internal set; }
    }
    public class OfflineLoginResult
    {
        public string name { get; internal set; }
        public string uuid { get; internal set; }
    }
}