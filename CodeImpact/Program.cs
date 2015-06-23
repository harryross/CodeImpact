using CodeImpact.Controller;

namespace CodeImpact
{
    class Program
    {
        static void Main(string[] args)
        {
            var method = new PopulateDirectoryController();
            method.PopulateDictionary("D:\\Dev\\CodeImpact");
        }
    }



}
