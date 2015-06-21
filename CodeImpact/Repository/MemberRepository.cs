using System;
using System.Collections.Generic;
using System.Linq;
using CodeImpact.Model;
using Neo4jClient;

namespace CodeImpact.Repository
{
    public class MemberRepository
    {
        private static GraphClient Client { get; set; }

        public MemberRepository()
        {
            Client = new GraphClient(new Uri("http://neo4j:metead@localhost.:7474/db/data"));
            Client.Connect();
        }

        public void CreateMemberImplementedRelationship(Member interfaceMethod, Member classMethod)
        {
            Client.Cypher
                .Match("(fromMember:Member)", "(toMember:Member)")
                .Where((Member toMember) => toMember.MemberFullName == classMethod.MemberFullName)
                .AndWhere((Member fromMember) => fromMember.MemberFullName == interfaceMethod.MemberFullName)
                .CreateUnique("fromMember-[:IMPLEMENTED_BY]->toMember")
                .ExecuteWithoutResults();
        }

        public List<Member> GetMembersFromClass(FileClass fileClass)
        {
            var methods = Client.Cypher
                .Match("(member:Member)-[BELONGS_TO]->(mainFile:Class)")
                .Where((FileClass mainFile) => mainFile.FullClassName == fileClass.FullClassName)
                .Return(member => member.As<Member>())
                .Results
                .ToList();
            return methods;
        }

        public void CreateOrMergeMember(Member member)
        {
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
        }

        public void CreateMemberAndFileClassRelationship(FileClass fileClass, Member member)
        {
            Client.Cypher
                .Match("(fromMember:Member)", "(toFile:Class)")
                .Where((FileClass toFile) => toFile.FullClassName == fileClass.FullClassName)
                .AndWhere((Member fromMember) => fromMember.MemberFullName == member.MemberFullName)
                .CreateUnique("fromMember-[:BELONGS_TO]->toFile")
                .ExecuteWithoutResults();
        }
    }
}