using System;
using System.Collections.Generic;
using System.Linq;
using CodeImpact.Model;
using ICSharpCode.NRefactory.TypeSystem;
using Neo4jClient;

namespace CodeImpact.Repository
{
    public class ClassRepository
    {
        private static GraphClient Client { get; set; }

        public ClassRepository()
        {
            Client = new GraphClient(new Uri("http://neo4j:metead@localhost.:7474/db/data"));
            Client.Connect();
        }

        public List<FileClass> GetAllClassesFromGraph()
        {
            var classes = Client.Cypher
                .Match("(_class:Class)")
                .Return(_class => _class.As<FileClass>())
                .Results.ToList();

            return classes;
        }

        public IEnumerable<FileClass> GetAllInterfacesFromGraph()
        {
            var interfaces = Client.Cypher
                .Match("(_interface:Class)")
                .Where((FileClass _interface) => _interface.Kind.ToString() == TypeKind.Interface.ToString())
                .Return(_interface => _interface.As<FileClass>())
                .Results.ToList();

            return interfaces;
        }

        public void CreateOrMergeClass(FileClass fileClass)
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

        public void CreateBaseTypeRelationship(FileClass baseClass, FileClass superClass)
        {
            Client.Cypher
                .Match("(fromFile:Class)", "(toFile:Class)")
                .Where((FileClass fromFile) => fromFile.FullClassName == baseClass.FullClassName)
                .AndWhere((FileClass toFile) => toFile.FullClassName == superClass.FullClassName)
                .CreateUnique("fromFile-[:BASE_TYPE]->toFile")
                .ExecuteWithoutResults();
        }

        public void CreateClassReferencesRelationship(FileClass fromClass, FileClass toClass)
        {
            Client.Cypher
                .Match("(fromFile:Class)", "(toFile:Class)")
                .Where((FileClass fromFile) => fromFile.FullClassName == fromClass.FullClassName)
                .AndWhere((FileClass toFile) => toFile.FullClassName == toClass.FullClassName)
                .CreateUnique("fromFile-[:REFERENCES]->toFile")
                .ExecuteWithoutResults();
        }

        public List<FileClass> GetSuperClassesOfInterface(FileClass superClass)
        {
            var classes = Client.Cypher
                .Match("(_class:Class)-[:BASE_TYPE*]->(baseType:Class)")
                .Where((FileClass baseType) => baseType.FullClassName == superClass.FullClassName)
                .Return(_class => _class.As<FileClass>())
                .Results.ToList();
            return classes;
        }
    }
}