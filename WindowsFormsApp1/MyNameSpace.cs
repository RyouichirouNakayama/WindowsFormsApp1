using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//テンプレートクラスを指定させるのは無理らしい。
//そこで..

namespace WindowsFormsApp1
{
    internal class MyNameSpace
    {
        internal string _Name = "";
        internal string _FullNameSpace = "";
        internal string _FullNameSpace_RemoveDot = "";
        internal List<TypeDefinition> _types = new List<TypeDefinition>();
        internal List<MyNameSpace> _childs = new List<MyNameSpace>();
        internal MyNameSpace _parent = null;

        internal void _01_make_namespace_and_class()
        {
            string ns_parent = _parent?._FullNameSpace_RemoveDot ?? "myProject";//ルートだけ

            aa.append("");//区切り
            string temp = $"var {_FullNameSpace_RemoveDot} = myBasicModelEditor.createPackage({ns_parent},\"{_Name}\");";
            aa.append(temp);

            foreach (TypeDefinition type in this._types)
            {
                make_class(type);
            }

            foreach (MyNameSpace child in this._childs)
            {
                child._01_make_namespace_and_class();
            }
        }

        void make_class(TypeDefinition type)
        {
            string var_class_name = get_var_class(type);

            aa.dicClasses[var_class_name] = true;

            string temp = $"var {var_class_name} = myBasicModelEditor.createClass({_FullNameSpace_RemoveDot}, \"{type.Name}\"); ";

            aa.append(temp);
        }

        internal void _02_make_fields()
        {
            aa.append("");//区切り
            foreach (TypeDefinition type in this._types)
            {
                make_filed(type);
            }

            foreach (MyNameSpace child in this._childs)
            {
                child._02_make_fields();
            }
        }

        string get_var_class(TypeReference type)
        {
            string fullname = type.Namespace.Replace(".", "") + "_" + type.Name;
            string var_class_name = $"c_{fullname}";
            return var_class_name;
        }

        string get_var_field(FieldDefinition field)//参照することはないかもしれない
        {
            string fullname = field.DeclaringType.Namespace.Replace(".", "") + "_" +
                field.DeclaringType.Name;
            string var_field = $"f_{fullname}_{field.Name}";
            return var_field;
        }

        string get_var_field(PropertyDefinition pro)//参照することはないかもしれない
        {
            string fullname = pro.DeclaringType.Namespace.Replace(".", "") + "_" +
                pro.DeclaringType.Name;
            string var_field = $"p_{fullname}_{pro.Name}";
            return var_field;
        }


        string get_typename(string arg,TypeReference type)
        {




            string typeName = arg;
            typeName = get_var_class(type);
            if (!aa.dicClasses.ContainsKey(typeName))
            {
                //CLR標準ライブラリなど他のアセンブリで宣言しているクラスの場合
                typeName = $"\"{type.Name}\"";
            }


            //switch (typeName)
            //{
            //    case "Byte": typeName = "\"byte\""; break;
            //    case "SByte": typeName = "\"sbyte\""; break;
            //    case "Int16": typeName = "\"short\""; break;
            //    case "UInt16": typeName = "\"ushort\""; break;
            //    case "Int32": typeName = "\"int\""; break;
            //    case "UInt32": typeName = "\"uint\""; break;
            //    case "Int64": typeName = "\"long\""; break;
            //    case "UInt64": typeName = "\"ulong\""; break;
            //    case "String": typeName = "\"string\""; break;
            //    case "Char": typeName = "\"char\""; break;
            //    case "Boolean": typeName = "\"bool\""; break;
            //    case "Single": typeName = "\"float\""; break;
            //    case "Double": typeName = "\"double\""; break;
            //    case "Object": typeName = "\"object\""; break;

            //    default:
            //        //このアセンブリ内の自作クラスの場合はよいが
            //        typeName = get_var_class(type);
            //        if (!aa.dicClasses.ContainsKey(typeName))
            //        {
            //            //CLR標準ライブラリなど他のアセンブリで宣言しているクラスの場合
            //            typeName = $"\"{type.Name}\"";
            //        }
            //        break;
            //}
            return typeName;
        }

        void make_filed(TypeDefinition type)
        {
            string temp = "";
            string var_class_name = get_var_class(type);

            if (true)
            {
                foreach (var field in type.Fields)
                {
                    if (field.Name.EndsWith("k__BackingField"))
                    {
                        continue;
                    }

                    string typeName = get_typename(field.FieldType.Name, field.FieldType);
                    string name_field = get_var_field(field);

                    temp =
                    $"var {name_field} = myBasicModelEditor.createAttribute(" +
                    $"{var_class_name},\"{field.Name}\",{typeName}); ";

                    aa.append(temp);
                }
            }

            foreach (PropertyDefinition pro in type.Properties)
            {
                string var_name = get_var_field(pro);

                TypeReference typeReference = pro.PropertyType;

                string kata = get_typename(typeReference.Name, typeReference);

                if (typeReference is GenericInstanceType)
                {
                    GenericInstanceType gene = typeReference as GenericInstanceType;

                    string name = gene.Name.Substring(0, gene.Name.IndexOf('`'));
                    //ReactiveProperty`1

                    var ss = gene.GenericArguments.Select(n => n.Name)
                        .Aggregate((a, b) => a + "," + b);

                    kata = $"\"{name}<{ss}>\"";
                }

                temp = $"var {var_name} = myBasicModelEditor.createAttribute(" +
                    $"{var_class_name},\"{pro.Name}\",{kata}); ";
                aa.append(temp);

                temp = $"{var_name}.setVisibility(\"public\");";
                aa.append(temp);

                temp = $"{var_name}.addStereotype(\"property\");";
                aa.append(temp);
            }

            //Console.WriteLine("------------メソッド------------");
            //foreach (var method in type.Methods)
            //{
            //    if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
            //    {
            //        continue;
            //    }
            //    //明示的な宣言があろうがなかろうが.ctorは存在する。
            //    if (method.Name.Contains(".ctor") || method.Name.Contains(".cctor"))
            //    {
            //        continue;
            //    }

            //    MethodAttributes att = method.Attributes;
            //    att = att & ~MethodAttributes.HideBySig;
            //    string sss = att.ToString().Replace("CompilerControlled, ", "");
            //    Console.WriteLine(
            //        $"{sss} {method.ReturnType.Name} {method.Name}");
            //}
        }

        public override string ToString()
        {
            return _FullNameSpace;
        }

        internal MyNameSpace SearchNameSpace(string full)
        {
            if (this._FullNameSpace == full)
            {
                return this;
            }

            foreach (MyNameSpace child in this._childs)
            {
                MyNameSpace rtn = child.SearchNameSpace(full);
                if (rtn != null)
                {
                    return rtn;
                }
            }
            return null;
        }
    }
}
