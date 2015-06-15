using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeImpact.Helper;
using CodeImpact.Model;
using ICSharpCode.NRefactory.CSharp;
using Neo4jClient;

namespace CodeImpact.Commands
{
    class CreateFileStructureCommand
    {
        public static GraphClient Client { get; private set; }
        private SyntaxTree _syntaxTree;

        public CreateFileStructureCommand()
        {
            Client = new GraphClient(new Uri("http://neo4j:metead@localhost.:7474/db/data"));
        }
        public void WriteList(List<string> list)
        {
            
            Client.Connect();
            UpdateDatabase(list);
            CreateClassRelationships();
        }

        private void CreateClassRelationships()
        {
            var classes = Client.Cypher
                .Match("(_class:Class)")
                .Return(_class => _class.As<FileClass>())
                .Results.ToList();

            foreach (var c in classes)
            {
                AstNode theNode = GetNodeForClass.GetTopNodeForClass(c);
                if (theNode != null)
                {
                    var tree = theNode.Descendants.Where(x => x.Role == Roles.Identifier && x.Parent.Role == Roles.Type && x.ToString() != "var").ToList();
                    foreach (var member in tree)
                    {
                        if (member.ToString() != c.ClassName)
                        {
                            var member1 = member;
                            var name = classes.Where(x => x.ClassName == member1.ToString()).ToList();
                            if (name.Count != 1)
                            {
                                continue;
                            }
                            AddClassRelationShip(c.FullClassName, name.First().FullClassName);
                        }
                    }
                }
            }
        }



        private void AddClassRelationShip(string fromSourceFile, string toSourceFile)
        {
            Client.Cypher
                .Match("(fromFile:Class)", "(toFile:Class)")
                .Where((FileClass fromFile) => fromFile.FullClassName == fromSourceFile)
                .AndWhere((FileClass toFile) => toFile.FullClassName == toSourceFile)
                .CreateUnique("fromFile-[:REFERENCES]->toFile")
                .ExecuteWithoutResults();
        }

        private void UpdateDatabase(List<string> list)
        {
            foreach (var file in list)
            {
                var results = GetClassesForFile(file);
                foreach (var fileClass in results)
                {
                    
                    Client.Cypher
                        .Merge("(_class:Class { FullClassName: {fullClassName}})")
                        .OnCreate()
                        .Set("_class = {fileClass}")
                        .WithParams(new
                        {
                            fullClassName = fileClass.FullClassName,
                            fileClass
                        })
                        .ExecuteWithoutResults();
                }
                
            }
        }

        private List<FileClass> GetClassesForFile(string fileName)
        {
            var fileText = File.ReadAllText(fileName);
            _syntaxTree = new CSharpParser().Parse(fileText, Path.GetFileName(fileName));
            var tree = _syntaxTree.ToTypeSystem();
            var list = new List<FileClass>();
            foreach (var tld in tree.TopLevelTypeDefinitions)
            {
                var fileClass = new FileClass
                {
                    FullClassName = tld.ReflectionName,
                    ClassName = tld.Name,
                    File = fileName,
                    Type = FileType.CSharp
                };
                list.Add(fileClass);
            }
            return list;
        }
    }
}
