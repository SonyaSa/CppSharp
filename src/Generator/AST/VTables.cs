﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public static class VTables
    {
        public static List<VTableComponent> GatherVTableMethodEntries(Class @class)
        {
            switch (@class.Layout.ABI)
            {
            case CppAbi.Microsoft:
                return GatherVTableMethodsMS(@class);
            case CppAbi.Itanium:
                return GatherVTableMethodsItanium(@class);
            }

            throw new NotSupportedException(
                string.Format("VTable format for {0} is not supported", @class.Layout.ABI.ToString().Split('.').Last())
            );
        }

        private static List<VTableComponent> GatherVTableMethodEntries(VTableLayout layout)
        {
            var entries = new List<VTableComponent>();
            if (layout == null)
                return entries;

            entries.AddRange(from component in layout.Components
                             where component.Kind != VTableComponentKind.CompleteDtorPointer &&
                                   component.Kind != VTableComponentKind.RTTI &&
                                   component.Kind != VTableComponentKind.UnusedFunctionPointer &&
                                   component.Method != null
                             select component);

            return entries;
        }

        public static List<VTableComponent> GatherVTableMethodsMS(Class @class)
        {
            var entries = new List<VTableComponent>();

            foreach (var vfptr in @class.Layout.VFTables)
                entries.AddRange(GatherVTableMethodEntries(vfptr.Layout));

            return entries;
        }

        public static List<VTableComponent> GatherVTableMethodsItanium(Class @class)
        {
            return GatherVTableMethodEntries(@class.Layout.Layout);
        }

        public static int GetVTableComponentIndex(Class @class, VTableComponent entry)
        {
            switch (@class.Layout.ABI)
            {
            case CppAbi.Microsoft:
                foreach (var vfptr in @class.Layout.VFTables)
                {
                    var index = vfptr.Layout.Components.IndexOf(entry);
                    if (index >= 0)
                        return index;
                }
                break;
            default:
                // ignore offset to top and RTTI
                return @class.Layout.Layout.Components.IndexOf(entry) - 2;
            }

            throw new NotSupportedException();
        }

        static bool CanOverride(Method method, Method @override)
        {
            return method.OriginalName == @override.OriginalName &&
                   method.ReturnType == @override.ReturnType &&
                   method.Parameters.SequenceEqual(@override.Parameters,
                       new ParameterTypeComparer());
        }

        public static int GetVTableIndex(Method method, Class @class)
        {
            switch (@class.Layout.ABI)
            {
                case CppAbi.Microsoft:
                    return (from table in @class.Layout.VFTables
                            let j = table.Layout.Components.FindIndex(m => CanOverride(m.Method, method))
                            where j >= 0
                            select j).First();
                default:
                    // ignore offset to top and RTTI
                    return @class.Layout.Layout.Components.FindIndex(m => m.Method == method) - 2;
            }
        }
    }
}
