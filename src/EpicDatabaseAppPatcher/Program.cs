using dnlib.DotNet;
using dnlib.DotNet.Emit;
using EpicDatabaseAppPatcher.Utils;

if (args.Length < 2 || args.Length > 3) {
    Console.WriteLine($"Usage: {Utils.GetExecutableFileName()} <input file> <output file> [email]");
    Console.WriteLine("  [email] - Optional: Email address to patch into GetEmail method");
    return -1;
}

var inPath = args[0];
var outPath = args[1];
var patchEmail = args.Length == 3 ? args[2] : null;

if (!File.Exists(inPath))
{
    Console.WriteLine($"FATAL: Input file not found: {inPath}");
    return -1;
}

Console.WriteLine("EpicDatabaseAppPatcher - SFINXV");
Console.WriteLine("Don't use this tool for commercial purposes!");
Console.WriteLine("GitHub: https://github.com/SFINXVC/EpicDatabaseAppPatcher");

try
{
    var dir = Path.GetDirectoryName(inPath);

    if (string.IsNullOrEmpty(dir))
    {
        dir = Directory.GetCurrentDirectory();
    }

    var fileName = Path.GetFileNameWithoutExtension(inPath);
    var extension = Path.GetExtension(inPath);
    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
    var backupName = Path.Combine(dir, $"{fileName}.backup_{timestamp}{extension}");

    File.Copy(inPath, backupName, false);
    Console.WriteLine($"Created backup at: {backupName}");
}
catch (Exception e)
{
    Console.WriteLine($"FATAL: An error ocurred while trying to create a backup: {e.Message}");
    return -1;
}

ModuleDefMD? moduleDef;
try {
    moduleDef = ModuleDefMD.Load(inPath);
} catch (Exception e) {
    Console.WriteLine($"FATAL: An error ocurred while trying to load the module: {e.Message}");
    return -1;
}

var type = moduleDef.Types.FirstOrDefault(x => x.FullName == "TablePlus.Source.Service.LicenseService");
type ??= moduleDef.Types.FirstOrDefault(x => x.FullName.EndsWith("LicenseService"));

if (type is null) {
    Console.WriteLine("FATAL: LicenseService type not found!");
    return -1;
}

var method = type.Methods.FirstOrDefault(x => x.Name == "IsLicensed");
method ??= type.Methods.FirstOrDefault(x => x.Name == "LicenseService__IsLicensed");
method ??= type.Methods.FirstOrDefault(x => !x.IsStatic && x.Parameters.Count == 1 && x.ReturnType.FullName == "System.Boolean");

if (method is null) {
    Console.WriteLine("FATAL: Targeted method (IsLicensed) at LicenseService is not found!");
    return -1;
}

Console.WriteLine($"Trying to patch method: {type.FullName}::{method.Name}");

var newMethodBody = new CilBody();
newMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); // always return true
newMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ret));

method.Body = newMethodBody;

if (!string.IsNullOrEmpty(patchEmail)) {
    var getEmailMethod = type.Methods.FirstOrDefault(x => x.Name == "GetEmail");
    getEmailMethod ??= type.Methods.FirstOrDefault(x => x.Name == "LicenseService__GetEmail");
    getEmailMethod ??= type.Methods.FirstOrDefault(x => !x.IsStatic && x.Parameters.Count == 0 && x.ReturnType.FullName == "System.String");

    if (getEmailMethod is null) {
        Console.WriteLine("WARNING: GetEmail method not found, skipping email patch");
    } else {
        Console.WriteLine($"Trying to patch method: {type.FullName}::{getEmailMethod.Name}");
        
        var emailMethodBody = new CilBody();
        emailMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldstr, patchEmail));
        emailMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ret));
        
        getEmailMethod.Body = emailMethodBody;
    }
}

try {
    moduleDef.Write(outPath);
    Console.WriteLine($"Successfully wrote patched module to: {outPath}");
} catch (Exception e) {
    Console.WriteLine($"FATAL: An error ocurred while trying to write the module: {e.Message}");
    return -1;
}

return 0;