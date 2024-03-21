//注册命令
#load "Common/CmdReg.csx"
using System.Reflection;

class Test
{
    private static Logger Logger => _logger ??= new Logger(typeof(Test).ToString());
    private static Logger _logger;

    [CmdReg.CmdFuncEntry(alias = "Test", desc = "测试命令", example = @"dotnet-script Entry.csx Test --p4server p4d.perforce.com:1666 --p4user admin --stream stream_scrum")]
    [CmdReg.CmdFuncArg(names = ["--p", "--p4server"], type = CmdReg.EArgType.Necessary, desc = "p4服务器:端口")]
    [CmdReg.CmdFuncArg(names = ["--u", "--p4user"], type = CmdReg.EArgType.Necessary, desc = "p4用户名")]
    [CmdReg.CmdFuncArg(names = ["--s", "--stream"], type = CmdReg.EArgType.Optional, desc = "分析的Stream")]
    public static void FuncMain(string[] args)
    {
        //MethodBase.GetCurrentMethod().DeclaringType 只在非async函数中有正确返回值
        var p4server = CmdReg.GetArg(args, typeof(Test), "--p4server");
        var p4user = CmdReg.GetArg(args, typeof(Test), "--p4user");
        var stream = CmdReg.GetArg(args, typeof(Test), "--stream");
        // Logger.Log(p4server);
        // Logger.LogWarning(p4user);
        // Logger.LogError(stream);
    }
}

await CmdReg.CommandRun(Args.ToArray(), typeof(Test));