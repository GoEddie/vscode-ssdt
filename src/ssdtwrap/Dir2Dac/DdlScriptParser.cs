using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dir2Dac
{
    public class DdlScriptParser
    {
        private static readonly Regex _batch = new Regex(@"GO\s*$", RegexOptions.Multiline);
        
        private readonly SqlServerVersion _version;

        public DdlScriptParser(SqlServerVersion version)
        {
            
            _version = version;
        }

        private static List<string> GetBatches(string script)
        {
            return _batch.Split(script).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        public List<string> GetStatements(string script)
        {
            var batches = GetBatches(script);

            var parser = GetParser();
            var generator = GetGenerator();
            var visitor = new DdlVisitor(generator);
            visitor.Statements = new List<string>();

            foreach (var batch in batches)
            {
                IList<ParseError> errors;

                visitor.PrepareForNewBatch(batch);
                var fragment = parser.Parse(new StringReader(batch), out errors);
                if (errors.Count > 0)
                {
                    Console.WriteLine("errors");
                }
                fragment.Accept(visitor);
            }
            var returnableList = new List<string>();

            foreach (var statement in visitor.Statements)
            {
                if (statement.StartsWith("alter tab", StringComparison.OrdinalIgnoreCase))
                {
                    returnableList.Add(statement);
                    continue;
                }

                if (statement.StartsWith("create", StringComparison.OrdinalIgnoreCase))
                {
                    returnableList.Add(statement);
                    continue;
                }

                if (statement.StartsWith("alter", StringComparison.OrdinalIgnoreCase))
                {
                    returnableList.Add("CREATE" + statement.Substring(5));
                }
            }
            return returnableList;
        }

        private SqlScriptGenerator GetGenerator()
        {
            switch (_version)
            {
                case SqlServerVersion.Sql90:
                    return new Sql90ScriptGenerator();
                case SqlServerVersion.Sql100:
                    return new Sql100ScriptGenerator();
                case SqlServerVersion.Sql110:
                    return new Sql110ScriptGenerator();
                case SqlServerVersion.Sql120:
                    return new Sql120ScriptGenerator();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private TSqlParser GetParser()
        {
            //I am losing the comments for some readon....
            switch (_version)
            {
                case SqlServerVersion.Sql90:
                    return new TSql90Parser (true);
            
                case SqlServerVersion.Sql100:
                    return new TSql100Parser(true);
                case SqlServerVersion.Sql110:
                    return new TSql110Parser(true);
                case SqlServerVersion.Sql120:
                    return new TSql120Parser(true);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal class DdlVisitor : TSqlFragmentVisitor
    {
        private readonly SqlScriptGenerator _generator;
        private List<fragment_position> _topLevelObjects = new List<fragment_position>();
        public List<string> Statements;
        private string _currentBatch;

        public DdlVisitor(SqlScriptGenerator generator)
        {
            _generator = generator;
        }

        public void PrepareForNewBatch(string batch)
        {
            _topLevelObjects = new List<fragment_position>();
            _currentBatch = batch;
        }

        public override void Visit(TSqlStatement node)
        {
            if (node.GetType().Name.StartsWith("Create") || node.GetType().Name.StartsWith("AlterTabl") ||
                node.GetType().Name.StartsWith("AlterProc") || node.GetType().Name.StartsWith("AlterFunc"))
            {
                if (!_topLevelObjects.Any(p => p.Start < node.StartOffset && p.Length > node.FragmentLength))
                {
                    //string script;
                    //_generator.GenerateScript(node, out script);

                    var script = _currentBatch.Substring(node.StartOffset, node.FragmentLength);

                    _topLevelObjects.Add(new fragment_position {Start = node.StartOffset, Length = node.FragmentLength});
                    Statements.Add(script);

                    Console.WriteLine(node);
                }
            }
        }

        private struct fragment_position
        {
            public int Length;
            public int Start;
        }
    }
}