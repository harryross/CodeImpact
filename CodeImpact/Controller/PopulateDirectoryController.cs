using System;
using System.Collections.Generic;
using System.IO;
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
            var validEntries = new List<string>();
            foreach (var entry in entries)
            {
                if (entry.EndsWith(".cs"))
                {
                    validEntries.Add(entry);
                    Console.WriteLine(entry);
                }
            }
            var createCommand = new CreateFileStructureCommand();
            createCommand.WriteList(validEntries);
        }
    }
}