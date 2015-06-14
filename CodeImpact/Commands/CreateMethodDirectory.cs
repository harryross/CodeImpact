using System;
using System.IO;
using CodeImpact.Model;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using Neo4jClient;

namespace CodeImpact.Commands
{
    public class CreateMethodDirectory
    {
        public static GraphClient Client { get; private set; }
        private SyntaxTree _syntaxTree;
        private string _path;

        public CreateMethodDirectory()
        {
            Client = new GraphClient(new Uri("http://neo4j:metead@localhost.:7474/db/data"));
        }

        public void CreateMethodDependanciesForFile(string path)
        {
            _path = path;
            Client.Connect();
            var text = File.ReadAllText(path);
            _syntaxTree = new CSharpParser().Parse(text, Path.GetFileName(path));
            var tree = _syntaxTree.ToTypeSystem();
            PrintMethodTree(tree);
        }


        private void PrintMethodTree(CSharpUnresolvedFile tree)
        {
            foreach (IUnresolvedTypeDefinition type in tree.TopLevelTypeDefinitions)
            {
                foreach (var member in type.Members)
                {
                    AddMethodNode(member);
                }
            }
        }

        private void AddMethodNode(IUnresolvedMember memberBase)
        {
            var member = new Member
            {
                File = memberBase.BodyRegion.FileName,
                MemberType = memberBase.SymbolKind.ToString(),
                ReturnType = memberBase.ReturnType.ToString(),
                MemberName = memberBase.Name,
                MemberFullName = memberBase.ReflectionName,
                Accessibility = memberBase.Accessibility.ToString()
            };

            Client.Cypher
                    .Merge("(member:Member { MemberFullName: {memberFullName}})")
                    .OnCreate()
                    .Set("member = {member}")
                    .WithParams(new
                    {
                        memberFullName = member.MemberFullName,
                        member
                    })
                    .ExecuteWithoutResults();

            Client.Cypher
                .Match("(fromMember:Member)", "(toFile:File)")
                .Where((FileClass toFile) => toFile.File == _path)
                .AndWhere((Member fromMember) => fromMember.MemberFullName == member.MemberFullName)
                .CreateUnique("fromMember-[:BELONGS_TO]->toFile")
                .ExecuteWithoutResults();
        }
    }
}