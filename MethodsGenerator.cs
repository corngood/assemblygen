/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009 Arno Rehn <arno@arnorehn.de>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom;

static class CodeDomExtensions {

    public static bool TypeEquals(this CodeTypeReference self, CodeTypeReference other) {
        if (self.BaseType != other.BaseType || self.TypeArguments.Count != other.TypeArguments.Count)
            return false;

        for (int i = 0; i < self.TypeArguments.Count; i++) {
            if (!self.TypeArguments[i].TypeEquals(other.TypeArguments[i]))
                return false;
        }
        return true;
    }

    public static bool HasMethod(this CodeTypeDeclaration self, CodeMemberMethod method) {
        foreach (CodeTypeMember member in self.Members) {
            if (!(member is CodeMemberMethod) || member.Name != method.Name)
                continue;

            // now check the parameters
            CodeMemberMethod currentMeth = (CodeMemberMethod) member;
            if (currentMeth.Parameters.Count != method.Parameters.Count)
                continue;
            bool continueOuter = false;
            for (int i = 0; i < method.Parameters.Count; i++) {
                if (!method.Parameters[i].Type.TypeEquals(currentMeth.Parameters[i].Type) && method.Parameters[i].Direction != currentMeth.Parameters[i].Direction) {
                    continueOuter = true;
                    break;
                }
            }
            if (continueOuter)
                continue;
            return true;
        }
        return false;
    }
}

unsafe class MethodsGenerator {
    GeneratorData data;
    Translator translator;
    CodeTypeDeclaration type;

    public MethodsGenerator(GeneratorData data, Translator translator, CodeTypeDeclaration type) {
        this.data = data;
        this.translator = translator;
        this.type = type;
    }

    bool MethodOverrides(Smoke.Method* method, out MemberAttributes access) {
        Dictionary<short, string> allMethods = data.Smoke->FindAllMethods(method->classId, true);
        // Do this with linq... there's probably room for optimization here.
        // Select virtual and pure virtual methods from superclasses.
        var inheritedVirtuals = from entry in allMethods
                                where ((data.Smoke->methods[entry.Key].flags & (ushort) Smoke.MethodFlags.mf_virtual) > 0
                                    || (data.Smoke->methods[entry.Key].flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0)
                                where data.Smoke->methods[entry.Key].classId != method->classId
                                select entry.Key;

        access = MemberAttributes.Public;
        bool ret = false;

        foreach (short index in inheritedVirtuals) {
            Smoke.Method* meth = data.Smoke->methods + index;
            if (meth->name == method->name && meth->args == method->args &&
                (meth->flags & (uint) Smoke.MethodFlags.mf_const) == (method->flags & (uint) Smoke.MethodFlags.mf_const))
            {
                if ((meth->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                    access = MemberAttributes.Family;
                } else {
                    access = MemberAttributes.Public;
                }
                // don't return here - we need the access of the method in the topmost superclass
                ret = true;
            }
        }
        return ret;
    }

    Dictionary<IntPtr, List<CodeTypeMember>> nestedMembersCache = new Dictionary<IntPtr, List<CodeTypeMember>>();
    List<CodeTypeMember> GetAccessibleNestedMembers(Smoke.Class* klass) {
        List<CodeTypeMember> nestedMembers;
        if (nestedMembersCache.TryGetValue((IntPtr) klass, out nestedMembers)) {
            return nestedMembers;
        }

        nestedMembers = new List<CodeTypeMember>();
        for (; klass->className != (char*) IntPtr.Zero;
               klass = data.Smoke->classes + data.Smoke->inheritanceList[klass->parents])
        {
            try {
                foreach (CodeTypeMember member in data.SmokeTypeMap[(IntPtr) klass].Members) {
                    nestedMembers.Add(member);
                }
            } catch (KeyNotFoundException) {
                Debug.Print("  |--Unknown parent: {0}", ByteArrayManager.GetString(klass->className));
            }
        }
        return nestedMembers;
    }

    public void Generate(short index, string mungedName) {
        Smoke.Method *method = data.Smoke->methods + index;
        Generate(method, mungedName);
    }

    public void Generate(Smoke.Method *method, string mungedName) {
        if ((method->flags & (ushort) Smoke.MethodFlags.mf_virtual) == 0 && (method->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0
            && ((method->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0 || (method->flags & (ushort) Smoke.MethodFlags.mf_property) > 0))
        {
            GenerateProperty(method);
        } else {
            GenerateMethod(method, mungedName);
        }
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method) {
        string cppSignature = data.Smoke->GetMethodSignature(method);
        return GenerateBasicMethodDefinition(method, cppSignature);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method, string cppSignature) {
        // do we actually want that method?
        foreach (Regex regex in data.ExcludedMethods) {
            if (regex.IsMatch(cppSignature))
                return null;
        }

        // translate arguments
        List<CodeParameterDeclarationExpression> args = new List<CodeParameterDeclarationExpression>();
        int count = 1;
        bool isRef;
        string className = ByteArrayManager.GetString(data.Smoke->classes[method->classId].className);
        for (short* typeIndex = data.Smoke->argumentList + method->args; *typeIndex > 0; typeIndex++) {
            try {
                CodeParameterDeclarationExpression exp =
                    new CodeParameterDeclarationExpression(translator.CppToCSharp(data.Smoke->types + *typeIndex, out isRef), "arg" + count++);
                if (isRef) {
                    exp.Direction = FieldDirection.Ref;
                }
                args.Add(exp);
            } catch (NotSupportedException) {
                Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
                return null;
            }
        }

        // translate return type
        CodeTypeReference returnType = null;
        try {
            returnType = translator.CppToCSharp(data.Smoke->types + method->ret, out isRef);
        } catch (NotSupportedException) {
            Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
            return null;
        }

        CodeMemberMethod cmm;
        if ((method->flags & (uint) Smoke.MethodFlags.mf_ctor) > 0) {
            cmm = new CodeConstructor();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=
            ((CodeConstructor) cmm).ChainedConstructorArgs.Add(new CodeSnippetExpression("(System.Type) null"));
        } else {
            cmm = new CodeMemberMethod();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=

            string csName = ByteArrayManager.GetString(data.Smoke->methodNames[method->name]);
            if (!csName.StartsWith("operator")) {
                // capitalize the first letter
                StringBuilder builder = new StringBuilder(csName);
                builder[0] = char.ToUpper(builder[0]);
                string tmp = builder.ToString();

                // If the new name clashes with a name of a type declaration, keep the lower-case name.
                var typesWithSameName = from typeDecl in GetAccessibleNestedMembers(data.Smoke->classes + method->classId)
                                        where typeDecl is CodeTypeDeclaration
                                        where typeDecl.Name == tmp
                                        select typeDecl;
                if (typesWithSameName.Count() > 0) {
                    Debug.Print("  |--Conflicting names: method/type: {0} in class {1} - keeping original method name", tmp, className);
                } else {
                    csName = tmp;
                }
            }
            cmm.Name = csName;
            cmm.ReturnType = returnType;
        }

        // for destructors we already have this stuff set
        if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) == 0) {
            // set access
            if ((method->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                cmm.Attributes |= MemberAttributes.Family;
            } else {
                cmm.Attributes |= MemberAttributes.Public;
            }

            // virtual/final
            if ((method->flags & (uint) Smoke.MethodFlags.mf_virtual) == 0) {
                cmm.Attributes |= MemberAttributes.Final | MemberAttributes.New;
            } else {
                MemberAttributes access;
                if (MethodOverrides(method, out access)) {
                    cmm.Attributes = access | MemberAttributes.Override;
                }
            }

            if ((method->flags & (uint) Smoke.MethodFlags.mf_static) > 0) {
                cmm.Attributes |= MemberAttributes.Static;
            }
        } else {
            // hack, so we don't have to use CodeSnippetTypeMember to generator the destructor
            cmm.ReturnType = new CodeTypeReference(" ");
        }

        // add the parameters
        foreach (CodeParameterDeclarationExpression exp in args) {
            cmm.Parameters.Add(exp);
        }
        return cmm;
    }

    public void GenerateMethod(Smoke.Method *method, string mungedName) {
        string cppSignature = data.Smoke->GetMethodSignature(method);
        CodeMemberMethod cmm = GenerateBasicMethodDefinition(method, cppSignature);
        if (cmm == null)
            return;

        if (type.HasMethod(cmm)) {
            Debug.Print("  |--Skipping already implemented method {0}", cppSignature);
            return;
        }

        CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeMethod",
            new CodeAttributeArgument(new CodePrimitiveExpression(cppSignature)));
        cmm.CustomAttributes.Add(attr);

//         CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression(SmokeSupport.smokeInvocation_Invoke);
//         cmm.Statements.Add(new CodeExpressionStatement(invoke));

        type.Members.Add(cmm);
    }

    public void GenerateProperty(Smoke.Method *method) {
    }
}
