using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GOEddie.Dacpac.References;
using Microsoft.SqlServer.Dac.Model;

namespace Dir2Dac
{
    public class Args
    {
        private readonly string[] _args;
        public List<Reference> References { get; private set; }
        public string DacpacPath { get; private set; }
        public List<Source> SourcePath { get; private set; }
        public SqlServerVersion SqlServerVersion { get; private set; }
        public TSqlModelOptions SqlModelOptions { get; private set; }
        public string PreCompareScript { get; private set; }
        public string PostCompareScript { get; private set; }
        public bool FixDeployScripts { get; private set; }

        public Args(string[] args)
        {
            _args = args;
        }

        public ParseResult Parse()
        {
            SqlModelOptions = new TSqlModelOptions();
            References = new List<Reference>();
            SourcePath = new List<Source>();
            FixDeployScripts = false;

            foreach (var argUpper in _args)
            {
                var arg = argUpper.ToLower();

                if (arg.StartsWith("/fixddlscripts") || arg.StartsWith("/fix"))
                {
                    FixDeployScripts = true;
                }

                if (arg.StartsWith("/sp=") || arg.StartsWith("/sourcepath="))
                {
                    ReadSourcePath(argUpper);
                    continue;
                }

                if (arg.StartsWith("/pre=") || arg.StartsWith("/precompare="))
                {
                    ReadPreComparePath(arg);
                    continue;
                }

                if (arg.StartsWith("/post=") || arg.StartsWith("/postcompare="))
                {
                    ReadPostComparePath(arg);
                    continue;
                }

                if (arg.StartsWith("/dp=") || arg.StartsWith("/dacpacpath="))
                {
                    ReadDacpacPath(arg);
                    continue;
                }

                if (arg.StartsWith("/sv=") || arg.StartsWith("/sqlversion="))
                {
                    
                    if (!ReadSqlVersion(arg))
                        return ParseResult.Error;

                    continue;
                }

                if (arg.StartsWith("/do=") || arg.StartsWith("/databaseoption="))
                {
                    if (!ReadDatabaseOption(arg, argUpper))
                        return ParseResult.Error;

                    continue;
                }

                if (arg.StartsWith("/r=") || arg.StartsWith("/ref="))
                {
                    if (!ReadReference(arg, argUpper))
                    {
                        return ParseResult.Error;
                    }

                    continue;
                }

                if (arg.StartsWith("/?"))
                {
                    PrintArgs();
                    return ParseResult.OkShowError;
                }
            }
            
            return ParseResult.Ok;
        }

        private void ReadPostComparePath(string s)
        {
            PostCompareScript = s.Split('=')[1];
        }

        private void ReadPreComparePath(string s)
        {
            PreCompareScript = s.Split('=')[1];
        }

        public static void PrintArgs()
        {
            Console.WriteLine("Dir2Dac AKA SqlDacage.exe - take a scripts folder and build a dacpac complete with database options and references...");
            Console.WriteLine(@"


            /SourcePath=c:\Path\to\scriptsFolder:filter     You can specify multiple sources (eg if you wanted to use different filters)
            /sp=c:\Path\to\scriptsFolder:filter

            /SourcePath=c:\Path\to\scriptsFolder            If you do not specify a filter then you get *.sql by default
            /sp=c:\Path\to\scriptsFolder

            /DacpacPath=c:\Path\to\Output\Dacpac.dacpac
            /dp=c:\Path\to\Output\Dacpac.dacpac

            /SqlVersion=SQLVersion          SQLVersion is one of the SqlServerVersion enumeration (i.e. Sql90, Sql100, Sql110, Sql120, Sql130, SqlAzure etc)
            /sv=SQLVersion              

            /DatabaseOption=DatabaseOptionName=DatabaseOptioneValue The option is one of the properties on TSqlModelOptions current page = https://msdn.microsoft.com/en-us/library/microsoft.sqlserver.dac.model.tsqlmodeloptions(v=sql.120).aspx
            /do=DatabaseOptionName=DatabaseOptioneValue

            /ref=this=c:\path\to\dacpac.dacpac=name     The path doesn't have to be correct as long as you copy the dacpac to the same folder as the output dacpac
            /r=this=c:\path\to\dacpac.dacpac=name

            /ref=other=c:\path\to\dacpac.dacpac=name=databaseName     Same as ""this"" except you can pass a database name for cross-db references
            /r=other=c:\path\to\dacpac.dacpac=name=databaseName

            /ref=otherserver=c:\path\to\dacpac.dacpac=name=databaseName=serverName     Same as ""other"" except you can pass a server name for linked server references
            /r=otherserver=c:\path\to\dacpac.dacpac=name=databaseName=servName

            /ref=master=c:\path\to\dacpac.dacpac     Add a reference to the master system db, you will need to copy in the correct version for your target sql version
            /r=master=c:\path\to\dacpac.dacpac

            /ref=msdb=c:\path\to\dacpac.dacpac     Add a reference to the msdb system db, you will need to copy in the correct version for your target sql version
            /r=msdb=c:\path\to\dacpac.dacpac
            
            /precompare=c:\path\to\precompare\script.sql
            /pre=c:\path\to\precompare\script.sql


            /postcompare=c:\path\to\postcompare\script.sql
            /post=c:\path\to\postcompare\script.sql

            /?  - Bask in the glory of this help

"
                              );
        }

        private bool ReadReference(string arg, string argUpper)
        {
            var parts = arg.Split('=');
            var partsUpper = argUpper.Split('=');
            if (parts.Length < 3)
            {
                Console.WriteLine("Error unable to parse reference - format is /r=type=path=name=dbname=servername - arg = {0}", argUpper);
                return false;
            }

            if (parts[1] == "this")
            {
                var r = new ThisReference(partsUpper[2], partsUpper[3]);
                References.Add(r);
                return true;
            }

            if (parts[1] == "other")
            {
                if (parts.Length != 5)
                {
                    Console.WriteLine("Error unable to parse reference - format is /r=other=path=name=dbname - arg = {0}", argUpper);
                    return false;
                }

                var r = new OtherReference(partsUpper[2], partsUpper[3], partsUpper[4]);
                References.Add(r);
                return true;
            }


            if (parts[1] == "otherserver")
            {
                if (parts.Length != 6)
                {
                    Console.WriteLine("Error unable to parse reference - format is /r=otherserver=path=name=dbname=servername - arg = {0}", argUpper);
                    return false;
                }

                var r = new OtherServerReference(partsUpper[2], partsUpper[3], partsUpper[4], partsUpper[5]);
                References.Add(r);
                return true;
            }

            if (parts[1] == "master" || parts[1] == "msdb")
            {
                if (parts.Length != 3)
                {
                    Console.WriteLine("Error unable to parse reference - format is /r=master=path - arg = {0}", argUpper);
                    return false;
                }

                var r = new SystemReference(partsUpper[2], parts[1] + ".dacpac", parts[1]);
                References.Add(r);
                return true;
            }
            

            return false;
        }

        private bool ReadDatabaseOption(string arg, string argUpper)
        {
            var options = SqlModelOptions;

            var stringOption = arg.Split('=')[1];

            if (arg.Split('=').Length != 3)
            {
                Console.WriteLine("Error unable to parse DatabaseOption - format is /do=OptionName=OptionValue - arg = {0}", argUpper);
                
                    return false;
                
            }

            var stringValue = argUpper.Split('=')[2];

            var property = options.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLowerInvariant() == stringOption);

            if (property == null)
            {
                Console.WriteLine("Error unable to parse DatabaseOption - property name was not recognised - arg = {0}", argUpper);
                
                    return false;
                
            }

            if (property.PropertyType == typeof (int) || property.PropertyType == typeof (int?))
            {
                var i = Int32.Parse(stringValue);
                property.SetValue(options, i);
                return true;
            }

            if (property.PropertyType == typeof (string))
            {
                property.SetValue(options, stringValue);
                return true;
            }

            if (property.PropertyType == typeof (bool) || property.PropertyType == typeof (bool?))
            {
                var b = Boolean.Parse(stringValue);
                property.SetValue(options, b);
                return true;
            }

            if (property.PropertyType == typeof (bool) || property.PropertyType == typeof (bool?))
            {
                var b = Boolean.Parse(stringValue);
                property.SetValue(options, b);
                return true;
            }

            Console.WriteLine("Error setting database option: {0} - cannot handle type, add to Args.cs", argUpper);
            return false;
        }

        private bool ReadSqlVersion(string arg)
        {
            var stringVersion = arg.Split('=')[1];
            SqlServerVersion version;

            if (!Enum.TryParse(stringVersion, true, out version))
            {
                Console.WriteLine("Error unable to parse SqlVersion: " + stringVersion);
                return false;

            }
            
            SqlServerVersion = version;

            return true;
        }

        private void ReadDacpacPath(string arg)
        {
            DacpacPath = arg.Split('=')[1];
        }

        private void ReadSourcePath(string arg)
        {
            var parts = arg.Split('=');
            if (parts.Length == 2)
            {
                SourcePath.Add(new Source()
                {
                    Path = parts[1],
                    Filter = "*.sql"
                });

                return;
            }

            SourcePath.Add(new Source()
            {
                Path = parts[1],
                Filter = parts[2]
            });

        }
    }

    public class Source
    {
        public string Path;
        public string Filter;
    }

    public enum ParseResult
    {
        Error,
        OkShowError,
        Ok
    }

    public class Reference
    {
        protected static ReferenceBuilder _referenceBuilder = new ReferenceBuilder();

        public virtual CustomData GetData()
        {
            throw new Exception("override this please");
        }
    }

    public class ThisReference : Reference
    {
        private readonly string _fileName;
        private readonly string _logicalName;

        public override CustomData GetData()
        {
            return _referenceBuilder.BuildThisDatabaseReference(_fileName, _logicalName);
        }

        public ThisReference(string fileName, string logicalName)
        {
            _fileName = fileName;
            _logicalName = logicalName;
        }
    }


    public class SystemReference : Reference
    {
        private readonly string _fileName;
        private readonly string _logicalName;
        private readonly string _database;

        public override CustomData GetData()
        {
            return _referenceBuilder.BuildSystemDatabaseReference(_database, _fileName, _logicalName);
        }

        public SystemReference(string fileName, string logicalName,  string database)
        {
            _fileName = fileName;
            _logicalName = logicalName;
            _database = database;
        }
    }


    public class OtherReference : Reference
    {
        private readonly string _fileName;
        private readonly string _logicalName;
        private readonly string _database;

        public override CustomData GetData()
        {
            return _referenceBuilder.BuildOtherDatabaseReference(_database, _fileName, _logicalName);
        }

        public OtherReference(string fileName, string logicalName, string database)
        {
            _fileName = fileName;
            _logicalName = logicalName;
            _database = database;
        }
    }

    public class OtherServerReference : Reference
    {
        private readonly string _fileName;
        private readonly string _logicalName;
        private readonly string _database;
        private readonly string _server;

        public override CustomData GetData()
        {
            return _referenceBuilder.BuildOtherServerReference(_database, _fileName, _logicalName, _server);
        }

        public OtherServerReference(string fileName, string logicalName, string database, string server)
        {
            _fileName = fileName;
            _logicalName = logicalName;
            _database = database;
            _server = server;
        }
    }


    enum ReferenceType
    {
        ThisDatababase,
        OtherDatabase,
        OtherServer,
        Master,
        Msdb
    }
}
