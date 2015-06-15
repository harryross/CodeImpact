﻿using System.IO;
using System.Linq;
using CodeImpact.Model;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace CodeImpact.Helper
{
    public static class GetNodeForClass
    {
        public static AstNode GetTopNodeForClass(FileClass fileClass)
        {
            SyntaxTree tree;
            AstNode theNode = null;
            var fileText = File.ReadAllText(fileClass.File);
            tree = new CSharpParser().Parse(fileText, Path.GetFileName(fileClass.File));
            var classTree =
                tree.Descendants.Where(
                    y => y.NodeType == NodeType.TypeDeclaration && y.Role == Role.GetByIndex(2));

            foreach (var ct in classTree)
            {
                var temp = ct.Descendants.First(x => x.Role == Roles.Identifier && x.NodeType == NodeType.Token);
                if (temp.ToString() == fileClass.ClassName)
                {
                    theNode = ct;
                    continue;
                }
            }
            return theNode;
        }

        public static CSharpUnresolvedFile GetSyntaxTree(FileClass fileClass)
        {
            SyntaxTree tree;
            var fileText = File.ReadAllText(fileClass.File);
            tree = new CSharpParser().Parse(fileText, Path.GetFileName(fileClass.File));
            return tree.ToTypeSystem();
            
        }
    }
}