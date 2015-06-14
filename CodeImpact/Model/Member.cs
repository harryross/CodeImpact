using System.Collections.Generic;

namespace CodeImpact.Model
{
    public class Member
    {
        public string MemberName { get; set; }
        public string MemberFullName { get; set; }
        public string ReturnType { get; set; }
        public string File { get; set; }
        public string MemberType { get; set; }
        public string Accessibility { get; set; }
        public List<string> Parameters { get; set; }
    }


    public enum MemberType
    {
        Method,
        Constructor,
        Field,
        Property
    }
}