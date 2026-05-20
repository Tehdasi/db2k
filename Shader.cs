using System;
using System.Collections.Generic;
using System.Text;

namespace db2k
{
    public class Shader
    {
        public class GlslLine
        {
            public string text = "";
            public string srcFilename = "";
            public int srcLineNumber;
        }

        public class Uniform
        {
            public string name;
            public string glType;
            public string glslType;
        }

        public class Texture
        {
            public string name;
            public byte[] data;
        }

        public class SourceSection
        {
            public string filename = "";
            public int glslStart, glslEnd;
            public int srcLineNumber;
        }



        public List<SourceSection> sourceSections;
        public List<GlslLine> glslLines;
        public HashSet<string> includedGlslFiles;

        public List<Texture> textures;
        public List<Uniform> uniforms;

        public string name;

        public Shader()
        {
            sourceSections = [];
            includedGlslFiles = [];
            glslLines = [];

            textures = [];
            uniforms = [];
        }

        public void Output(StreamWriter sw)
        {
            sw.WriteLine($"var {name} = ");
            sw.WriteLine($"{{ _: {{ name: '{name}',");

            //    uniformLines.ForEach(_ => sw.WriteLine(_));

            sw.WriteLine("text: `");
            glslLines.ForEach(_ => sw.WriteLine(_.text));
            sw.WriteLine("`,");


            sw.WriteLine("textures: [");
            foreach (var t in textures)
            {
                sw.WriteLine($"{{ name: '{t.name}', data: ");
                Misc.AddBase64String(t.data, sw);
                sw.WriteLine("},");
            }
            sw.WriteLine("],");

            sw.WriteLine("uniforms: [");
            uniforms.ForEach( _=>sw.WriteLine( $"{{ name: '{_.name}', glType: '{_.glType}' }}," ) );
            sw.WriteLine("],");

            sw.WriteLine("sourceMap: [");
            sourceSections.ForEach(_ => sw.WriteLine($"['{_.filename}',{_.glslStart},{_.glslEnd},{_.srcLineNumber}],"));
            sw.WriteLine("],");




            sw.WriteLine("}};");
        }
    }
}
