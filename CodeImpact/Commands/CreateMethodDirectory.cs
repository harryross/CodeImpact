using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeImpact.Helper;
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
                var tree = GetNodeForClass.GetSyntaxTree(c);
            }
/*            var text = File.ReadAllText(path);
            _syntaxTree = new CSharpParser().Parse(text, Path.GetFileName(path));
            var tree = _syntaxTree.ToTypeSystem();
            PrintMethodTree(tree);*/
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
                .Where((FileClass toFile) => toFile.File == "somepast")
                .AndWhere((Member fromMember) => fromMember.MemberFullName == member.MemberFullName)
                .CreateUnique("fromMember-[:BELONGS_TO]->toFile")
                .ExecuteWithoutResults();
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

        
    }
}