//注册命令
#load "Common/CmdReg.csx"
#load "UnityProj/Assets/Editor/ExportContentData.cs"//支持相对路径载入 .cs 文件，并通过宏兼容csx语法
using System.Reflection;

class ExportDataFromExcel
{
    private static Logger Logger => _logger ??= new Logger(typeof(ExportDataFromExcel).ToString());
    private static Logger _logger;

    [CmdReg.CmdFuncEntry(alias = "ExportDataFromExcel", desc = "模拟共用Unity项目代码Excel导出数据")]
    [CmdReg.CmdFuncArg(names = ["--p", "--path"], type = CmdReg.EArgType.Necessary, desc = "excel目录或文件路径")]
    public static void FuncMain(string[] args)
    {
        var path = CmdReg.GetArg(args, typeof(ExportDataFromExcel), "--path");
        ExportContentData.Export(Path.GetFullPath(path));
    }
}

await CmdReg.CommandRun(Args.ToArray(), typeof(ExportDataFromExcel));