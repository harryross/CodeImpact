using System.Collections.Generic;
using CodeImpact.Helper;
using CodeImpact.Model;
using CodeImpact.Repository;

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
            var members = new List<Member>();
            foreach (var @class in classes)
            {
                members.AddRange(_memberRepository.GetMembersFromClass(@class));
            }
            var classMembers = _memberRepository.GetMembersFromClass(fileClass);
            foreach (var member in classMembers)
            {
                var nodes = GetNodeForClass.GetTopNodeForMember(member);

            }
        }

        
    }
}