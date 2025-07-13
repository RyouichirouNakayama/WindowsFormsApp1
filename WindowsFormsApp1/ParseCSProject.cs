using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using Mono.Cecil;
using System.Windows.Forms;
using System.Text;

namespace WindowsFormsApp1
{
    internal class ParseCSProject
    {
        internal void Start(string path_csproj)
        {
            XDocument doc = XDocument.Load(path_csproj);

            XNamespace ns = doc.Root.Name.Namespace;
            doc.Root.Name = doc.Root.Name.LocalName;
            doc.Root.ReplaceAttributes();
            foreach (var elem in doc.Descendants())
            {
                elem.Name = elem.Name.LocalName;
            }

            var groups = doc.Descendants("PropertyGroup");

            var first = groups.Where(n => n.Attribute("Condition") == null).FirstOrDefault();
            if (first == null)
            {
                return;
            }

            var debug_x64 = groups.Where(n => n.Attribute("Condition") != null)
                .Where(n => n.Attribute("Condition").Value.Contains("Debug|x64")).FirstOrDefault();
            if (debug_x64 == null)
            {
                return;
            }

            string ProjectGuid = first.Element("ProjectGuid").Value;
            string OutputType = first.Element("OutputType").Value;
            string RootNamespace = first.Element("RootNamespace").Value;
            string AssemblyName = first.Element("AssemblyName").Value;
            string TargetFrameworkVersion = first.Element("TargetFrameworkVersion").Value;
            string OutputPath = debug_x64.Element("OutputPath").Value;

            string topdirectory = Path.GetDirectoryName(path_csproj);
            string dir_assembly = Path.Combine(topdirectory, OutputPath);
            string extension = OutputType.ToLower().Contains("exe") ? ".exe" : ".dll";
            string filename_assembly = AssemblyName + extension;
            string path = Path.Combine(dir_assembly, filename_assembly);

            if (!File.Exists(path))
            {
                return;
            }       

            MyNameSpace root = new MyNameSpace();
            root._FullNameSpace = RootNamespace;
            root._FullNameSpace_RemoveDot = RootNamespace;
            root._Name = RootNamespace;

            try
            {
                AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(path);
                foreach (var type in asm.MainModule.Types)
                {
                    if (!type.FullName.StartsWith(RootNamespace))
                    {
                        continue;
                    }

                    string[] strs = type.Namespace.Split('.');
                    MyNameSpace myns = root.SearchNameSpace(type.Namespace);
                    if (myns == null)
                    {
                        MyNameSpace before_parent = root;
                        StringBuilder sb = new StringBuilder();
                        sb.Append(strs[0]);
                        for (int i = 1; i < strs.Length; i++)
                        {
                            sb.Append("." + strs[i]);
                            string full = sb.ToString();
                            myns = root.SearchNameSpace(full);
                            if (myns == null)
                            {
                                myns = new MyNameSpace();
                                myns._FullNameSpace = full;
                                myns._FullNameSpace_RemoveDot = full.Replace(".", "");
                                myns._Name = strs[i];
                                myns._parent = before_parent;
                                before_parent._childs.Add(myns);
                            }
                            before_parent = myns;
                        }
                    }
                    myns._types.Add(type);
                }

                string all = WindowsFormsApp1.Properties.Resource1.template;




                aa.init_sb();
                root._01_make_namespace_and_class();
                all = all.Replace("//namespace_and_class", aa.get_sb());

                aa.init_sb();
                root._02_make_fields();
                all = all.Replace("//filed", aa.get_sb());

                File.WriteAllText("replaced.js", all, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                { }
            }
            { }
        }

        void WriteLine(string arg)
        {
            Debug.WriteLine(arg);





            //foreach (var type in asm.MainModule.Types)
            //{
            //    if (!type.FullName.StartsWith(RootNamespace))
            //    {
            //        continue;
            //    }
            //    Console.WriteLine("");
            //    Console.WriteLine("------------" + type.FullName + "------------");

            //    Console.WriteLine("------------フィールド------------");
            //    foreach (var field in type.Fields)
            //    {
            //        if (field.Name.EndsWith("k__BackingField"))
            //        {
            //            continue;
            //        }

            //        var ss = field.FieldType;

            //        Console.WriteLine(
            //     $"{field.FieldType.Name}:{field.Name}");
            //    }


            //    Console.WriteLine("------------プロパティ------------");
            //    foreach (var property in type.Properties)
            //    {
            //        TypeReference typeReference = property.PropertyType;
            //        string kata = typeReference.Name;
            //        if (typeReference is GenericInstanceType)
            //        {
            //            GenericInstanceType gene = typeReference as GenericInstanceType;

            //            string name = gene.Name.Substring(0, gene.Name.IndexOf('`'));//ReactiveProperty`1

            //            var ss = gene.GenericArguments.Select(n => n.Name)
            //                .Aggregate((a, b) => a + "," + b);

            //            kata = $"{name}<{ss}>";
            //            { }
            //        }

            //        Console.WriteLine($"{kata}:{property.Name}");
            //    }

            //    Console.WriteLine("------------メソッド------------");
            //    foreach (var method in type.Methods)
            //    {
            //        if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
            //        {
            //            continue;
            //        }
            //        //明示的な宣言があろうがなかろうが.ctorは存在する。
            //        if (method.Name.Contains(".ctor") || method.Name.Contains(".cctor"))
            //        {
            //            continue;
            //        }

            //        MethodAttributes att = method.Attributes;
            //        att = att & ~MethodAttributes.HideBySig;
            //        string sss = att.ToString().Replace("CompilerControlled, ", "");
            //        Console.WriteLine(
            //            $"{sss} {method.ReturnType.Name} {method.Name}");
            //    }
            //}
        }



    }
}
