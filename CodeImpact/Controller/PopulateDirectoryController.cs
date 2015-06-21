using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeImpact.Commands;

namespace CodeImpact.Controller
{
    public class PopulateDirectoryController
    {
        public void PopulateDictionary(string path)
        {
            ListAllDirectories(path);
        }


        private void ListAllDirectories(string path)
        {
            var entries = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            var validEntries = entries.Where(entry => entry.EndsWith(".cs")).ToList();
            validEntries.RemoveAll(x => x.Contains("bin") || x.Contains("debug"));
            var createCommand = new CreateFileStructureCommand();
            createCommand.WriteList(validEntries);
            var createMethod = new CreateMethodDirectory();
            var member = new MemberRelationships();

            createMethod.CreateMethodDependanciesForFile();
            createMethod.GetMemberReferencesForInterfaces();
            member.CreateClassFields();

            /*var methods = new CreateMethodRelationships();
            methods.CreateMethodRelationshipsAll(validEntries);
            methods.GetMethodCallsToOtherFiles(validEntries);*/
        }
    }
}