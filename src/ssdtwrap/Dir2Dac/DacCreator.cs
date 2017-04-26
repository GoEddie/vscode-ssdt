using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using GOEddie.Dacpac.References;
using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Model;

namespace Dir2Dac
{
    public class DacCreator
    {
        private readonly TSqlModelOptions _options;
        private readonly string _outputPath;
        private readonly string _postDeployScript;
        private readonly string _preDeployScript;
        private readonly List<Reference> _references;
        private readonly List<Source> _sourceFolder;
        private readonly SqlServerVersion _version;
        private readonly DdlScriptParser ScriptFixer;

        public DacCreator(Args args)
        {
            _outputPath = args.DacpacPath;
            _sourceFolder = args.SourcePath;
            _references = args.References;
            _version = args.SqlServerVersion;
            _options = args.SqlModelOptions;
            _preDeployScript = args.PreCompareScript;
            _postDeployScript = args.PostCompareScript;
            if(args.FixDeployScripts)
                ScriptFixer = new DdlScriptParser(_version);
        }

        public DacCreator(string dacpacPath, List<Reference> references, SqlServerVersion version, TSqlModelOptions sqlModelOptions, string preDeploy, string postDeploy)
        {
            _outputPath = dacpacPath;
            _references = references;
            _version = version;
            _options = sqlModelOptions;
            _preDeployScript = preDeploy;
            _postDeployScript = postDeploy;
            
        }

        public bool Write()
        {
            try
            {
                var dummyModel = BuildInitialEmptyModel();

                DacPackageExtensions.BuildPackage(_outputPath, dummyModel, new PackageMetadata());

                AddReferences();

                AddPrePostScripts();

                var model = BuildActualModel();

                WriteFinalDacpac(model);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        public bool Write(TSqlModel model)
        {
            try
            {
                var dummyModel = BuildInitialEmptyModel();

                DacPackageExtensions.BuildPackage(_outputPath, dummyModel, new PackageMetadata());

                AddReferences();

                AddPrePostScripts();
                
                WriteFinalDacpac(model);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        private void AddPrePostScripts()
        {
            if (string.IsNullOrEmpty(_preDeployScript) && string.IsNullOrEmpty(_postDeployScript))
                return;

            using (var package = Package.Open(_outputPath, FileMode.Open, FileAccess.ReadWrite))
            {
                if (!string.IsNullOrEmpty(_preDeployScript))
                {
                    if (!File.Exists(_preDeployScript))
                    {
                        throw new FileNotFoundException("Pre Deploy Script was not found", _preDeployScript);
                    }

                    var part = package.CreatePart(new Uri("/predeploy.sql", UriKind.Relative), "text/plain");

                    using (var reader = new StreamReader(_preDeployScript))
                    {
                        reader.BaseStream.CopyTo(part.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite));
                    }
                }


                if (!string.IsNullOrEmpty(_postDeployScript))
                {
                    if (!File.Exists(_postDeployScript))
                    {
                        throw new FileNotFoundException("Post Deploy Script was not found", _postDeployScript);
                    }


                    var part = package.CreatePart(new Uri("/postdeploy.sql", UriKind.Relative), "text/plain");

                    using (var reader = new StreamReader(_postDeployScript))
                    {
                        reader.BaseStream.CopyTo(part.GetStream(FileMode.OpenOrCreate, FileAccess.ReadWrite));
                    }


                    package.Close();
                }
            }
        }

        private void WriteFinalDacpac(TSqlModel model)
        {
            using (var dac = DacPackage.Load(_outputPath, DacSchemaModelStorageType.File, FileAccess.ReadWrite))
            {
                var messages = model.Validate();

                foreach (var message in messages)
                {
                    Console.WriteLine(message.MessageType + " : " + message.Message);
                }

                try
                {
                    dac.UpdateModel(model, new PackageMetadata());
                    Console.WriteLine("Success.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to write dacpac - errors/warnings: " + e);
                    throw;
                }
            }
        }
        private Dictionary<string, bool> _alreadyAdded = new Dictionary<string, bool>();

        private TSqlModel BuildActualModel()
        {
            var model = new TSqlModel(_outputPath);

            foreach (var source in _sourceFolder)
            {
                var finder = new ScriptFinder(source.Path, source.Filter);

                foreach (var script in finder.GetScripts(ScriptFixer))
                {
                    try
                    {
                        if(!_alreadyAdded.ContainsKey(script))
                            model.AddObjects(script);

                        _alreadyAdded[script] = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error adding script: {0}, script:\r\n{1}", e.Message, script);
                    }
                }
            }
            

            return model;
        }

        private void AddReferences()
        {
            var parser = new HeaderWriter(_outputPath, new DacHacFactory());
            foreach (var reference in _references)
            {
                parser.AddCustomData(reference.GetData());
            }

            parser.Close();
        }

        private TSqlModel BuildInitialEmptyModel()
        {
            var dummyModel = new TSqlModel(_version, _options);
            dummyModel.AddOrUpdateObjects("create procedure a as select 1", "dummy", new TSqlObjectOptions());
            dummyModel.DeleteObjects("dummy");
            return dummyModel;
        }
    }
}