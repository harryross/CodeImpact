using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeImpact.Helper;
using CodeImpact.Model;
using CodeImpact.Repository;
using ICSharpCode.NRefactory.CSharp;

namespace CodeImpact.Commands
{
    class CreateFileStructureCommand
    {
        private SyntaxTree _syntaxTree;
        private readonly ClassRepository _classRepository;

        public CreateFileStructureCommand()
        {
            _classRepository = new ClassRepository();
        }

        public void WriteList(List<string> list)
        {
            UpdateDatabase(list);
            CreateClassRelationships();
        }

        private void CreateClassRelationships()
        {
            var classes = _classRepository.GetAllClassesFromGraph();

            foreach (var c in classes)
            {
                AstNode theNode = GetNodeForClass.GetTopNodeForClass(c);
                if (theNode != null)
                {
                    var hasBaseType =
                        theNode.Descendants.Where(x => x.NodeType == NodeType.TypeReference && x.Role == Roles.BaseType)
                            .ToList();
                    foreach (var bt in hasBaseType)
                    {
                        if (bt.ToString() != c.ClassName)
                        {
                            var member1 = bt;
                            var name = classes.Where(x => x.ClassName == member1.ToString()).ToList();
                            if (name.Count != 1)
                            {
                                continue;
                            }
                            _classRepository.CreateBaseTypeRelationship(c, name.First());
                        }
                        
                    }
                    var tree = theNode.Descendants.Where(x => x.Role == Roles.Identifier && (x.Parent.Role == Roles.Type) && x.ToString() != "var").Distinct().ToList();
                    foreach (var member in tree)
                    {
                        if (member.ToString() != c.ClassName)
                        {
                            var member1 = member;
                            var name = classes.Where(x => x.ClassName == member1.ToString()).ToList();
                            if (name.Count != 1)
                            {
                                continue;
                            }
                            _classRepository.CreateClassReferencesRelationship(c, name.First());
                        }
                    }
                }
            }
        }


        private void UpdateDatabase(List<string> list)
        {
            foreach (var file in list)
            {
                var results = GetClassesForFile(file);
                foreach (var fileClass in results)
                {
                    
                    _classRepository.CreateOrMergeClass(fileClass);
                }
                
            }
        }

        private List<FileClass> GetClassesForFile(string fileName)
        {
            var fileText = File.ReadAllText(fileName);
            _syntaxTree = new CSharpParser().Parse(fileText, Path.GetFileName(fileName));
            var tree = _syntaxTree.ToTypeSystem();
            var list = new List<FileClass>();
            foreach (var tld in tree.TopLevelTypeDefinitions)
            {
                var isFinalNode = tld.Name.ToLower().Contains("controller");

                var fileClass = new FileClass
                {
                    FullClassName = tld.ReflectionName,
                    ClassName = tld.Name,
                    File = fileName,
                    Type = FileType.CSharp,
                    Kind = tld.Kind,
                    IsWebsiteClas = isFinalNode
                };
                list.Add(fileClass);
            }
            return list;
        }
    }
}
