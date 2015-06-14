using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            CreateFileRelationships(list);
        }

        private void CreateFileRelationships(List<string> list)
        {
            string fileText;
            foreach (var filePath in list)
            {
                fileText = File.ReadAllText(filePath);
                List<string> tempList = new List<string>(list);
                tempList.Remove(filePath);
                foreach (var file in tempList)
                {
                    string temp = Path.GetFileName(file);
                    if (temp != null)
                    {
                        temp = temp.Replace(".cs", String.Empty);
                        if (fileText.Contains(temp))
                        {
                            AddFileRelationShip(filePath, file);
                        }
                    }
                }
            }
            
        }

        private void AddFileRelationShip(string fromSourceFile, string toSourceFile)
        {
            Client.Cypher
                .Match("(fromFile:File)", "(toFile:File)")
                .Where((FileClass fromFile) => fromFile.File == fromSourceFile)
                .AndWhere((FileClass toFile) => toFile.File == toSourceFile)
                .CreateUnique("fromFile-[:REFERENCES]->toFile")
                .ExecuteWithoutResults();
        }

        private void UpdateDatabase(List<string> list)
        {
            foreach (var file in list)
            {
                var text = File.ReadAllText(file);
                _syntaxTree = new CSharpParser().Parse(text, Path.GetFileName(file));
                var tree = _syntaxTree.ToTypeSystem();
                var fileClass = new FileClass
                {
                    File = file,
                    Type = FileType.CSharp
                };
                var file1 = file;
                Client.Cypher
                    .Merge("(file:File { File: {fileName}})")
                    .OnCreate()
                    .Set("file = {fileClass}")
                    .WithParams(new
                    {
                        fileName = file1,
                        fileClass
                    })
                    .ExecuteWithoutResults();
            }
        }
    }
}
