using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using Mono.Cecil;
using System.Windows.Forms;
using System.Text;
using System.Web;

namespace WindowsFormsApp1
{
    class clsSet
    {
        internal string _01name = "";
        internal string _02type = "";
        internal string _03modifier = "";
    }

    class AstahBase
    {
        internal string _可視性 = "public";
        internal string _ステレオタイプ = "";
        internal AstahClass parent = null;
        internal clsSet nameset = new clsSet();

        internal bool isStatic = false;
        internal bool isReadOnly = false;
    }    

    class AstahClass : AstahBase
    {
        internal bool isInterface = false;
        internal string _基底クラス名 = "";
        internal List<string> _interfaces = new List<string>();


        internal List<AstahAttribute> attributes = new List<AstahAttribute>();
        internal List<AstahOperation> opes = new List<AstahOperation>();

        internal string _namespace = "";
        internal string _クラス名 = "";        

        internal string get_var()
        {
            string fullname = bb.get_namespace_var(_namespace) + "_" + _クラス名;
            return $"c_{fullname}";
        }
        public override string ToString() { return _クラス名; }
    }

    class AstahAttribute : AstahBase
    {
        internal string get_var()
        {
            string fullname = bb.get_namespace_var(parent._namespace) + "_" + parent._クラス名
                + "_" + nameset._01name;

            return $"f_{fullname}";
        }        

        public override string ToString() { return nameset._01name; }
    }

    class AstahOperation : AstahBase
    {
        internal string get_var()
        {
            string fullname = bb.get_namespace_var(parent._namespace) + "_" + parent._クラス名
                + "_" + nameset._01name;

            return $"m_{fullname}";
        }
        internal List<clsSet> paras = new List<clsSet>();
        public override string ToString() { return nameset._01name; }
    }      

    internal class bb
    {
        internal static string get_namespace_var(string _namespace)
        {
            return _namespace.Replace(".", "");
        }

        internal void start2(string path_csproj)
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

            Start(path);
        }


        string get_可視性(PropertyDefinition pro)
        {
            //getterとsetterで可視性を設定できるがおそらくアスターにはない
            string visi = "public";
            if (pro.GetMethod != null && pro.GetMethod.IsPublic)
            {
                visi = "public";
            }
            else if (pro.SetMethod != null && pro.SetMethod.IsPublic)
            {
                visi = "public";
            }

            if (pro.GetMethod != null && pro.GetMethod.IsAssembly)
            {
                visi = "package";
            }
            else if (pro.SetMethod != null && pro.SetMethod.IsAssembly)
            {
                visi = "package";
            }

            if (pro.GetMethod != null && pro.GetMethod.IsFamily)
            {
                visi = "protected";
            }
            else if (pro.SetMethod != null && pro.SetMethod.IsFamily)
            {
                visi = "protected";
            }

            if (pro.GetMethod != null && pro.GetMethod.IsPrivate)
            {
                visi = "private";
            }
            else if (pro.SetMethod != null && pro.SetMethod.IsPrivate)
            {
                visi = "private";
            }
            return visi;
        }

        bool get_Static(PropertyDefinition pro)
        {
            //getterとsetterで可視性を設定できるがおそらくアスターにはない
            bool isStatic = false;
            if (pro.GetMethod != null && pro.GetMethod.IsStatic)
            {
                isStatic = true;
            }
            else if (pro.SetMethod != null && pro.SetMethod.IsStatic)
            {
                isStatic = true;
            }
            return isStatic;
        }

        void Start(string path)
        {       
            try
            {
                AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(path);

                foreach (TypeDefinition type in asm.MainModule.Types)
                {
                    if (type.Namespace == "")
                    {
                        continue;//空文字の名前空間があり、見たことないクラスとかがある。
                    }

                    get_class(type);

                    foreach (FieldDefinition field in type.Fields)
                    {
                        gets(field.FieldType);
                    }

                    foreach (PropertyDefinition prop in type.Properties)
                    {
                        gets(prop.PropertyType);
                    }

                    foreach (MethodDefinition method in type.Methods)
                    {
                        gets(method.ReturnType);

                        foreach (ParameterDefinition param in method.Parameters)
                        {
                            gets(param.ParameterType);
                        }
                    }
                }


                foreach (TypeDefinition type in asm.MainModule.Types)
                {
                    if (type.Namespace == "")
                    {
                        continue;//空文字の名前空間があり、見たことないクラスとかがある。
                    }

                    AstahClass cls = get_class(type);
                    if (cls == null)
                    {
                        continue;
                    }                    

                    foreach (FieldDefinition field in type.Fields)
                    {
                        if (field.Name.EndsWith("k__BackingField"))//プロパティの内部フィールドと思われる
                        {
                            continue;
                        }

                        AstahAttribute attr = new AstahAttribute();
                        attr.parent = cls;
                        attr.nameset._01name = field.Name;
                        string visi = "private";
                        if (field.IsPublic)
                        {
                            visi = "public";
                        }
                        else if (field.IsFamily)
                        {
                            visi = "protected";
                        }
                        else if (field.IsAssembly)
                        {
                            visi = "package";
                        }
                        attr._可視性 = visi;
                        attr.isReadOnly = field.IsInitOnly;
                        attr.isStatic = field.IsStatic;

                        var rtn = get_type_and_modifier(field.FieldType);
                        attr.nameset._02type = rtn.Item1;
                        attr.nameset._03modifier = rtn.Item2;

                        cls.attributes.Add(attr);
                    }

                    foreach (PropertyDefinition pro in type.Properties)
                    {
                        AstahAttribute attr = new AstahAttribute();
                        attr.parent = cls;
                        attr.nameset._01name = pro.Name;
                        attr._可視性 = get_可視性(pro);
                        attr.isStatic= get_Static(pro);

                        var rtn = get_type_and_modifier(pro.PropertyType);
                        attr.nameset._02type = rtn.Item1;
                        attr.nameset._03modifier = rtn.Item2;
                        attr._ステレオタイプ = "property";
                        cls.attributes.Add(attr);
                    }

                    foreach (MethodDefinition method in type.Methods)
                    {
                        bool isConstructor = false;
                        string name_method = method.Name;
                        if (method.Name == ".ctor")
                        {
                            name_method = type.Name;
                            isConstructor = true;
                        }                       
                        else if (name_method.Contains("."))//詳細不明
                        {
                            //MainWindowクラスに
                            //System.Windows.Markup.IComponentConnector.Connectがある                      
                            continue;
                        }

                        if (name_method.Contains("<"))//詳細不明
                        {
                            continue;
                        }

                        //同名のプロパティがあるか確認してもよい
                        if (name_method.StartsWith("get_") || name_method.StartsWith("set_"))
                        {
                            continue;
                        }
                        if (name_method.StartsWith("add_") || name_method.StartsWith("remove_"))
                        {
                            continue;
                        }

                        AstahOperation ope = new AstahOperation();
                        ope.parent = cls;                                                  

                        string visibility = "public";
                        if ((method.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                        {
                            visibility = "public";
                        }
                        else if ((method.Attributes & MethodAttributes.Assembly) == MethodAttributes.Assembly)
                        {
                            visibility = "package";
                        }
                        else if ((method.Attributes & MethodAttributes.Family) == MethodAttributes.Family)
                        {
                            visibility = "protected";
                        }
                        else if ((method.Attributes & MethodAttributes.Private) == MethodAttributes.Private)
                        {
                            visibility = "private";
                            if (isConstructor)
                            {
                                continue;
                            }
                        }
                                              
                        ope._可視性 = visibility;
                        ope.isStatic = method.IsStatic;

                        var rtn = get_type_and_modifier(method.ReturnType);
                        ope.nameset._01name = name_method;
                        ope.nameset._02type = rtn.Item1;
                        ope.nameset._03modifier = rtn.Item2;

                        foreach (ParameterDefinition param in method.Parameters)
                        {
                            var get2 = get_type_and_modifier(param.ParameterType);
                            clsSet newset = new clsSet();
                            newset._01name = param.Name;
                            newset._02type = get2.Item1;
                            newset._03modifier = get2.Item2;
                            ope.paras.Add(newset);
                        }
                        cls.opes.Add(ope);
                    }
                }

                string all = get_template();

                //①名前空間(フォルダ)の作成
                aa.clear_sb();
                foreach (var va in g_dicNS_Class)
                {
                    string _namespace = va.Key;
                    string[] strs = _namespace.Split('.');
                    string ns_parent = "myProject";
                    if (strs.Length > 1)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < strs.Length - 1; i++)
                        {
                            sb.Append(strs[i]);
                            string temp_ns = sb.ToString();
                            if (!g_dicNS_and_Var.ContainsKey(temp_ns))
                            {
                                make(temp_ns, ns_parent);
                            }
                            ns_parent = get_namespace_var(sb.ToString());
                            sb.Append(".");
                        }
                    }
                    make(_namespace, ns_parent);
                }

                all = all.Replace("//namespace", aa.get_sb());

                //②クラスの作成
                aa.clear_sb();
                foreach (var va in g_dicNS_Class)
                {
                    string var_ns = "";
                    if (!g_dicNS_and_Var.TryGetValue(va.Key, out var_ns))
                    {
                        throw new Exception("naiyo");
                    }

                    foreach (AstahClass cls in va.Value)
                    {
                        string var_name_class = cls.get_var();

                        string temp = $"var {var_name_class} = myBasicModelEditor.createClass(" +
                            $"{var_ns}, \"{cls._クラス名}\"); ";
                        aa.append(temp);

                        if (cls.isInterface)
                        {
                            temp = $"{var_name_class}.addStereotype(\"interface\");";
                            aa.append(temp);
                        }
                    }
                }
                all = all.Replace("//class", aa.get_sb());

                //②汎化
                aa.clear_sb();
                foreach (var va in g_dicNS_Class)
                {
                    foreach (AstahClass cls in va.Value)
                    {
                        string var_name_class = cls.get_var();
                       
                        if (cls._基底クラス名 != "")
                        {
                            string super = to_var_class(cls._基底クラス名);

                            string temp = $"myBasicModelEditor.createGeneralization(" +
                            $"{var_name_class}, {super},\"\");";
                            aa.append(temp);
                        }

                        foreach(var inter in cls._interfaces)
                        {
                            string var_inter = to_var_class(inter);

                            string temp = $"myBasicModelEditor.createDependency(" +
                            $"{var_inter}, {var_name_class},\"\");";
                            aa.append(temp);
                        }

                    }
                }
                all = all.Replace("//super", aa.get_sb());



                //③属性の作成
                aa.clear_sb();
                foreach (var va in g_dicNS_Class)
                {
                    foreach (AstahClass cls in va.Value)
                    {
                        string var_name_class = cls.get_var();

                        foreach (AstahAttribute attr in cls.attributes)
                        {
                            StringBuilder sb = new StringBuilder();

                            string att_class = to_var_class(attr.nameset._02type);
                            string var_att = attr.get_var();

                            string temp =
                             $"var {var_att} = myBasicModelEditor.createAttribute(" +
                             $"{var_name_class},\"{attr.nameset._01name}\",{att_class}); ";
                            aa.append(temp);

                            temp = $"{var_att}.setVisibility(\"{attr._可視性}\");";
                            aa.append(temp);

                            if (attr.isStatic)//追加
                            {
                                temp = $"{var_att}.setStatic(true);";
                                aa.append(temp);
                            }

                            if (attr.isReadOnly)//追加
                            {
                                temp = $"{var_att}.setChangeable(false);";
                                aa.append(temp);
                            }

                            if (attr.nameset._03modifier != "")
                            {
                                temp = $"{var_att}.setTypeModifier(\"{attr.nameset._03modifier}\");";
                                aa.append(temp);
                            }

                            if (attr._ステレオタイプ != "")
                            {
                                temp = $"{var_att}.addStereotype(\"{attr._ステレオタイプ}\");";
                                aa.append(temp);
                            }
                        }
                    }
                }
                all = all.Replace("//filed", aa.get_sb());


                aa.clear_sb();
                foreach (var va in g_dicNS_Class)
                {
                    foreach (AstahClass cls in va.Value)
                    {
                        string var_name_class = cls.get_var();

                        foreach (AstahOperation ope in cls.opes)
                        {
                            StringBuilder sb = new StringBuilder();

                            string var_name_戻り型 = to_var_class(ope.nameset._02type);
                            string var_ope = ope.get_var();

                            string temp =
                             $"var {var_ope} = myBasicModelEditor.createOperation(" +
                             $"{var_name_class},\"{ope.nameset._01name}\",{var_name_戻り型}); ";
                            aa.append(temp);

                            temp = $"{var_ope}.setVisibility(\"{ope._可視性}\");";
                            aa.append(temp);

                            if (ope.isStatic)//追加
                            {
                                temp = $"{var_ope}.setStatic(true);";
                                aa.append(temp);
                            }

                            if (ope.nameset._03modifier != "")
                            {
                                temp = $"{var_ope}.setTypeModifier(\"{ope.nameset._03modifier}\");";
                                aa.append(temp);
                            }

                            foreach (var para in ope.paras)
                            {                                
                                string var_name = to_var_class(para._02type);

                                temp = $"var temp = myBasicModelEditor.createParameter" +
                                    $"({var_ope},\"{para._01name}\",{var_name});";
                                aa.append(temp);

                                if (para._03modifier != "")
                                {
                                    temp = $"temp.setTypeModifier(\"{para._03modifier}\");";
                                    aa.append(temp);
                                }
                            }

                            //if (attr._ステレオタイプ != "")
                            //{
                            //    temp = $"{var_this}.addStereotype(\"{attr._ステレオタイプ}\");";
                            //    aa.append(temp);
                            //}

                        }
                    }
                }

                all = all.Replace("//method", aa.get_sb());
                File.WriteAllText("replaced.js", all, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                { }
            }
            { }
        }

        string to_var_class(string arg)
        {
            int last = arg.LastIndexOf(".");
            string _namespace = arg.Substring(0, last);
            string _classname = arg.Substring(last + 1);
            string var_name = "c_" + bb.get_namespace_var(_namespace) + "_" + _classname;
            return var_name;
        }

        (string, string) get_type_and_modifier(TypeReference typeR)
        {
            (string, string) rtn = ("", "");

            rtn.Item1 = get_型(typeR.FullName);

            if (typeR is GenericInstanceType)
            {
                GenericInstanceType gene = typeR as GenericInstanceType;
                //ひとまず入れ子は考慮しない。
                var ss = gene.GenericArguments.Select(n => get_型(n.Name))
                    .Aggregate((a, b) => a + "," + b);

                rtn.Item2 = $"<{ss}>";
            }
            return rtn;
        }

        Dictionary<string, string> g_dicNS_and_Var = new Dictionary<string, string>();
        SortedDictionary<string, List<AstahClass>> g_dicNS_Class =
                 new SortedDictionary<string, List<AstahClass>>();       

        void make(string _namespace, string ns_parent)
        {
            string[] strs = _namespace.Split('.');
            string namespace_replaced = get_namespace_var(_namespace);
            string temp = $"var {namespace_replaced} = " +
                $"myBasicModelEditor.createPackage({ns_parent},\"{strs[strs.Length - 1]}\");";
            aa.append(temp);
            g_dicNS_and_Var[_namespace] = namespace_replaced;
        }        

        AstahClass gets(TypeReference typeR)
        {
            string fullnamespace = typeR.Namespace;
            List<AstahClass> types = null;
            if (!g_dicNS_Class.TryGetValue(fullnamespace, out types))
            {
                types = new List<AstahClass>();
                g_dicNS_Class[fullnamespace] = types;
            }
            string name = get_型(typeR.Name);
            foreach (var va in types)
            {
                if (va._クラス名 == name)
                {
                    return va;
                }
            }            

            AstahClass clsCLASS = new AstahClass();
            clsCLASS._namespace = fullnamespace;
            clsCLASS._クラス名 = name;

            try
            {
                TypeDefinition def = typeR.Resolve();
                clsCLASS.isInterface = def.IsInterface;
            }
            catch
            {

            }

            types.Add(clsCLASS);
            return clsCLASS;
        }

        AstahClass get_class(TypeDefinition type)
        {
            if (type.FullName == ("<Module>"))
            {
                return null;
            }

            AstahClass cls = gets(type as TypeReference);
            if (cls == null)
            {
                return null;
            }

            if (!type.IsPublic)
            {
                cls._可視性 = "private";
            }

            if (type.BaseType != null)//増えます。
            {
                gets(type.BaseType);
                cls._基底クラス名 = get_型(type.BaseType.FullName);
            }

            cls.isInterface = type.IsInterface;

            if (type.HasInterfaces)
            {
                foreach (InterfaceImplementation iface in type.Interfaces)
                {
                    gets(iface.InterfaceType);

                    string temp = get_型(iface.InterfaceType.FullName);
                    if (!cls._interfaces.Contains(temp))
                    {
                        cls._interfaces.Add(temp);
                    }                   
                }
            }  

            return cls;         
        }

        string get_型(string name_of_class)
        {
            int index_of_kakko = name_of_class.IndexOf('[');
            if (index_of_kakko != -1)
            {
                name_of_class = name_of_class.Substring(0, index_of_kakko);
                name_of_class += "s";
            }

            int index = name_of_class.IndexOf('`');
            if (index != -1)
            {
                return name_of_class.Substring(0, index);
            }
            return name_of_class;
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

        string get_template()
        {
            string rtn = File.ReadAllText("template.txt");

            return rtn;
        }
    }  
}
