using System.IO;
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
            var member = GetNodeForClass.GetSyntaxTreeForFileClass(fileClass);
            if (member != null)
            {
                foreach (var method in member.Methods)
                {
                    GetMethodsInClassAndCreateRelationship(fileClass, method);
                }
                foreach (var field in member.Fields)
                {
                    CreateFieldInClassAndCreateRelationship(fileClass, field);
                }
            }
        }

        private void CreateFieldInClassAndCreateRelationship(FileClass fileClass, IUnresolvedField method)
        {
            var member = new Field
            {
                FileName = method.BodyRegion.FileName,
                MemberType = method.SymbolKind.ToString(),
                ReturnType = method.ReturnType.ToString(),
                MemberName = method.Name,
                MemberFullName = method.ReflectionName,
                Accessibility = method.Accessibility.ToString(),
                Class = fileClass.FullClassName,
                File = fileClass.File
            };

            var referencedClasses = _classRepository.GetClassesReferencedBy(fileClass);
            var referencedClass = referencedClasses.SingleOrDefault(x => x.ClassName == method.ReturnType.ToString());

            _memberRepository.CreateOrMergeField(member);
            _memberRepository.CreateFieldAndFileClassRelationship(fileClass, member);
            if (referencedClass != null)
            {
                _memberRepository.CreateIsOfTypeClassRelationship(member, referencedClass);
            }
        }

        private void GetMethodsInClassAndCreateRelationship(FileClass fileClass, IUnresolvedMethod method)
        {
            var member = new Member
            {
                FileName = method.BodyRegion.FileName,
                MemberType = method.SymbolKind.ToString(),
                ReturnType = method.ReturnType.ToString(),
                MemberName = GetMemberName(method.Name, fileClass.ClassName),
                MemberFullName = GetMemberFullName(method.Name, method.ReflectionName, fileClass.FullClassName, fileClass.ClassName),
                Accessibility = method.Accessibility.ToString(),
                Class = fileClass.FullClassName,
                File = fileClass.File
            };

            _memberRepository.CreateOrMergeMember(member);
            _memberRepository.CreateMemberAndFileClassRelationship(fileClass, member);
        }

        private string GetMemberName(string name, string className)
        {
            if (name == ".ctor")
            {
                return className;
            }
            return name;
        }

        private string GetMemberFullName(string name, string fullName, string fullClassName, string className)
        {
            if (name == ".ctor")
            {
                return fullClassName + "." + className;
            }
            return fullName;
        }
    }
}