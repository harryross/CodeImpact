using System;
using System.Linq;
using CodeImpact.Helper;
using CodeImpact.Model;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Neo4jClient;

namespace CodeImpact.Commands
{
    public class CreateMethodDirectory
    {
        public static GraphClient Client { get; private set; }

        public CreateMethodDirectory()
        {
            Client = new GraphClient(new Uri("http://neo4j:metead@localhost.:7474/db/data"));
        }

        public void CreateMethodDependanciesForFile()
        {
            Client.Connect();
            var classes = Client.Cypher
                .Match("(_class:Class)")
                .Return(_class => _class.As<FileClass>())
                .Results.ToList();

            foreach (var c in classes)
            {
                GetMembersInClass(c);
            }
        }


        private void GetMembersInClass(FileClass fileClass)
        {
            AstNode theNode = GetNodeForClass.GetTopNodeForClass(fileClass);
            var tree = GetNodeForClass.GetSyntaxTree(fileClass);
            var member = tree.TopLevelTypeDefinitions.SingleOrDefault(x => x.Name == fileClass.ClassName);
            if (member != null)
            {
                foreach (var method in member.Methods)
                {
                    GetMethodsInClassAndCreateRelationship(fileClass, method);
                }
            }
        }

        private void GetMethodsInClassAndCreateRelationship(FileClass fileClass, IUnresolvedMethod method)
        {
            var member = new Member
            {
                File = method.BodyRegion.FileName,
                MemberType = method.SymbolKind.ToString(),
                ReturnType = method.ReturnType.ToString(),
                MemberName = method.Name,
                MemberFullName = method.ReflectionName,
                Accessibility = method.Accessibility.ToString(),
                Class = fileClass.FullClassName
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
                .Match("(fromMember:Member)", "(toFile:Class)")
                .Where((FileClass toFile) => toFile.FullClassName == fileClass.FullClassName)
                .AndWhere((Member fromMember) => fromMember.MemberFullName == member.MemberFullName)
                .CreateUnique("fromMember-[:BELONGS_TO]->toFile")
                .ExecuteWithoutResults();
        }
    }
}