using System;
using System.Linq;
using Mono.Cecil;

namespace IL_Rewriting
{
    class Program
    {
        static void Main()
        {
            var assemblyDefinition=   AssemblyDefinition.ReadAssembly("IL_Rewriting.exe");
            var type = assemblyDefinition.Modules.Single(w => w.Types.Single(s => s.Name == "Person") != null).Types.Single(s=> s.Name=="Person");
            
        }
    }
}
