using System;
using System.Collections.Generic;
using System.Linq;
using CodeImpact.Model;
using CodeImpact.Model.Contract;
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
        public List<Field> GetFieldsFromClass(FileClass fileClass)
        {
            var methods = Client.Cypher
                .Match("(member:Field)-[BELONGS_TO]->(mainFile:Class)")
                .Where((FileClass mainFile) => mainFile.FullClassName == fileClass.FullClassName)
                .Return(member => member.As<Field>())
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

        public void CreateFieldAndFileClassRelationship(FileClass fileClass, Field member)
        {
            Client.Cypher
                .Match("(fromMember:Field)", "(toFile:Class)")
                .Where((FileClass toFile) => toFile.FullClassName == fileClass.FullClassName)
                .AndWhere((Field fromMember) => fromMember.MemberFullName == member.MemberFullName)
                .CreateUnique("fromMember-[:BELONGS_TO]->toFile")
                .ExecuteWithoutResults();
        }

        public void CreateOrMergeField(Field field)
        {
            Client.Cypher
                .Merge("(field:Field { MemberFullName: {memberFullName}})")
                .OnCreate()
                .Set("field = {field}")
                .WithParams(new
                {
                    memberFullName = field.MemberFullName,
                    field
                })
                .ExecuteWithoutResults();
        }

        public void CreateIsOfTypeClassRelationship(Field member, FileClass referencedClass)
        {
            Client.Cypher
                .Match("(fromMember:Field)", "(toFile:Class)")
                .Where((FileClass toFile) => toFile.FullClassName == referencedClass.FullClassName)
                .AndWhere((Field fromMember) => fromMember.MemberFullName == member.MemberFullName)
                .CreateUnique("fromMember-[:OF_TYPE]->toFile")
                .ExecuteWithoutResults();
        }

        public void CreateMemberRelationship(Member fromMember, IMember toMember)
        {
            if (toMember.GetType() == typeof (Member))
            {
                Client.Cypher
                    .Match("(fromMem:Member)", "(toMem:Member)")
                    .Where((Member toMem) => toMem.MemberFullName == toMember.MemberFullName)
                    .AndWhere((Member fromMem) => fromMem.MemberFullName == fromMember.MemberFullName)
                    .CreateUnique("fromMem-[:CALLS_TO]->toMem")
                    .ExecuteWithoutResults();
            }
            else if (toMember.GetType() == typeof (Field))
            {
                Client.Cypher
                    .Match("(fromMem:Member)", "(toMem:Field)")
                    .Where((Field toMem) => toMem.MemberFullName == toMember.MemberFullName)
                    .AndWhere((Member fromMem) => fromMem.MemberFullName == fromMember.MemberFullName)
                    .CreateUnique("fromMem-[:USES]->toMem")
                    .ExecuteWithoutResults();
            }
        }
    }
}