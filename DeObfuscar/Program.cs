using System;
using System.IO;
using System.Text;
using System.Threading;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace DeObfuscar {
    class Program {
        internal static string ClassName;
        internal static ModuleDefMD module;
        internal static byte[] StringStorage;

        static void Main(string[] args) {
            try {
                module = ModuleDefMD.Load(args[0]);
                if (GetArray()) {
                    PrepareArray();

                    foreach (var type in module.GetTypes()) {
                        if (!type.HasMethods)
                            continue;

                        foreach (var method in type.Methods) {
                            if (!method.HasBody)
                                continue;

                            for (var i = 0; i < method.Body.Instructions.Count; i++) {
                                if (method.Body.Instructions[i].OpCode == OpCodes.Call &&
                                    method.Body.Instructions[i].Operand.ToString().Contains(ClassName)) {

                                    var decMethod = (MethodDef)method.Body.Instructions[i].Operand;
                                    if (decMethod.Body.Instructions.Count == 11) {
                                        method.Body.Instructions[i].OpCode = OpCodes.Ldstr;
                                        method.Body.Instructions[i].Operand = Decrypt(decMethod.Body.Instructions[7].GetLdcI4Value(), decMethod.Body.Instructions[8].GetLdcI4Value());
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    Console.WriteLine("Could not find array :(");
                    Thread.Sleep(3000);
                    Environment.Exit(0);
                }

                Console.WriteLine("Saving methods...");
                Save(args[0], module);
                Console.WriteLine("Saved!");
            }
            catch { }
        }

        internal static string Decrypt(int index, int count) => Encoding.UTF8.GetString(StringStorage, index, count);

        internal static void PrepareArray() {
            for (int i = 0; i < StringStorage.Length; i++)
                StringStorage[i] = (byte)(StringStorage[i] ^ i ^ 170);
        }

        internal static bool GetArray() {
            try {
                foreach (var type in module.GetTypes()) {
                    if (!type.HasMethods)
                        continue;

                    foreach (var method in type.Methods) {
                        if (!method.HasBody)
                            continue;

                        for (var i = 0; i < method.Body.Instructions.Count; i++) { //Has 33 instructions!!! (check if static or dynamic)
                            if (method.IsStatic &&
                                method.Body.Instructions[i].IsLdcI4() &&
                                method.Body.Instructions[i + 1].OpCode == OpCodes.Newarr &&
                                method.Body.Instructions[i + 1].Operand.ToString() == "System.String" &&
                                method.Body.Instructions[i + 2].OpCode == OpCodes.Stsfld &&
                                method.Body.Instructions[i + 3].IsLdcI4() &&
                                method.Body.Instructions[i + 4].OpCode == OpCodes.Newarr &&
                                method.Body.Instructions[i + 4].Operand.ToString() == "System.Byte" &&
                                method.Body.Instructions[i + 5].OpCode == OpCodes.Dup &&
                                method.Body.Instructions[i + 6].OpCode == OpCodes.Ldtoken &&
                                method.Body.Instructions[i + 7].OpCode == OpCodes.Call &&
                                method.Body.Instructions[i + 7].Operand.ToString() == "System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.RuntimeFieldHandle)" &&
                                method.Body.Instructions[i + 8].OpCode == OpCodes.Stsfld) {

                                ClassName = type.Name;
                                StringStorage = ((FieldDef)method.Body.Instructions[i + 6].Operand).InitialValue;

                                return true;
                            }
                        }
                    }
                }

                return false;
            } catch { return false; }
        }

        static void Save(string location, ModuleDefMD module) {

            Console.WriteLine("saving module!");

            var Writer = new NativeModuleWriterOptions(module, true) {
                KeepExtraPEData = true,
                KeepWin32Resources = true,
                Logger = DummyLogger.NoThrowInstance //prevents errors from being thrown
            };

            Writer.MetadataOptions.Flags = MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
            module.NativeWrite($"{Path.GetFileNameWithoutExtension(location)}-Dec{Path.GetExtension(location)}", Writer);

        }
    }
}

/* Made by DarkObb */