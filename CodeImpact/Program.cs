using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImpact.Controller;

namespace CodeImpact
{
    class Program
    {
        static void Main(string[] args)
        {
            var method = new PopulateDirectoryController();
            method.PopulateDictionary("D:\\Dev\\Hogwarts\\project");
            Console.ReadKey();
        }
    }



}
