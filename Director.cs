using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Centaurus
{
    public class Director
    {
        private string? version;
        private string? cachePath; // TODO: Implement caching

        private readonly Dictionary<string, MethodInfo> commands = [];

        public Director()
        {
            commands = (
                from type in Assembly.GetExecutingAssembly().GetTypes()
                let cca = type.GetCustomAttribute<CommandClassAttribute>()
                where cca != null
                let prefix = (cca.Prefix == "") ? cca.Prefix + "." : ""
                from method in type.GetMethods()
                where method.GetCustomAttribute<NoCommandAttribute>() == null
                where method.IsStatic
                let cna = method.GetCustomAttribute<CommandNameAttribute>()
                select (prefix + (cna?.Name ?? method.Name), method)
            ).ToDictionary(e => e.Item1, e => e.method);
        }

        public void Call(string[] segments)
        {
            if (segments.Length == 0)
            {
                Console.Error.WriteLine("No command specified");
                return;
            }
            MethodInfo m = commands[segments[0]];
            ParameterInfo[] ps = m.GetParameters();
            if (ps.Skip(segments.Length - 1).Any(p => p.DefaultValue == DBNull.Value))
            {
                Console.Error.WriteLine("Not all required parameters satisfied");
                return;
            }
            object?[] param = new object[ps.Length];
            for (int i = 0; i < param.Length; i++)
            {
                param[i] = (i < segments.Length) ? TransformParamter(segments[i], ps[i]) : ps[i].DefaultValue;
            }

            m.Invoke(null, param);
        }

        private static object? TransformParamter(string segment, ParameterInfo parameter)
        {
            if (parameter.ParameterType == typeof(string))
            {
                return segment;
            } else
            {
                Console.WriteLine("Unmapped parameter type detected: " + parameter.ParameterType.FullName);
                return null;
            }
        }
    }
}
