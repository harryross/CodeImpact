using System;
using System.Collections.Generic;
using System.Linq;
using CodeImpact.Helper;
using CodeImpact.Model;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Cecil;
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

        public void GetMemberReferencesForInterfaces()
        {
            Client.Connect();
            var interfaces = GetAllInterfaces();
            foreach (var @interface in interfaces)
            {
                GetInterfaceMemberImplentation(@interface);
            }
        }

        private void GetInterfaceMemberImplentation(FileClass @interface)
        {
            var implementedClass = GetImplementationsOfInterface(@interface);
            var interfaceMethods = GetMethodsFromFileClass(@interface);
            foreach (var fileClass in implementedClass)
            {
                var classMethods = GetMethodsFromFileClass(fileClass);
                foreach (var classMethod in classMethods)
                {
                    var relationshipMethod =
                        interfaceMethods.SingleOrDefault(x => x.MemberName == classMethod.MemberName);
                    if (relationshipMethod != null)
                    {
                        CreateMethodImplentation(relationshipMethod, classMethod);
                    }
                }
            }
        }

        private void CreateMethodImplentation(Member interfaceMethod, Member classMethod)
        {
            Client.Cypher
                .Match("(fromMember:Member)", "(toMember:Member)")
                .Where((Member toMember) => toMember.MemberFullName == classMethod.MemberFullName)
                .AndWhere((Member fromMember) => fromMember.MemberFullName == interfaceMethod.MemberFullName)
                .CreateUnique("fromMember-[:IMPLEMENTED_BY]->toMember")
                .ExecuteWithoutResults();
        }

        private IEnumerable<Member> GetMethodsFromFileClass(FileClass fileClass)
        {
            var methods = Client.Cypher
                .Match("(member:Member)-[BELONGS_TO]->(mainFile:Class)")
                .Where((FileClass mainFile) => mainFile.FullClassName == fileClass.FullClassName)
                .Return(member => member.As<Member>())
                .Results
                .ToList();
            return methods;
        }

        public IEnumerable<FileClass> GetAllInterfaces()
        {
            var interfaces = Client.Cypher
                .Match("(_interface:Class)")
                .Where((FileClass _interface) => _interface.Kind.ToString() == TypeKind.Interface.ToString())
                .Return(_interface => _interface.As<FileClass>())
                .Results.ToList();
            return interfaces;
        } 
        

        private void GetMembersInClass(FileClass fileClass)
        {
            var tree = GetNodeForClass.GetSyntaxTree(fileClass);
            IEnumerable<FileClass> implementations = null;
            if (fileClass.Kind == TypeKind.Interface)
            {
                implementations = GetImplementationsOfInterface(fileClass);
            }
            var member = tree.TopLevelTypeDefinitions.SingleOrDefault(x => x.ReflectionName == fileClass.FullClassName);
            if (member != null)
            {
                foreach (var method in member.Methods)
                {
                    GetMethodsInClassAndCreateRelationship(fileClass, method);
                }
            }
        }

        public IEnumerable<FileClass> GetImplementationsOfInterface(FileClass fileClass)
        {
            var classes = Client.Cypher
                .Match("(_class:Class)-[:BASE_TYPE*]->(baseType:Class)")
                .Where((FileClass baseType) => baseType.FullClassName == fileClass.FullClassName)
                .Return(_class => _class.As<FileClass>())
                .Results.ToList();
            return classes;
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