#load "Utils.csx"
using System.Text;
using System.Text.RegularExpressions;

public class CmdReg
{
    /// <summary>
    /// 命令属性。命令函数只能有一个命令属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CmdFuncEntryAttribute : Attribute
    {
        /// <summary>
        /// 命令的名称，如不赋值会使用函数本名。
        /// </summary>
        public string alias;
        /// <summary>
        /// 命令的详细说明。
        /// </summary>
        public string desc;
        /// <summary>
        /// 命令范例。
        /// </summary>
        public string example;
    }
    /// <summary>
    /// 命令参数的属性，命令函数可包含多个命令参数属性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class CmdFuncArgAttribute : Attribute
    {
        /// <summary>
        /// 参数名。可用短名-p，但可能和以下的系统参数冲突。可用长名--path，但也会有冲突的可能。长短名都用--比较好。
        /// 参数名不能和以下dotnet-script的系统参数名相同
        /// -i|--interactive                    Execute a script and drop into the interactive mode afterwards.
        /// -c|--configuration <configuration>  Configuration to use for running the script [Release/Debug] Default is "Debug"
        /// -s|--sources <SOURCE>               Specifies a NuGet package source to use when resolving NuGet packages.
        /// -d|--debug                          Enables debug output.
        /// --verbosity                         Set the verbosity level of the command. Allowed values are t[trace], d[ebug], i[nfo], w[arning], e[rror], and c[ritical].
        /// --no-cache                          Disable caching (Restore and Dll cache)
        /// --isolated-load-context             Use isolated assembly load context
        /// --info                              Displays environmental information
        /// -?|-h|--help                        Show help information.
        /// -v|--version                        Show version information.
        /// </summary>
        public string[] names;
        /// <summary>
        /// 参数类型，表明是否为必要参数。
        /// </summary>
        public EArgType type = EArgType.Optional;
        /// <summary>
        /// 参数的详细说明
        /// </summary>
        public string desc = string.Empty;
    }
    private struct FuncInfo(string name, string alias, string desc, string example)
    {
        public string funcName = name;
        public string funcAlias = alias;
        public string funcDesc = desc;
        public string funcExample = example;
    }
    private struct ArgInfo(string[] names, EArgType type, string desc)
    {
        public string[] argNames = names;
        public EArgType argType = type;
        public string argDesc = desc;
    }
    private static Logger Logger => _logger ??= new Logger("");
    private static Logger _logger;

    const int kCmdLayoutLength = 13;
    const int kArgLayoutLength = 20;

    //todo 可以封装一个类处理是否是异步函数
    public delegate Task TaskCommandFunc(string[] args);
    public delegate void GenericCommandFunc(string[] args);

    private static readonly Dictionary<string, (FuncInfo, Type, ArgInfo[])> CommandDict = new();
    /// <summary>
    /// 命令参数类型
    /// </summary>
    public enum EArgType
    {
        /// <summary>
        /// 必要参数
        /// </summary>
        Necessary,
        /// <summary>
        /// 非必要参数
        /// </summary>
        Optional
    }

    /// <summary>
    /// 生成命令参数说明
    /// </summary>
    private static string GenerateArgsGuideContent(ArgInfo[] registedArgInfos)
    {
        var content = new StringBuilder();
        if (registedArgInfos != null && registedArgInfos.Length > 0)
        {
            content.AppendLine($"  参数:");
            foreach (var argInfo in registedArgInfos)
            {
                var argNames = argInfo.argNames;
                var argType = argInfo.argType;
                var argDesc = argInfo.argDesc;
                var argTxt = argType == EArgType.Optional ? $"[{string.Join('|', argNames)}]".PadRight(kArgLayoutLength) : $"{string.Join('|', argNames)}".PadRight(kArgLayoutLength);
                content.AppendLine($"  {argTxt}{argDesc}");
            }
        }
        else
        {
            content.AppendLine($"无参数");
        }
        return content.ToString();
    }
    /// <summary>
    /// 检查命令参数，未通过检查时输出使用说明
    /// </summary>
    public static bool CheckArgs(string invokeFuncName, string[] args)
    {
        if (!CommandDict.Keys.Select(x => x.ToLower()).Contains(invokeFuncName.ToLower()))
        {
            Logger.LogError($"没有名为<{invokeFuncName}>的命令");
            return false;
        }

        var cmdKv = CommandDict.Where(x => x.Key.ToLower().Equals(invokeFuncName.ToLower())).ToArray()[0];
        var bArgLess = false;
        var registedArgInfos = cmdKv.Value.Item3;

        //paramName paramValue模式，使用于命令脚本使用GetArg获取参数
        var necessary = registedArgInfos.Where(info => info.argType == EArgType.Necessary);
        //var necessaryCount = necessary.Where(argInfo => argInfo.argName.Split("|").Intersect(args).Any()).Count();
        //trick
        var necessaryCount = necessary.Where(argInfo => argInfo.argNames.Intersect(args.Where(arg => arg.Contains("-"))).Any()).Count();
        var needNecessaryCount = necessary.Count();
        bArgLess = necessaryCount != needNecessaryCount;

        if (bArgLess)
        {
            var funcDesc = cmdKv.Value.Item1.funcDesc;
            var content = new StringBuilder();
            if (bArgLess) Logger.LogWarning("必要参数有误，说明如下:\n");
            content.AppendLine($"命令  : {cmdKv.Key,-kCmdLayoutLength}  {funcDesc}");
            content.AppendLine(GenerateArgsGuideContent(registedArgInfos));
            var funcExample = cmdKv.Value.Item1.funcExample;
            if (!string.IsNullOrEmpty(funcExample))
            {
                content.AppendLine($"  范例: \r\n  {funcExample}");
            }
            Logger.Log(content.ToString());

            return false;
        }
        return true;
    }
    /// <summary>
    /// 输出命令的使用说明
    /// </summary>
    private static void CommandUsage(bool bFromCSX)
    {
        //cmds = cmds.Length == 0 ? CommandDict.Keys.ToArray<object>() : cmds;
        var content = new StringBuilder();
        if (bFromCSX)
        {
            content.AppendLine("使用方法: dotnet-script Entry.csx [命令] [参数]\n");
        }
        else
        {
            content.AppendLine("使用方法: Entry.exe [命令] [参数]\n");
        }
        content.AppendLine("命令不区分大小写，可用命令如下:");
        var count = CommandDict.Keys.Count;
        var idx = 0;
        foreach (var (cmd, (registedFuncInfo, _, registedArgInfos)) in CommandDict)
        {
            content.AppendLine($"命令  : {cmd,-kCmdLayoutLength}  {registedFuncInfo.funcDesc}");
            content.AppendLine(GenerateArgsGuideContent(registedArgInfos));
            var funcExample = registedFuncInfo.funcExample;
            if (!string.IsNullOrEmpty(funcExample))
            {
                content.AppendLine($"  范例: \r\n  {funcExample}");
            }
            if(idx++ < count - 1) content.AppendLine("");
        }
        Logger.Log(content.ToString());
    }
    private static bool bCollected = false;
    /// <summary>
    /// 收集注册过的命令
    /// </summary>
    private static void Collection()
    {
        if (bCollected) return;
        bCollected = true;
        var registedClasses =
            (from cls in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
             let members = cls.GetMembers().Where(member => member.GetCustomAttributes(typeof(CmdFuncEntryAttribute), true).Length > 0).ToArray()
             let func_args_arr = (from member in members
                                  let funcName = member.Name
                                  let funcAttr = member.GetCustomAttributes(typeof(CmdFuncEntryAttribute), true)[0] as CmdFuncEntryAttribute
                                  let funcAlias = string.IsNullOrEmpty(funcAttr.alias) ? funcName : funcAttr.alias
                                  let funcDesc = funcAttr.desc
                                  let funcExample = funcAttr.example
                                  let argAttrs = member.GetCustomAttributes(typeof(CmdFuncArgAttribute), true).Select(x => x as CmdFuncArgAttribute).ToArray()
                                  let argInfos = argAttrs.Select(x => new ArgInfo(x.names, x.type, x.desc)).ToArray()
                                  select new { funcInfo = new FuncInfo(funcName, funcAlias, funcDesc, funcExample), argInfos }).ToArray()
             where cls.GetMembers().Where(member => member.GetCustomAttributes(typeof(CmdFuncEntryAttribute), true).Length > 0).Count() > 0
             select new { classType = cls, func_args_arr }).ToArray();
        foreach (var registedClass in registedClasses)
        {
            foreach (var func_args in registedClass.func_args_arr)
            {
                CommandDict[func_args.funcInfo.funcAlias] = (func_args.funcInfo, registedClass.classType, func_args.argInfos);
            }
        }
    }

    /// <summary>
    /// 兼容paramName paramValue模式
    /// 智能启动函数，可在统一入口Entry中调用，也可在功能文件中直接调用
    /// </summary>
    public static async Task CommandRun(string[] args, Type t)
    {
        var registedFuncName = (System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(type => type == t).First().GetMembers().Where(member => member.GetCustomAttributes(typeof(CmdFuncEntryAttribute), true).Length > 0).First().GetCustomAttributes(typeof(CmdFuncEntryAttribute), true).First() as CmdFuncEntryAttribute).alias;
        await CommandRun(args, registedFuncName);
    }
    /// <summary>
    /// 智能启动函数，可在统一入口Entry中调用，也可在功能文件中直接调用
    /// </summary>
    public static async Task CommandRun(string[] args, string registedFuncName = "")
    {
        Collection();
        //from csx      :System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.Name==script.dll          Location==C:\Users\xindong\AppData\Local\Temp\dotnet-script\E\github\CSXToolBox\Entry.csx\execution-cache\script.dll
        //from Entry.exe:System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.Name==scriptAssembly.dll  Location==string.Empty
        var bFromCSX = System.Reflection.Assembly.GetExecutingAssembly().ManifestModule.Name.Equals("script.dll");
        var groups = Regex.Match(System.Reflection.Assembly.GetExecutingAssembly().Location, @"^.*\\(.*)\.csx\\execution-cache\\script.dll").Groups;
        var entryFileName = groups.Count > 1 ? groups[1].Value : string.Empty;
        //从Entry.csx进入的话算registedFuncName==string.Empty
        entryFileName = entryFileName.Equals("Entry") ? string.Empty : entryFileName;
        if (bFromCSX ? entryFileName.Equals(registedFuncName) : string.IsNullOrEmpty(registedFuncName))
        {
            var bEntry = false;
            var bValidArgs = false;
            var invokeFuncName = string.Empty;
            TaskCommandFunc taskFuncMain = null;
            GenericCommandFunc genericFuncMain = null;
            string[] funcArgs = null;
            var bSpecifyParamNameMode = args.Where(arg => arg.Contains("-")).Count() > 0;
            //从Entry.exe进入
            if (string.IsNullOrEmpty(registedFuncName))
            {
                if (args.Length < 1)
                {
                    CommandUsage(bFromCSX);
                    return;
                }
                else if (CommandDict.Where(x => x.Key.ToLower().Equals(args[0].ToLower())).Count() <= 0)
                {
                    Logger.LogError($"找不到名为<{args[0]}>的命令");
                    CommandUsage(bFromCSX);
                    return;
                }

                if (bValidArgs = CheckArgs(args[0], args[1..args.Length]))
                {
                    bEntry = true;
                    invokeFuncName = args[0];
                    var cmdInfo = CommandDict.Where(x => x.Key.ToLower().Equals(invokeFuncName.ToLower())).ToArray()[0].Value;
                    var funcInfo = cmdInfo.Item1;
                    var ins = Activator.CreateInstance(cmdInfo.Item2);
                    var method = cmdInfo.Item2.GetMethod(funcInfo.funcName);
                    if (method.ReturnType == typeof(Task) || method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        taskFuncMain = (string[] args) => (Task)(method?.Invoke(ins, new object[] { args }));
                    }
                    else
                    {
                        genericFuncMain = (string[] args) => method?.Invoke(ins, new object[] { args });
                    }

                    funcArgs = args[1..args.Length].ToArray();
                }
            }
            else if (bValidArgs = CheckArgs(registedFuncName, args.ToArray()))
            {
                invokeFuncName = registedFuncName;
                var cmdInfo = CommandDict.Where(x => x.Key.ToLower().Equals(invokeFuncName.ToLower())).ToArray()[0].Value;
                var ins = Activator.CreateInstance(cmdInfo.Item2);
                var funcInfo = cmdInfo.Item1;
                var method = cmdInfo.Item2.GetMethod(funcInfo.funcName);
                if (method.ReturnType == typeof(Task) || method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    taskFuncMain = (string[] args) => (Task)(method?.Invoke(ins, new object[] { args }));
                }
                else
                {
                    genericFuncMain = (string[] args) => method?.Invoke(ins, new object[] { args });
                }

                funcArgs = args.ToArray();
            }
            var pwd = Directory.GetCurrentDirectory();
            try
            {
                if (bValidArgs)
                {
                    if (bSpecifyParamNameMode)
                    {
                        Logger.Log($"Invoke func{(bEntry ? " from entry" : "")}: {invokeFuncName}({(funcArgs == null ? "" : string.Join(", ", Enumerable.Range(0, funcArgs.Length / 2).Select(i => $"{funcArgs[i * 2]}:{funcArgs[i * 2 + 1]}")))})");
                    }
                    else
                    {
                        Logger.Log($"Invoke func{(bEntry ? " from entry" : "")}: {invokeFuncName}({(funcArgs == null ? "" : string.Join(", ", funcArgs.ToArray()))})");
                    }

                    if (taskFuncMain != null)
                    {
                        await taskFuncMain?.Invoke(funcArgs);
                    }
                    else if (genericFuncMain != null)
                    {
                        genericFuncMain?.Invoke(funcArgs);
                    }
                }
            }
            finally
            {
                Directory.SetCurrentDirectory(pwd);
            }
        }
    }

    /// <summary>
    /// 获取参数，Necessary类型参数不用填defaultValue，填了也用不到。Optional参数要填默认值。
    /// </summary>
    public static string GetArg(string[] args, Type t, string argName, string defaultValue = "")
    {
        var type = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().Where(type => type == t).First() ?? throw new Exception($"没有这个类型<{t}>");
        var member = type.GetMembers().Where(member => member.GetCustomAttributes(typeof(CmdFuncEntryAttribute), true).Length > 0).First() ?? throw new Exception($"<{t}>没有入口函数");
        var attrs = member.GetCustomAttributes(typeof(CmdFuncArgAttribute), true).Where(argAttr => ((CmdFuncArgAttribute)argAttr).names.Contains(argName));
        var attr = attrs.Any() ? attrs.First() as CmdFuncArgAttribute : throw new Exception($"尝试获取参数<{argName}>的值，但命令<{t}>中未声明过。");
        if (args.Length > 0 && args.Where(arg => arg.Contains("-")).Count() == 0)
        {
            throw new Exception($"该命令使用paramName paramValue传参模式");
        }

        //var idx = Array.FindIndex(args, arg => argName.Split("|").Contains(arg));
        var idx = Array.FindIndex(args, arg => attr.names.Contains(arg));
        if (idx < 0)
        {
            if (attr.type == EArgType.Necessary)
            {
                //理论上走不到这里，会在checkargs中被拦住
                throw new Exception($"缺少必要参数<{argName}>");
            }
            else
            {
                return defaultValue;
            }
        }
        if (idx == args.Length - 1)
        {
            throw new Exception($"参数<{argName}>缺少值");
        }
        return args[idx + 1];
    }
}