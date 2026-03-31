
namespace Assets.Scripts.Game.SimpleScript.Scripting
{
  public class SystemFunctionReturnData
  {
    public string Data;
    public int TickCooldown;

    public SystemFunctionReturnData()
    {
      TickCooldown = 1;
    }

    public static SystemFunctionReturnData Success(int tickCooldown = 1)
    {
      return new SystemFunctionReturnData() { TickCooldown = tickCooldown };
    }
    public static SystemFunctionReturnData Success(string data, int tickCooldown = 1)
    {
      return new SystemFunctionReturnData() { Data = data, TickCooldown = tickCooldown };
    }

    // Errors
    public static SystemFunctionReturnData Error(string errorCode)
    {
      return new SystemFunctionReturnData() { Data = $"!E{errorCode}" };
    }
    public static SystemFunctionReturnData Error(string errorCode, params object[] args)
    {
      return new SystemFunctionReturnData() { Data = $"!E{errorCode} {string.Join(" ", args)}" };
    }

    public static SystemFunctionReturnData InvalidFunction()
    {
      return Error("1000");
    }
    public static SystemFunctionReturnData InvalidParameters(int expected)
    {
      return Error("1001", expected);
    }
    public static SystemFunctionReturnData NullReference()
    {
      return Error("1002");
    }
    public static SystemFunctionReturnData InvalidParameterType(int parameterIndex, string expectedType)
    {
      return Error("1003", parameterIndex, expectedType);
    }
    public static SystemFunctionReturnData NotImplemented()
    {
      return Error("9999");
    }
    public static SystemFunctionReturnData Custom(string customError)
    {
      return Error("9000", customError);
    }
  }
}