using System.Collections.Generic;
using CodeImpact.Model.Contract;

namespace CodeImpact.Model
{
    public class Field : IMember
    {
        public string MemberName { get; set; }
        public string MemberFullName { get; set; }
        public string ReturnType { get; set; }
        public string File { get; set; }
        public string MemberType { get; set; }
        public string Accessibility { get; set; }
        public List<string> Parameters { get; set; }
        public string Class { get; set; }
        public string FileName { get; set; }
    }


    public enum FieldType
    {
        Method,
        Constructor,
        Field,
        Property
    }
}