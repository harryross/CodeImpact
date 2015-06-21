using System.Linq;
using CodeImpact.Helper;
using CodeImpact.Model;
using CodeImpact.Repository;
using ICSharpCode.NRefactory.TypeSystem;

namespace CodeImpact.Commands
{
    public class CreateMethodDirectory
    {
        private readonly ClassRepository _classRepository;
        private readonly MemberRepository _memberRepository;

        public CreateMethodDirectory()
        {
            _classRepository = new ClassRepository();
            _memberRepository = new MemberRepository();
        }

        public void CreateMethodDependanciesForFile()
        {
            var classes = _classRepository.GetAllClassesFromGraph();

            foreach (var c in classes)
            {
                GetMembersInClass(c);
            }
        }

        public void GetMemberReferencesForInterfaces()
        {
            var interfaces = _classRepository.GetAllInterfacesFromGraph();
            foreach (var @interface in interfaces)
            {
                GetInterfaceMemberImplentation(@interface);
            }
        }

        private void GetInterfaceMemberImplentation(FileClass @interface)
        {
            var implementedClass = _classRepository.GetSuperClassesOfInterface(@interface);
            var interfaceMethods = _memberRepository.GetMembersFromClass(@interface);
            foreach (var fileClass in implementedClass)
            {
                var classMethods = _memberRepository.GetMembersFromClass(fileClass);
                foreach (var classMethod in classMethods)
                {
                    var relationshipMethod =
                        interfaceMethods.SingleOrDefault(x => x.MemberName == classMethod.MemberName);
                    if (relationshipMethod != null)
                    {
                        _memberRepository.CreateMemberImplementedRelationship(relationshipMethod, classMethod);
                    }
                }
            }
        }

        private void GetMembersInClass(FileClass fileClass)
        {
            var tree = GetNodeForClass.GetSyntaxTree(fileClass);
            var member = tree.TopLevelTypeDefinitions.SingleOrDefault(x => x.ReflectionName == fileClass.FullClassName);
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

            _memberRepository.CreateOrMergeMember(member);
            _memberRepository.CreateMemberAndFileClassRelationship(fileClass, member);
            
        }
    }
}