//注册命令
#load "Common/CmdReg.csx"
using System.Reflection;

class ExportDataFromExcel
{
    private static Logger Logger => _logger ??= new Logger(typeof(ExportDataFromExcel).ToString());
    private static Logger _logger;

    [CmdReg.CmdFuncEntry(alias = "ExportDataFromExcel", desc = "从Excel导出数据")]
    [CmdReg.CmdFuncArg(names = ["--p", "--path"], type = CmdReg.EArgType.Necessary, desc = "excel目录或文件路径")]
    public static void FuncMain(string[] args)
    {
        var path = CmdReg.GetArg(args, typeof(ExportDataFromExcel), "--path");
        //使用库读取Excel
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        {
            //await Task.Delay(10000);
            Logger.Log($"readed {path}");
            stream.Close();
        }
    }
}

await CmdReg.CommandRun(Args.ToArray(), typeof(ExportDataFromExcel));