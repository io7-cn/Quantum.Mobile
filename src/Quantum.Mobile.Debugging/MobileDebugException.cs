namespace Quantum.Mobile.Debugging;

public sealed class MobileDebugException : Exception
{
    public int Status { get; }
    public string Code { get; }

    public MobileDebugException(int status, string code) : base(code)
    {
        Status = status;
        Code = code;
    }
}
