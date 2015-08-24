using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using EventAttributes = Mono.Cecil.EventAttributes;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace IL_Rewriting
{
    class Program
    {
        static void Main()
        {
            var assemblyDefinition = AssemblyDefinition.ReadAssembly("IL_Rewriting.exe");
            var type = assemblyDefinition.Modules.Single(w => w.Types.Single(s => s.Name == "Person") != null).Types.Single(s => s.Name == "Person");
            type.Interfaces.Add(assemblyDefinition.MainModule.Import(typeof(INotifyPropertyChanged)));

            var propertyChangedFieldDefinition = new FieldDefinition("PropertyChanged"
                , FieldAttributes.Private
                , assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler)));

            type.Fields.Add(propertyChangedFieldDefinition);

            var propertyChangedEventDefinition = new EventDefinition("PropertyChanged"
                , EventAttributes.None
                , assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler)));

            var removeMethodDefinition = assemblyDefinition.MainModule.Import(typeof(Delegate).GetMethod("Remove",
                new[] { typeof(Delegate), typeof(Delegate) }));
            
            var removePropertyChanged = new MethodDefinition("remove_PropertyChanged", MethodAttributes.Public |
            MethodAttributes.SpecialName |
            MethodAttributes.NewSlot |
            MethodAttributes.HideBySig |
            MethodAttributes.Virtual |
            MethodAttributes.Final
            , assemblyDefinition.MainModule.Import(typeof(void)));

            removePropertyChanged.Overrides.Add(assemblyDefinition.MainModule.Import(typeof(INotifyPropertyChanged).GetMethod("remove_PropertyChanged")));
            removePropertyChanged.Parameters.Add(new ParameterDefinition(assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler))));
            var il = removePropertyChanged.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, propertyChangedFieldDefinition);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, removeMethodDefinition);
            il.Emit(OpCodes.Castclass, assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler)));
            il.Emit(OpCodes.Stfld, propertyChangedFieldDefinition);
            il.Emit(OpCodes.Ret);
            propertyChangedEventDefinition.RemoveMethod = removePropertyChanged;
            
            type.Methods.Add(removePropertyChanged);

            var addMethodDefinition = assemblyDefinition.MainModule.Import(typeof(Delegate).GetMethod("Combine",
                new[] { typeof(Delegate), typeof(Delegate) }));
            var addPropertyChanged  = new MethodDefinition("add_PropertyChanged", MethodAttributes.Public |
            MethodAttributes.SpecialName |
            MethodAttributes.NewSlot |
            MethodAttributes.HideBySig |
            MethodAttributes.Virtual |
            MethodAttributes.Final, assemblyDefinition.MainModule.Import(typeof(void)));
            addPropertyChanged.Parameters.Add(new ParameterDefinition(assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler))));
            addPropertyChanged.Overrides.Add(assemblyDefinition.MainModule.Import(typeof(INotifyPropertyChanged).GetMethod("add_PropertyChanged")));
            il = addPropertyChanged.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, propertyChangedFieldDefinition);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, addMethodDefinition);
            il.Emit(OpCodes.Castclass, assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler)));
            il.Emit(OpCodes.Stfld, propertyChangedFieldDefinition);
            il.Emit(OpCodes.Ret);
            propertyChangedEventDefinition.AddMethod = addPropertyChanged;
            type.Methods.Add(addPropertyChanged);

            type.Events.Add(propertyChangedEventDefinition);
            var propertyChanged = new MethodDefinition("OnPropertyChanged",
                MethodAttributes.Virtual | MethodAttributes.Public, assemblyDefinition.MainModule.Import(typeof(void)));


            assemblyDefinition.Write("t1.dll");
            //var ilProcessor = propertyChanged.Body.GetILProcessor();
            //var localVariableDefinition = assemblyDefinition.MainModule.Import(typeof (bool));

            //ilProcessor.Emit(OpCodes.Nop);
            //propertyChanged.Body.Variables.Add(new VariableDefinition(localVariableDefinition));

        }
    }
}
