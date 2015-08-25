using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Fclp;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace IL_Rewriting
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new FluentCommandLineParser<ApplicationArguments>();
            p.Setup(appArg => appArg.FileName).As('f', "fileName");
            var result = p.Parse(args);
            if(result.HasErrors)throw new ArgumentException(string.Join(",",args));

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(p.Object.FileName);

            foreach (var moduleDefinition in assemblyDefinition.Modules)
            {
                foreach (var typeDefinition in moduleDefinition.Types)
                {
                    if(typeDefinition.CustomAttributes.Contains(new CustomAttribute(assemblyDefinition.MainModule.Import(typeof(NotifyPropertyChanged).GetConstructor(Type.EmptyTypes)))))
                    {
                        ImplementINotifyPropertyChanged(assemblyDefinition, typeDefinition);
                    }
                }
            }

            assemblyDefinition.Write(p.Object.FileName,new WriterParameters(){WriteSymbols = true});

        }

        private static void ImplementINotifyPropertyChanged(AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition)
        {
            typeDefinition.Interfaces.Add(assemblyDefinition.MainModule.Import(typeof(INotifyPropertyChanged)));

            var propertyChangedFieldDefinition = new FieldDefinition("PropertyChanged"
                , FieldAttributes.Private
                , assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler)));

            typeDefinition.Fields.Add(propertyChangedFieldDefinition);

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
            typeDefinition.Methods.Add(removePropertyChanged);

            var addMethodDefinition = assemblyDefinition.MainModule.Import(typeof(Delegate).GetMethod("Combine",
                new[] { typeof(Delegate), typeof(Delegate) }));
            var addPropertyChanged = new MethodDefinition("add_PropertyChanged", MethodAttributes.Public |
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
            typeDefinition.Methods.Add(addPropertyChanged);

            typeDefinition.Events.Add(propertyChangedEventDefinition);

            var propertyChanged = new MethodDefinition("OnPropertyChanged",
                MethodAttributes.Virtual | MethodAttributes.Public, assemblyDefinition.MainModule.Import(typeof(void)));
            propertyChanged.Parameters.Add(new ParameterDefinition("propertyName", ParameterAttributes.None, assemblyDefinition.MainModule.Import(typeof(string)))
            {
                CustomAttributes =
                {
                    new CustomAttribute(assemblyDefinition.MainModule.Import(typeof(CallerMemberNameAttribute).GetConstructor(Type.EmptyTypes)))
                },
                Attributes = ParameterAttributes.Optional
            });
            var ilProcessor = propertyChanged.Body.GetILProcessor();
            var localVariableDefinition = assemblyDefinition.MainModule.Import(typeof(bool));
            propertyChanged.Body.Variables.Add(new VariableDefinition(localVariableDefinition));
            var returnLabel = Instruction.Create(OpCodes.Nop);
            ilProcessor.Emit(OpCodes.Nop);
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldfld, propertyChangedFieldDefinition);
            ilProcessor.Emit(OpCodes.Ldnull);
            ilProcessor.Emit(OpCodes.Cgt_Un);
            ilProcessor.Emit(OpCodes.Stloc_0);
            ilProcessor.Emit(OpCodes.Ldloc_0);
            ilProcessor.Emit(OpCodes.Brfalse_S, returnLabel);
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldfld, propertyChangedFieldDefinition);
            ilProcessor.Emit(OpCodes.Ldarg_0);
            ilProcessor.Emit(OpCodes.Ldarg_1);
            ilProcessor.Emit(OpCodes.Newobj, assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventArgs).GetConstructor(new[] { typeof(string) })));
            ilProcessor.Emit(OpCodes.Callvirt, assemblyDefinition.MainModule.Import(typeof(PropertyChangedEventHandler).GetMethod("Invoke")));
            ilProcessor.Append(returnLabel);
            ilProcessor.Emit(OpCodes.Ret);

            typeDefinition.Methods.Add(propertyChanged);
            foreach (var propertyDefinition in typeDefinition.Properties)
            {
                var fieldName = propertyDefinition.Name.ToCamelCase();
                var fieldDefinition = new FieldDefinition(fieldName, FieldAttributes.Private, propertyDefinition.PropertyType);
                typeDefinition.Fields.Add(fieldDefinition);
                propertyDefinition.GetMethod.Body.Instructions.Clear();
                il = propertyDefinition.GetMethod.Body.GetILProcessor();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, fieldDefinition);
                il.Emit(OpCodes.Ret);

                propertyDefinition.SetMethod.Body.Instructions.Clear();
                il = propertyDefinition.SetMethod.Body.GetILProcessor();
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, fieldDefinition);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, propertyDefinition.Name);
                il.Emit(OpCodes.Call, propertyChanged);
                il.Emit(OpCodes.Nop);
                il.Emit(OpCodes.Ret);
            }
        }
    }
}
