using System.Diagnostics;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using db2k;
using static System.Net.Mime.MediaTypeNames;



bool htmlChanged = false;
string? outFile = null;
string baseUrl = "http://localhost:6969/";

bool gotTestRequest = false;


void HttpThread()
{
    HttpListener listener = new HttpListener();
    listener.Prefixes.Add(baseUrl);

    listener.Start();

    while (true)
    {
        var context = listener.GetContext();

        HttpListenerRequest req = context.Request;
        HttpListenerResponse resp = context.Response;

        if (req.Url != null)
        {
            string reqPath = req.Url.AbsolutePath;


            Debug.WriteLine($"req: {req.Url}");

            if (reqPath.StartsWith("/updated"))
            {
                if (htmlChanged)
                {
                    resp.StatusCode = 205;
                    htmlChanged = false;
                }
                else
                    resp.StatusCode = 204;

                resp.AddHeader("Access-Control-Allow-Origin", "*");

                resp.Close();
            }
            else if (reqPath == "/testfinished")
            {
                gotTestRequest = true;
                resp.Close();
            }
            else if (reqPath == "/index.html" || reqPath == "/")
            {
                string txt = null;

                if (File.Exists(outFile))
                {
                    try
                    {
                        txt = File.ReadAllText(outFile);
                    }
                    catch (Exception e)
                    {
                        txt = "Couldn't read outfile";
                    }
                }
                else
                {
                    txt = $"<html><body>{outFile} not built</body></html>";
                }

                byte[] data = Encoding.UTF8.GetBytes(txt);
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                resp.AddHeader("Access-Control-Allow-Origin", "localhost");

                resp.OutputStream.Write(data, 0, data.Length);
                resp.Close();
            }
        }
    }
}




void StartServer()
{
    Thread httpThread;

    httpThread = new Thread(HttpThread);

    httpThread.IsBackground = true;
    httpThread.Start();
}


int Run()
{
    bool test = false;
    bool batchTest = false;
    var argv = Environment.GetCommandLineArgs();
    string? mainFile = null;
    bool monitor = false;

    Resources.Init();

    if (argv.Length == 1)
    {
        Console.WriteLine("    ________________  ________________  _________________  _________  __________");
        Console.WriteLine("   |                \\|                \\|                 \\|         |/         /");
        Console.WriteLine("   |      |          \\      |          |____              |                   /");
        Console.WriteLine("  |      |            |                  \\ /             /                    \\");
        Console.WriteLine("  |      |            |      |            \\             /____                  \\");
        Console.WriteLine(" |                    |                    |                 |                  \\");
        Console.WriteLine("_|_______________Demo_|____________Builder_|_______________2000_____|\\___________\\_");
        Console.WriteLine("                                                                      by Factory");
        Console.WriteLine("");
        Console.WriteLine("Usage: db2k <.glsl/js file>");
        Console.WriteLine("Options:");
        Console.WriteLine("-m      : montor the input files and re-compile whenever they change");
        Console.WriteLine("-o=<.html file>      : use a specific output file, instead of just out/index.html");
        Console.WriteLine("-t        : test mode");
        return 0;
    }

    for (int i = 1; i < argv.Length; i++)
    {
        var arg = argv[i];

        if (arg.StartsWith("--o="))
        {
            outFile = arg.Substring(4);
        }
        else if (arg == "-m")
        {
            monitor = true;
        }
        else if(arg == "-t")
        {
            test = true;
        }
        else
        {
            mainFile = arg;
        }
    }


    if( test )
    {
        if( mainFile == null )
        {
            Console.WriteLine("The test directory has to be specified");
        }
        else
        {
            StartServer();
            var dirs= Directory.GetDirectories(mainFile);

            foreach( var d in dirs )
            {
                string testFile;
                bool buildOnly= false;

                // skip the resource dir, no test in it
                if (d.EndsWith("\\res"))
                    continue;

                Console.WriteLine($"{d}:");

                if (File.Exists(Path.Combine(d, "test.js")))
                    testFile = Path.Combine(d, "test.js");
                else
                    testFile = Path.Combine(d, "test.glsl");

                {
                    var tst= File.ReadAllText(testFile);

                    if( tst.StartsWith("//#test norun"))
                        buildOnly= true;
                }

                var db= new DemoBuilder(testFile,Path.Combine(d, "out\\index.html"), true);

                string res= "";
                if( !db.Build(ref res) )
                {
                    Console.WriteLine($"test {Path.GetDirectoryName(d)} failed to build.");
                    continue;
                }
                else
                {
                    if (!buildOnly)
                    {
                        outFile = db.outFile;

                        gotTestRequest = false;

                        var chromeProcess = Process.Start("c:\\program files\\google\\chrome\\application\\chrome.exe", "http://localhost:6969");

                        bool done = false;

                        while (!done)
                        {
                            if (gotTestRequest)
                                done = true;

                            Thread.Sleep(100);
                        }

                        if( !chromeProcess.HasExited )
                            chromeProcess.Kill();
                        Thread.Sleep(100);
                        outFile = null;
                    }
                }
                Console.WriteLine("pass");
            }
        }


        return 0;
    }



    if (mainFile == null)
    {
        Console.WriteLine("A glsl/js file must be specified.");
        return 1;
    }
    else
    {
        if (!File.Exists(mainFile))
        {
            Console.WriteLine($"{mainFile} doesn't exist.");
            return 1;
        }
    }

    if (outFile == null)
    {
        outFile = DemoBuilder.DeriveOutputFilename(mainFile);
    }


    if (monitor)
    {
        StartServer();

        Console.WriteLine($"Starting up http server {baseUrl}");

        string[] watchedFiles = [mainFile];
        DateTime lastCompile = DateTime.Now;

        bool done= false;
        DateTime start= DateTime.Now;

        while(!done)
        {
            var db = new DemoBuilder(mainFile, outFile, true);
            string resultsText = "";
            db.Build(ref resultsText);
            Console.WriteLine(resultsText);

            watchedFiles = db.referencedFiles.ToArray();

            bool anyChanged = false;

            while (!anyChanged && !done)
            {
                anyChanged = watchedFiles.Any(f =>
                {
                    var ft = File.GetLastWriteTime(f);
                    return ft > lastCompile;
                });

                Thread.Sleep(100);
            }

            lastCompile = DateTime.Now;
            htmlChanged = true;
        }

        return 1;
    }
    else
    {
        var db = new DemoBuilder(mainFile, outFile, false);

        string results = "";

        var res = db.Build(ref results);

        return res ? 0 : 1;
    }
}

return Run();