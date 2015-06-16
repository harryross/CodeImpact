using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeImpact.Model;
using ICSharpCode.NRefactory.CSharp;
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
                    .OptionalMatch("(member:Member)-[:BELONGS_TO]->(file:Class)")
                    .Where((FileClass file) => file.FullClassName == f)
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
                    var sing = node.Children.SingleOrDefault(x => x.Role == Roles.Identifier);
                    if (sing != null)
                    {
                        var ident = sing.ToString();
                        var mem = members.SingleOrDefault(x => x.Member.MemberName == ident);
                        if (mem == null)
                        {
                            mem = members.SingleOrDefault(x => x.Member.MemberName == ".ctor");
                        }
                        foreach (var m in node.Descendants.Where(x => x.Role == Roles.Identifier))
                        {
                            foreach (var me in members)
                            {
                                if (me.Member.MemberName == m.ToString() && me.Member.MemberFullName != mem.Member.MemberFullName)
                                {
                                    LinkMethods(mem.Member.MemberFullName, me.Member.MemberFullName);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void GetMethodCallsToOtherFiles(List<string> files)
        {
            foreach (var file in files)
            {
                GetMethodCalls(file);
            }
        }

        private void GetMethodCalls(string file)
        {
            var allFiles = Client.Cypher
                .OptionalMatch("(baseFile:File)-[REFERENCES]->(relatedFiles:File)")
                 .Where((FileClass baseFile) => baseFile.File == file)
                 .Return((baseFile, relatedFiles) => new
                 {
                     BaseFile = baseFile.As<FileClass>(),
                     RelatedFiles = relatedFiles.CollectAs<FileClass>()
                 }).Results.SingleOrDefault();
            var baseMembers = Client.Cypher
                .OptionalMatch("(member:Member)-[:BELONGS_TO]->(mainFile:File)")
                .Where((FileClass mainFile) => mainFile.File == file)
                .Return(member => member.As<Member>())
                .Results
                .ToList();
            
            
            if (allFiles != null)
            {
                foreach (var files in allFiles.RelatedFiles)
                {
                    var members = Client.Cypher
                        .OptionalMatch("(member:Member)-[:BELONGS_TO]->(mainFile:File)")
                        .Where((FileClass mainFile) => mainFile.File == files.Data.File)
                        .Return(member => member.As<Member>())
                        .Results
                        .ToList();

                    var text = File.ReadAllText(file);
                    _syntaxTree = new CSharpParser().Parse(text, Path.GetFileName(file));
                    var otherTree = _syntaxTree.Descendants.Where(x => x.NodeType == NodeType.Member).ToList();
                    foreach (var node in otherTree)
                    {
                        var singleChild = node.Children.SingleOrDefault(x => x.Role == Roles.Identifier);
                        if (singleChild != null)
                        {
                            var singleChildString = singleChild.ToString();
                            var mem = baseMembers.SingleOrDefault(x => x.MemberName == singleChildString);
                            foreach (var m in node.Descendants.Where(x => x.Role == Roles.Identifier))
                            {
                                foreach (var me in members)
                                {
                                    if (me.MemberName == m.ToString() && me.MemberFullName != mem.MemberFullName)
                                    {
                                        LinkMethods(mem.MemberFullName, me.MemberFullName);
                                    }
                                }
                            }

                        }



                    }
                }
            }
        }

        private void LinkMethods(string fromMethod, string toMethod)
        {
            Client.Cypher
                .Match("(fromMember:Member)", "(toMember:Member)")
                .Where((Member toMember) => toMember.MemberFullName == toMethod)
                .AndWhere((Member fromMember) => fromMember.MemberFullName == fromMethod)
                .CreateUnique("fromMember-[:METHOD_CALLS]->toMember")
                .ExecuteWithoutResults();
        }
    }
}