using System.Collections.Generic;
using System.Linq;
using CodeImpact.Helper;
using CodeImpact.Model;
using CodeImpact.Model.Contract;
using CodeImpact.Repository;
using ICSharpCode.NRefactory.CSharp;

namespace CodeImpact.Commands
{
    public class MemberRelationships
    {
        private readonly ClassRepository _classRepository;
        private readonly MemberRepository _memberRepository;

        public MemberRelationships()
        {
            _classRepository = new ClassRepository();
            _memberRepository= new MemberRepository();
        }

        public void CreateClassFields()
        {
            var allClasses = _classRepository.GetAllClassesFromGraph();
            foreach (var @class in allClasses)
            {
                GetMemberCalls(@class);
            }
        }

        private void GetMemberCalls(FileClass fileClass)
        {
            var classes = _classRepository.GetClassesReferencedBy(fileClass);
            classes.AddRange(_classRepository.GetClassBaseTypes(fileClass));
            classes.Add(fileClass);
            var members = new List<IMember>();
            foreach (var @class in classes)
            {
                members.AddRange(_memberRepository.GetMembersFromClass(@class));
                members.AddRange(_memberRepository.GetFieldsFromClass(@class));
            }
            var classMembers = _memberRepository.GetMembersFromClass(fileClass);
            foreach (var member in classMembers)
            {
                var nodes = GetNodeForClass.GetTopNodeForMember(member);
                if (nodes != null)
                {
                    var fieldsInMember = nodes.Descendants.Where(x => x.Role == Roles.Identifier && x.NodeType == NodeType.Token);
                    foreach (var field in fieldsInMember)
                    {
                        var tempMember =
                            members.Where(
                                x => x.MemberName == field.ToString() && member.MemberName != field.ToString());
                        foreach (var tm in tempMember)
                        {
                            _memberRepository.CreateMemberRelationship(member, tm);
                        }
                    }
                }
            }
        }
    }
}