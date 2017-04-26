using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SSDTWrap.DacFx
{
    public class ModelWrapper
    {
        private readonly SqlServerVersion _version;
        private readonly string _directory;
        private readonly string _refactorLog;
        private readonly string _preScript;
        private readonly string _postScript;

        private readonly TSqlModel _model;
        private readonly Dictionary<string, DateTime> _caches = new Dictionary<string, DateTime>();
        private readonly TSqlParser _parser;

        public ModelWrapper(SqlServerVersion version, string directory, string refactorLog, string preScript, string postScript, List<DacReference> ssdtReferences)
        {
            _version = version;
            _directory = directory;
            _refactorLog = refactorLog;
            _preScript = preScript;
            _postScript = postScript;

            _model = GetModel();
            _parser = GetParser();
            FillModel();
        }

        public TSqlModel CurrentModel()
        {
            return _model;
        }

        private TSqlParser GetParser()
        {
            switch (_version)
            {
                case SqlServerVersion.Sql90:
                    return new TSql90Parser(false);
                case SqlServerVersion.Sql100:
                    return new TSql100Parser(false);
                case SqlServerVersion.SqlAzure:
                    return new TSql140Parser(false);
                case SqlServerVersion.Sql110:
                    return new TSql110Parser(false);
                case SqlServerVersion.Sql120:
                    return new TSql120Parser(false);
                case SqlServerVersion.Sql130:
                    return new TSql130Parser(false);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FillModel()
        {
            var dir = new DirectoryInfo(_directory);
            foreach (var file in dir.GetFiles("*.sql", SearchOption.AllDirectories))
            {
                AddFile(file);
            }
            Validate();
        }

        public void Validate()
        {
            _model.Validate();
        }

        private Dictionary<string, List<string>> _files = new Dictionary<string, List<string>>();

        public void AddFile(FileInfo file)
        {
            var name = file.FullName;
            var lastChanged = file.LastWriteTimeUtc;

            if (_files.ContainsKey(name))
            {   //do we need to clear out some old ones??
                var existingScripts = _files[name];
                foreach (var f in existingScripts)
                {
                    IList<ParseError> err;
                    var parsed = _parser.Parse(new StringReader("--"), out err);
                    _model.AddOrUpdateObjects(parsed as TSqlScript, f, new TSqlObjectOptions());

                }
            }

            var scripts = GetScripts(name);

            _caches[name] = lastChanged;
            var scriptList = new List<string>();

            foreach (var script in scripts)
            {
                IList<ParseError> err;
                var parsed = _parser.Parse(new StringReader(script.Content), out err);
                _model.AddOrUpdateObjects(parsed as TSqlScript, script.Path, new TSqlObjectOptions());
                scriptList.Add(script.Path);
            }

            _files[name] = scriptList;
        }

        private string GetChecksum(string s)
        {
            var sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(s);
            byte[] hash = sha1.ComputeHash(buf, 0, buf.Length);
            return System.BitConverter.ToString(hash);
        }
        private List<Script> GetScripts(string fileName)
        {
            var scripts = new List<Script>();

            var content = new StreamReader(fileName).ReadToEnd();
            int i = 0;
            foreach (var part in content.Split(new string[]{"\nGO"}, StringSplitOptions.None))
            {
                scripts.Add(new Script()
                {
                    Checksum = GetChecksum(part),
                    Content = part,
                    Path = $"{fileName}:{i++}",
                    FileName = fileName
                });
            }

            return scripts;
        }

        private class Script
        {
            public string Content { get; set; }
            public string FileName { get; set; }
            public string Path { get; set; }
            public int Part { get; set; }
            public string Checksum { get; set; }    //just a date for now but will be a checksum
        }

        private TSqlModel GetModel()
        {
            return new TSqlModel(_version, new TSqlModelOptions());
        }

        public void Build(SsdtConfig settings)
        {
//            var p = new Dir2Dac.Program()
        }
        //NEXTTODO - MOVE Dir2Dac into this solution and use it!
    }
}
