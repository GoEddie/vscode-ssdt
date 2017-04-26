using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dir2Dac
{
    internal class ScriptFinder
    {
        private readonly string _rootPath;
        private readonly string _filter;

        public ScriptFinder(string path, string filter)
        {
            _rootPath = path;
            _filter = filter;
        }

        public List<string> GetScripts(DdlScriptParser scriptFixer)
        {
            var scripts = GetScripts(_rootPath);
            if (scriptFixer != null)
            {
                var fixedScripts  = new List<string>();
                foreach (var script in scripts)
                {
                    fixedScripts.AddRange(scriptFixer.GetStatements(script));
                }

                return fixedScripts;
            }

            return scripts;
        }

        private List<string> GetScripts(string path)
        {
            var scripts = new DirectoryInfo(path).EnumerateFiles(_filter).Select(f => File.ReadAllText(f.FullName)).ToList();

            foreach (var d in new DirectoryInfo(path).EnumerateDirectories())
            {
                scripts.AddRange(GetScripts(d.FullName));
            }

            return scripts;
        }
    }
}