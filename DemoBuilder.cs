namespace db2k;

using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using static db2k.Shader;


public class DemoBuilder
{
    public string mainFile;
    public string outFile;
    List<string> jsExtraLines;
    string[] splashHtmlLines;

    const string defaultSplashHtml = "<p style='text-align: center;'>Click to start</p>";


    public HashSet<string> referencedFiles;


    List<Shader> shaders = [];


    Regex textureRegex;
    Regex javascriptRegex;
    Regex includeRegex;
    Regex ifdefRegex;
    Regex audioRegex;
    Regex splashRegex;
    Regex optionRegex;
    Regex uniformRegex;
    Regex shaderRegex;


    Regex commandStartRegex;

    string rootDir;

    string audioFilename;
    byte[] audioFileData;

    bool startPause;
    bool monitor;

    bool glslMain;

    public DemoBuilder(string mainFilename, string outFilename, bool _monitor)
    {
        audioFilename = "";
        mainFile = mainFilename;
        outFile = outFilename;
        referencedFiles = [];
        startPause = false;
        monitor = _monitor;
        rootDir = Path.GetDirectoryName(mainFile) ?? ".\\";

        commandStartRegex  = new Regex("^//#(.+) ");

        textureRegex = new Regex("^//#texture \\\"(.+)\" (.+)$");
        javascriptRegex = new Regex("^//#javascript \\\"(.+)\"$");
        includeRegex = new Regex("^//#include \\\"(.+)\"$");
        audioRegex = new Regex("^//#audio \\\"(.+)\"$");
        splashRegex = new Regex("^//#splash \\\"(.+)\"$");
        ifdefRegex = new Regex("^//#ifdef (.+)$");
        optionRegex = new Regex("^//#option ([^ ]+) ([^ ]+)$");
        uniformRegex = new Regex("^//#uniform ([^ ]+) ([^ ]+)$");
        shaderRegex = new Regex("^//#shader \\\"([^ ]+)\" ([^ ]+)$");

        jsExtraLines = [];
        splashHtmlLines = [defaultSplashHtml];
    }

    public static string DeriveOutputFilename(string mainFile)
    {
        string baseDir = Path.GetDirectoryName(mainFile) ?? ".\\";

        baseDir = Path.Combine(baseDir, "out");

        Directory.CreateDirectory(baseDir);

        return Path.Combine(baseDir, "index.html");
    }

    static string Time()
    {
        return DateTime.Now.ToString("HH:mm:ss");
    }


    public void AddReferencedFile(string filename)
    {
        string baseDir = Path.GetDirectoryName(mainFile) ?? ".\\";

        referencedFiles.Add(Path.Combine(baseDir, filename));
    }

    byte[] ReadBinaryFile( string filename, string sourceFile, int sourceLine )
    {
        if( !File.Exists(filename))
            throw new Exception( $"File {filename} doesn't exist" );

        AddReferencedFile(filename);
        return File.ReadAllBytes(filename);
    }

    string[] ReadTextFile( string filename, string sourceFile, int sourceLine )
    {
        if( !File.Exists(filename))
            throw new Exception( $"Error: {sourceFile}:{sourceLine}: File '{filename}' doesn't exist" );

        AddReferencedFile(filename);

        return File.ReadAllLines(filename);
    }

    void IncludeGlsl( Shader shader, string filename, bool mainFile, string sourceFile, int sourceLine)
    {
        if (shader.includedGlslFiles.Contains(filename))
            return;

        shader.includedGlslFiles.Add(filename);

        var lns = ReadTextFile(filename, sourceFile, sourceLine);


        for (int k = 0; k < lns.Length; k++)
        {
            var gLine = lns[k];
            int lineNum= k;
            string errorPreamble= $"{filename}:{lineNum+1}:";
    
            var startMatch= commandStartRegex.Match(gLine);

            if( startMatch.Success )
            {
                var textureMatch = textureRegex.Match(gLine);
                var jsMatch = javascriptRegex.Match(gLine);
                var includeMatch = includeRegex.Match(gLine);
                var audioMatch = audioRegex.Match(gLine);
                var optionMatch = optionRegex.Match(gLine);
                var splashMatch = splashRegex.Match(gLine);
                var uniformMatch = uniformRegex.Match(gLine);

                if (optionMatch.Success)
                {
                    var optionName = optionMatch.Groups[1].Value;
                    var optionValue = optionMatch.Groups[2].Value;

                    if( optionName == "startpause" )
                    {
                        if( optionValue != "true" && optionValue != "false" )
                            throw new Exception( $"{errorPreamble} unknown value for startpause '{optionValue}'" );

                        startPause = optionValue == "true";
                    }
                    else
                    {
                        throw new Exception( $"{filename}:{lineNum+1}: unknown option '{optionName}'" );
                    }
                }
                else if( uniformMatch.Success)
                {
                    var arrayRegex = new Regex("(float|int|uint|vec3)\\[([0-9]+)\\]");
                    var type = uniformMatch.Groups[1].Value;
                    var name = uniformMatch.Groups[2].Value;


                    var arrayMatch = arrayRegex.Match(type);
                    var glType = "";

                    if (arrayMatch.Success)
                    {
                        switch (arrayMatch.Groups[1].Value)
                        {
                            case "float":
                                glType = "1fv";
                                break;
                            case "int":
                                glType = "1iv";
                                break;
                            case "uint":
                                glType = "1uiv";
                                break;
                            case "vec2":
                                glType = "2fv";
                                break;
                            case "vec3":
                                glType = "3fv";
                                break;
                        }
                    }
                    else if (type == "float")
                        glType = "1f";
                    else if (type == "int")
                        glType = "1i";
                    else if (type == "uint")
                        glType = "1ui";
                    else if (type == "vec2")
                        glType = "2f";
                    else if (type == "vec3")
                        glType = "3f";
                    else
                    {
                        throw new Exception($"{errorPreamble} Unknown type '{type}'");
                    }

                    shader.uniforms.Add( new() { name = name, glType = glType, glslType = type });
                    shader.glslLines.Insert(0, new() { text = gLine.Substring(3) + ";", srcLineNumber = 0, srcFilename = "generated" });

                }
                else if (textureMatch.Success)
                {
                    var fn = textureMatch.Groups[1].Value;
                    var varName = textureMatch.Groups[2].Value;

                    var path = Path.Combine(rootDir, fn);

                    if (path.EndsWith(".png") || path.EndsWith(".jpg"))
                    {
                        shader.textures.Add( new () { name= varName, data = ReadBinaryFile(path, filename, lineNum ) });

                        shader.glslLines.Insert(0, new () { text = $"uniform sampler2D {varName};", srcLineNumber = 0, srcFilename = "generated" });
                    }
                }
                else if (jsMatch.Success && mainFile)
                {
                    var fn = jsMatch.Groups[1].Value;

                    var path = Path.Combine(rootDir, fn);

                    jsExtraLines.AddRange(ReadTextFile(path, filename, lineNum));
                }
                else if (includeMatch.Success)
                {
                    var fn = includeMatch.Groups[1].Value;

                    var path = Path.Combine(rootDir, fn);

                    IncludeGlsl(shader, path, false, filename, lineNum);
                }
                else if (audioMatch.Success)
                {
                    var fn = audioMatch.Groups[1].Value;

                    audioFilename = Path.Combine(rootDir, fn);
                    audioFileData = ReadBinaryFile(audioFilename, filename, lineNum);
                    startPause = true;
                }
                else if (splashMatch.Success)
                {
                    var fn = splashMatch.Groups[1].Value;
                    splashHtmlLines= ReadTextFile(Path.Combine(rootDir, fn), filename, lineNum);
                    startPause=true;
                }
                else if( gLine.StartsWith("//#test"))
                {
                    // test specific, ignore
                }
                else
                {
                    throw new Exception($"{filename}:{lineNum+1}: Line in wrong format for a command: '{gLine}'");
                }
            }
            else
                shader.glslLines.Add(new Shader.GlslLine() { text = gLine, srcFilename = filename, srcLineNumber = k + 1 });
        }
    }


    void ProcessJavascriptMain(string mainFile)
    {
        var lns = ReadTextFile(mainFile, "main", 69);

        for (int k = 0; k < lns.Length; k++)
        {
            var gLine = lns[k];
            int lineNum = k;
            string errorPreamble = $"{mainFile}:{lineNum + 1}:";

            var startMatch = commandStartRegex.Match(gLine);

            if (startMatch.Success)
            {
                var shaderMatch = shaderRegex.Match(gLine);

                if (shaderMatch.Success)
                {
                    string glslFile = shaderMatch.Groups[1].Value;

                    var path = Path.Combine(rootDir, glslFile);

                    var sh = new Shader();

                    sh.name = shaderMatch.Groups[2].Value;

                    IncludeGlsl(sh, path, false, mainFile, lineNum);

                    shaders.Add(sh);
                }
                else
                    throw new Exception($"{mainFile}:{lineNum + 1}: Line in wrong format for a command: '{gLine}'");
            }
            else
                jsExtraLines.Add(gLine);
        }
    }

    public bool Build( ref string results )
    {
        try
        {
            HashSet<string> options = [];
            DateTime start = DateTime.Now;


            if (!File.Exists(mainFile))
            {
                results= $"mainFile ({mainFile}) doesn't exist";
                return false;
            }

            AddReferencedFile(mainFile);


            if (rootDir == "")
                rootDir = ".\\";

            if (File.Exists(outFile))
                File.Delete(outFile);

            glslMain = mainFile.EndsWith(".glsl");

            var sw = new StreamWriter(File.Create(outFile));

            string[] htmlLines = Resources.index_html.ToArray();
            string[] jsLines = Resources.main_js.ToArray();
            List<string> uniformLines = [];


            if ( glslMain )
            {
                var sh= new Shader();
                sh.name = "shady";

                jsExtraLines.Add("function update(frame, fts) {");
                jsExtraLines.Add(" curShader= shady;");
                jsExtraLines.Add("}");

                IncludeGlsl(sh, mainFile, true, mainFile, 1);
                shaders.Add(sh);
            }
            else
            {
                ProcessJavascriptMain(mainFile);
                options.Add("USEJAVASCRIPT");
            }

            if (startPause) options.Add("USERWAIT");
            if (monitor) options.Add("MONITOR");
            // if (uniforms.Count > 0) options.Add("USEUNIFORMS");

            
            List<string> shaderLines = [];

            foreach (var shader in shaders)
            {

                int glLineNumber = 8;
                foreach (var gl in shader.glslLines)
                {
                    string uFilename = Path.GetRelativePath(rootDir, gl.srcFilename).Replace('\\', '/');

                    if (shader.sourceSections.Count == 0)
                    {
                        shader.sourceSections.Add(new SourceSection() { filename = uFilename, glslStart = glLineNumber, glslEnd = glLineNumber - 1, srcLineNumber = gl.srcLineNumber });
                    }

                    SourceSection ss = shader.sourceSections.Last();

                    if (ss.filename == uFilename)
                    {
                        ss.glslEnd++;
                    }
                    else
                    {
                        shader.sourceSections.Add(new SourceSection() { filename = uFilename, glslStart = glLineNumber, glslEnd = glLineNumber, srcLineNumber = gl.srcLineNumber });
                    }

                    glLineNumber++;
                }
            }


            jsExtraLines.Add($"function initShadersInternal()");
            jsExtraLines.Add("{");
            foreach (var shader in shaders)
            {
                jsExtraLines.Add( $"\tinitShader({shader.name});" );
                foreach (var u in shader.uniforms)
                    uniformLines.Add($"uniform {u.glslType} {u.name};");
            }
            jsExtraLines.Add("};");
            jsExtraLines.Add("");

            for (int i = 0; i < htmlLines.Length; i++)
            {
                var ln = htmlLines[i];

                if (ln == "//#insertjspoint")
                {
                    bool inIf = false;
                    bool usingIf = false;
                    for (int j = 0; j < jsLines.Length; j++)
                    {
                        ln = jsLines[j];

                        var ifdefMatch = ifdefRegex.Match(ln);

                        if (ln == "//#insertsplashpoint")
                        {
                            sw.WriteLine("var splashHtml= ");
                            foreach (var ln2 in splashHtmlLines)
                                sw.WriteLine($"`{ln2}` + ");
                            sw.WriteLine("'';");
                        }
                        else if (ln == "//#insertdatapoint")
                        {
                            foreach (var shader in shaders)
                            {
                                shader.Output(sw);
                            }
                            sw.WriteLine("");

                            {
                                string[] soundFileLines = Resources.sound_null_js.ToArray();

                                if (audioFilename.EndsWith(".mp3"))
                                {
                                    soundFileLines = Resources.sound_mp3_js.ToArray();
                                }


                                foreach (var aln in soundFileLines)
                                    sw.WriteLine(aln);

                                sw.WriteLine("var audioData=");

                                if (audioFilename == "")
                                    sw.WriteLine("''");
                                else
                                    Misc.AddBase64String(audioFileData, sw);
                                sw.WriteLine(";");
                            }

                            if (jsExtraLines != null)
                            {
                                foreach (var jsln in jsExtraLines)
                                    sw.WriteLine(jsln);
                            }
                        }
                        else if (ifdefMatch.Success)
                        {
                            if (inIf)
                                throw new Exception($"double //#ifdef hit at line {j}");

                            string option = ifdefMatch.Groups[1].Value;
                            inIf = true;

                            usingIf = options.Contains(option);
                        }
                        else if (ln == "//#else")
                        {
                            if (!inIf)
                                throw new Exception($"//#else, but not in //#ifdef {j}");

                            usingIf = !usingIf;
                        }
                        else if (ln == "//#endif")
                        {
                            if (inIf == false)
                                throw new Exception($"//#endif without matching //#ifdef at line {j}");

                            inIf = false;
                        }
                        else
                        {
                            if (inIf)
                            {
                                if (usingIf)
                                    sw.WriteLine(ln);
                            }
                            else
                                sw.WriteLine(ln);
                        }
                    }
                }
                else
                    sw.Write(ln);
            }




            sw.Close();

            results += $"{Time()}: Build Done! (tt:{DateTime.Now - start})";
        }
        catch(Exception e)
        {
            results= e.Message;
            return false;
        }

        return true;
    }
}