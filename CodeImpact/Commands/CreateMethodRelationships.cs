using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeImpact.Model;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using Neo4jClient;

namespace CodeImpact.Commands
{
    public class CreateMethodRelationships
    {
        public static GraphClient Client { get; private set; }
        private SyntaxTree _syntaxTree;
        private string _path;

        public CreateMethodRelationships()
        {
            Client = new GraphClient(new Uri("http://neo4j:metead@localhost.:7474/db/data"));
        }

        public void CreateMethodRelationshipsAll(List<string> files)
        {
            Client.Connect();
            foreach (var f in files)
            {
                var members = Client.Cypher
                    .OptionalMatch("(member:Member)-[:BELONGS_TO]->(file:File)")
                    .Where((FileClass file) => file.File == f)
                    .Return((member, file) => new
                    {
                        Member = member.As<Member>(),
                        File = file.As<FileClass>()
                    })
                    .Results.ToList();
                    var text = File.ReadAllText(f);
                    _syntaxTree = new CSharpParser().Parse(text, f);
                    var otherTree = _syntaxTree.Descendants.Where(x => x.NodeType == NodeType.Member).ToList();
                foreach (var node in otherTree)
                {
                    var methodtext = node.ToString();
                    var sing = node.Children.SingleOrDefault(x => x.Role == Roles.Identifier);
                    if (sing != null)
                    {
                        var ident = sing.ToString();
                        var mem = members.SingleOrDefault(x => x.Member.MemberName == ident);
                        if (mem == null)
                        {
                            mem = members.SingleOrDefault(x => x.Member.MemberName == ".ctor");
                        }
                        GetMethodsCalled(mem.Member, methodtext, f);
                    }
                }
            }
        }


        private void GetMethodsCalled(Member memberBase, string methodBase, string fileClass)
        {
            var members = Client.Cypher
                .OptionalMatch("(member:Member)-[:BELONGS_TO]->(file:File)")
                .Where((FileClass file) => file.File == fileClass)
                .Return(member => member.As<Member>())
                .Results
                .ToList();
         
            foreach (var m in members)
            {
                if (methodBase.Contains(m.MemberName) && memberBase.MemberFullName != m.MemberFullName)
                {
                    Client.Cypher
                        .Match("(fromMember:Member)", "(toMember:Member)")
                        .Where((Member toMember) => toMember.MemberFullName == m.MemberFullName)
                        .AndWhere((Member fromMember) => fromMember.MemberFullName == memberBase.MemberFullName)
                        .CreateUnique("fromMember-[:METHOD_CALLS]->toMember")
                        .ExecuteWithoutResults();
                }
            }
        }
    }
}