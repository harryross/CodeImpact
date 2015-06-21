using System.Collections.Generic;

namespace CodeImpact.Model.Contract
{
    public interface IMember
    {
        string MemberName { get; set; }
        string MemberFullName { get; set; }
        string ReturnType { get; set; }
        string File { get; set; }
        string MemberType { get; set; }
        string Accessibility { get; set; }
        List<string> Parameters { get; set; }
        string Class { get; set; }
        string FileName { get; set; }
    }
}