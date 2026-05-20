

List<string> lns= [
    "namespace db2k;",
    "",
    "class Resources {",
];

Console.WriteLine("Starting");

List<(string file,string name)> inits = [];

foreach( var f in Directory.GetFiles(".") )
{
    var fn= Path.GetFileName(f);

    if( fn.EndsWith(".cs") || fn.EndsWith(".bat"))
        continue;


    Console.WriteLine($"Processing {fn}");
    var name= fn.Replace(".","_");

    inits.Add((fn,name));

    lns.Add( $"\tpublic static string[] {name}= [");
    var flns= File.ReadAllLines(f);
    lns.AddRange(flns.Select( ln =>
    {
        var bln= ln.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"\t\t\"{bln}\","; 
    } ));
    lns.Add("\t];");
    lns.Add("");

}

string resDir = "..\\\\..\\\\..\\\\res";

lns.Add("\tpublic static void Init()");
lns.Add("\t{");
lns.Add($"\t\tstring resDir= Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), \"{resDir}\");");
lns.Add($"\t\tif(Directory.Exists(resDir))");
lns.Add("\t\t{");
foreach ( var inn in inits )
{
    lns.Add($"\t\t\t{inn.name} = File.ReadAllLines(Path.Combine(resDir,\"{inn.file}\"));");
}
lns.Add("\t\t}");
lns.Add("\t}");

lns.Add("}");

File.WriteAllLines("Resources.cs", lns);

Console.WriteLine("Finished");
