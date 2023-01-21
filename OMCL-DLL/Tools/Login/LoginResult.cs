namespace OMCL_DLL.Tools.Login.Result
{
    public class LoginResult
    {
        public string refresh_token { get; internal set; }
        public string access_token { get; internal set; }
        public string uuid { get; internal set; }
        public string name { get; internal set; }
    }
}