using System.Reflection;

var asm = Assembly.Load("EF.Data");
foreach (var t in asm.GetExportedTypes().OrderBy(t => t.FullName))
{
    if (t.FullName!.Contains("Exception") || t.FullName!.Contains("Processor"))
        Console.WriteLine(t.FullName);
}
