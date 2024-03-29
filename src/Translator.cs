﻿/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009, 2010 Arno Rehn <arno@arnorehn.de>

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
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Web.Util;

static class CollectionExtensions {
    public static void AddRange<T, U>(this IDictionary<T, U> self, IDictionary<T, U> dict) {
        foreach (KeyValuePair<T, U> pair in dict) {
            if (self.ContainsKey(pair.Key)) {
                Console.Error.WriteLine("IDictionary already contains {0}", pair.Key);
            }

            self.Add(pair.Key, pair.Value);
        }
    }
}

public unsafe class Translator
{
	private readonly GeneratorData data;

	public Translator(GeneratorData data)
		: this(data, new List<ICustomTranslator>())
	{
	}

	public Translator(GeneratorData data, IEnumerable<ICustomTranslator> customTranslators)
	{
		this.data = data;
		foreach (ICustomTranslator translator in customTranslators)
		{
			this.typeMap.AddRange(translator.TypeMap);
			this.typeStringMap.AddRange(translator.TypeStringMap);
			this.typeCodeMap.AddRange(translator.TypeCodeMap);
			this.InterfaceClasses.AddRange(translator.InterfaceClasses);
			this.ExcludedMethods.AddRange(translator.ExcludedMethods);
			this.NamespacesAsClasses.AddRange(translator.NamespacesAsClasses);
		}
	}

	#region private data

	public class TypeInfo
	{
		public TypeInfo()
		{
		}

		public TypeInfo(string name, int pDepth, bool isRef, bool isConst, bool isUnsigned, string templateParams)
		{
			this.Name = name;
			this.PointerDepth = pDepth;
			this.IsCppRef = isRef;
			this.IsConst = isConst;
			this.IsUnsigned = isUnsigned;
			this.TemplateParameters = templateParams;
		}

		public string Name = string.Empty;
		public int PointerDepth;
		public bool IsCppRef;
		public bool IsConst;
		public bool IsUnsigned;
		public bool IsRef;
		public string TemplateParameters = string.Empty;
	}

	public delegate object TranslateFunc(TypeInfo type, GeneratorData data, Translator translator);

	// map a C++ type string to a .NET type
	private readonly Dictionary<string, Type> typeMap = new Dictionary<string, Type>
	                                                    	{
	                                                    		{"char", typeof(sbyte)},
	                                                    		{"uchar", typeof(byte)},
	                                                    		{"short", typeof(short)},
	                                                    		{"ushort", typeof(ushort)},
	                                                    		{"int", typeof(int)},
	                                                    		{"uint", typeof(uint)},
	                                                    		{"long long", typeof(long)},
	                                                    		{"ulong long", typeof(ulong)},
	                                                    		{"float", typeof(float)},
	                                                    		{"double", typeof(double)},
	                                                    		{"bool", typeof(bool)},
	                                                    		{"void", typeof(void)},
	                                                    	};

	// map a C++ type string to a .NET type string
	private readonly Dictionary<string, string> typeStringMap = new Dictionary<string, string>
	                                                            	{
	                                                            		{"ulong", "NativeULong"},
	                                                            	};

	// custom translation code
	private readonly Dictionary<string, TranslateFunc> typeCodeMap = new Dictionary<string, TranslateFunc>
	                                                                 	{
	                                                                 		{
	                                                                 			"tagMSG",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"FILE",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"GUID",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"_PROCESS_INFORMATION",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"va_list",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"long",
	                                                                 			delegate(TypeInfo type, GeneratorData data,
	                                                                 			         Translator translator)
	                                                                 			{
	                                                                 				if (!type.IsUnsigned)
	                                                                 				{
	                                                                 					return "NativeLong";
	                                                                 				}
	                                                                 				type.IsUnsigned = false;
	                                                                 				return "NativeULong";
	                                                                 			}
	                                                                 		},

	                                                                 		{
	                                                                 			"size_t",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"sockaddr",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 			},
	                                                                 		{
	                                                                 			"_IO_FILE",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 			},

	                                                                 		{
	                                                                 			"_XEvent",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"_XDisplay",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"_XRegion",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 		{
	                                                                 			"FT_FaceRec_",
	                                                                 			delegate { throw new NotSupportedException(); }
	                                                                 		},
	                                                                 	};

	public List<string> InterfaceClasses = new List<string>();

	// C++ method signatures (without return type) that should be excluded
	public List<Regex> ExcludedMethods = new List<Regex>();

	// C++ namespaces that should be mapped to .NET classes
	public List<string> NamespacesAsClasses = new List<string>();

	#endregion

	#region public functions

	public CodeTypeReference CppToCSharp(Smoke.Class* klass)
	{
		bool isRef;
		return this.CppToCSharp(ByteArrayManager.GetString(klass->className), out isRef);
	}

	public CodeTypeReference CppToCSharp(Smoke.Type* type, out bool isRef)
	{
		string typeString = ByteArrayManager.GetString(type->name);
		isRef = false;
		if ((IntPtr) type->name == IntPtr.Zero)
		{
			return new CodeTypeReference(typeof(void));
		}

		Smoke.TypeId typeId = (Smoke.TypeId) (type->flags & (ushort) Smoke.TypeFlags.tf_elem);
		if (typeId == Smoke.TypeId.t_bool)
		{
			return new CodeTypeReference(typeof(bool));
		}
		if (typeId == Smoke.TypeId.t_char)
		{
			return new CodeTypeReference(typeof(sbyte));
		}
		if (typeId == Smoke.TypeId.t_uchar)
		{
			return new CodeTypeReference(typeof(byte));
		}
		if (typeId == Smoke.TypeId.t_short)
		{
			return new CodeTypeReference(typeof(short));
		}
		if (typeId == Smoke.TypeId.t_ushort)
		{
			return new CodeTypeReference(typeof(ushort));
		}
		if (typeId == Smoke.TypeId.t_int)
		{
			return new CodeTypeReference(typeof(int));
		}
		if (typeId == Smoke.TypeId.t_uint)
		{
			// HACK: qdrawutil.h says, DrawingHint is for internal use; nonetheless, SMOKE generates an overload using it; ignoring
			if (typeString == "unsigned int" || typeString == "QFlags<QDrawBorderPixmap::DrawingHint>")
			{
				return new CodeTypeReference(typeof(uint));
			}
		}
		if (typeId == Smoke.TypeId.t_long)
		{
			return new CodeTypeReference("NativeLong");
		}
		if (typeId == Smoke.TypeId.t_ulong)
		{
			return new CodeTypeReference("NativeULong");
		}
		if (typeId == Smoke.TypeId.t_float)
		{
			return new CodeTypeReference(typeof(float));
		}
		if (typeId == Smoke.TypeId.t_double)
		{
			return new CodeTypeReference(typeof(double));
		}
		return this.CppToCSharp(typeString, out isRef);
	}

	public CodeTypeReference CppToCSharp(string typeString)
	{
		bool isRef;
		return this.CppToCSharp(typeString, out isRef);
	}

	public CodeTypeReference CppToCSharp(string typeString, out bool isRef)
	{
		// HACK: some enums - only as typedef flags if used in signals/slots; otherwise cannot be found because meta object only has typedef in method signature; remove when fixed in SMOKE
		switch (typeString)
		{
			case "QDockWidget::DockWidgetFeatures":
				typeString = "QFlags<QDockWidget::DockWidgetFeature>";
				break;
			case "QGraphicsScene::SceneLayers":
				typeString = "QFlags<QGraphicsScene::SceneLayer>";
				break;
			case "Qt::DockWidgetAreas":
				typeString = "QFlags<Qt::DockWidgetArea>";
				break;
			case "Qt::WindowStates":
				typeString = "QFlags<Qt::WindowState>";
				break;
			case "QGraphicsBlurEffect::BlurHints":
				typeString = "QFlags<QGraphicsBlurEffect::BlurHint>";
				break;
			case "QItemSelectionModel::SelectionFlags":
				typeString = "QFlags<QItemSelectionModel::SelectionFlag>";
				break;
			case "Qt::Alignment":
				typeString = "QFlags<Qt::AlignmentFlag>";
				break;
			case "Qt::ToolBarAreas":
				typeString = "QFlags<Qt::ToolBarArea>";
				break;
			case "QIODevice::OpenMode":
				typeString = "QFlags<QIODevice::OpenModeFlag>";
				break;
			case "ColorDialogOptions":
			case "QColorDialog::ColorDialogOptions":
				typeString = "QFlags<QColorDialog::ColorDialogOption>";
				break;
			case "FontDialogOptions":
			case "QFontDialog::FontDialogOptions":
				typeString = "QFlags<QFontDialog::FontDialogOption>";
				break;
			case "PageSetupDialogOptions":
			case "QPageSetupDialog::PageSetupDialogOptions":
				typeString = "QFlags<QPageSetupDialog::PageSetupDialogOption>";
				break;
			case "PrintDialogOptions":
			case "QAbstractPrintDialog::PrintDialogOptions":
				typeString = "QFlags<QAbstractPrintDialog::PrintDialogOption>";
				break;
			case "WatchMode":
			case "QDBusServiceWatcher::WatchMode":
				typeString = "QFlags<QDBusServiceWatcher::WatchModeFlag>";
				break;
		}
		// yes, this won't match Foo<...>::Bar - but we can't wrap that anyway
		isRef = false;
		Match match = Regex.Match(typeString, @"^(const )?(unsigned |signed )?([\w\s:]+)(<.+>)?(\*)*(&)?$");
		if (!match.Success)
		{
			return this.CheckForFunctionPointer(typeString);
		}
		bool isConst = match.Groups[1].Value != string.Empty;
		bool isUnsigned = match.Groups[2].Value == "unsigned ";
		string name = match.Groups[3].Value;
		string templateArgument = match.Groups[4].Value;
		if (templateArgument != string.Empty)
		{
			// strip surrounding < and >
			templateArgument = templateArgument.Substring(1, templateArgument.Length - 2);
		}
		int pointerDepth = match.Groups[5].Value.Length;
		bool isCppRef = match.Groups[6].Value != string.Empty;

		if (name == "QFlags")
		{
			name = templateArgument;
			templateArgument = string.Empty;
		}

		// look up the translations in the translation tables above
		string partialTypeStr;
		TranslateFunc typeFunc;
		if (this.typeStringMap.TryGetValue(name, out partialTypeStr))
		{
			// C++ type string => .NET type string
			name = partialTypeStr;
		}
		else if (this.typeCodeMap.TryGetValue(name, out typeFunc))
		{
			// try to look up custom translation code
			TypeInfo typeInfo = new TypeInfo(name, pointerDepth, isCppRef, isConst, isUnsigned, templateArgument);
			object obj = typeFunc(typeInfo, this.data, this);
			isRef = typeInfo.IsRef;
			name = typeInfo.Name;
			pointerDepth = typeInfo.PointerDepth;
			isCppRef = typeInfo.IsCppRef;
			isConst = typeInfo.IsConst;
			isUnsigned = typeInfo.IsUnsigned;
			templateArgument = typeInfo.TemplateParameters;

			string s = obj as string;
			if (s != null)
			{
				name = s;
			}
			else
			{
				CodeTypeReference typeReference = obj as CodeTypeReference;
				if (typeReference != null)
				{
					return typeReference;
				}
			}
		}
		else
		{
			// if everything fails, just do some standard mapping
			CodeTypeDeclaration ifaceDecl;
			Type t;
			if (this.data.InterfaceTypeMap.TryGetValue(name, out ifaceDecl) ||
			    (this.data.ReferencedTypeMap.TryGetValue(name, out t) && t.IsInterface))
			{
				// this class is used in multiple inheritance, we need the interface
				int colon = name.LastIndexOf("::", StringComparison.Ordinal);
				string prefix = (colon != -1) ? name.Substring(0, colon) : String.Empty;
				string className = (colon != -1) ? name.Substring(colon + 2) : name;
				name = prefix;
				if (name != string.Empty)
				{
					name += '.';
				}
				name += 'I' + className;
			}
			name = name.Replace("::", ".");
		}

		// append 'u' for unsigned types - this is later resolved to a .NET type
		string ret = isUnsigned ? 'u' + name : name;
		if (templateArgument != string.Empty)
		{
			// convert template parameters
			ret += '<';
			List<string> args = Util.SplitUnenclosed(templateArgument, ',', '<', '>');
			for (int i = 0; i < args.Count; i++)
			{
				if (i > 0) ret += ',';
				bool tmp;
				ret += this.CppToCSharp(args[i], out tmp).BaseType;
			}
			ret += '>';
		}
		if (pointerDepth == 2 || (Util.IsPrimitiveType(ret) && (pointerDepth == 1 || (isCppRef && !isConst))))
		{
			isRef = true;
		}
		Type type;
		if (this.typeMap.TryGetValue(ret, out type))
		{
			return new CodeTypeReference(type);
		}
		// HACK: in qprinter.h there is a typedef PageSize PaperSize; but PageSize is obsolete; using PaperSize; remove when the typedef maps are added to SMOKE
		if (ret == "QPrinter.PageSize")
		{
			ret = "QPrinter.PaperSize";
		}
		return new CodeTypeReference(ret);
	}

	private CodeTypeReference CheckForFunctionPointer(string typeString)
	{
		Match match = MatchFunctionPointer(typeString);
		if (match.Success)
		{
			string returnType = match.Groups[1].Value;
			StringBuilder delegateNameBuilder = new StringBuilder();
			CodeTypeDelegate @delegate = new CodeTypeDelegate();
			@delegate.ReturnType = this.CppToCSharp(returnType);
			if (returnType == "void")
			{
				delegateNameBuilder.Append("Action");
			}
			else
			{
				delegateNameBuilder.Append("Func");
				CodeTypeReference returnTypeReference = this.CppToCSharp(returnType);
				delegateNameBuilder.Append(
					returnTypeReference.BaseType.Substring(returnTypeReference.BaseType.LastIndexOf('.') + 1));
			}
			foreach (string argumentType in match.Groups[2].Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
			{
				CodeTypeReference argType = this.CppToCSharp(argumentType);
				string argTypeName = argType.BaseType.Substring(argType.BaseType.LastIndexOf('.') + 1);
				string arg = argTypeName[0].ToString(CultureInfo.InvariantCulture).ToLowerInvariant() +
				             argTypeName.Substring(1);
				@delegate.Parameters.Add(new CodeParameterDeclarationExpression(argType, arg));
				delegateNameBuilder.Append(argTypeName);
			}
			@delegate.Name = delegateNameBuilder.ToString();
			if (@delegate.Name == "Action")
			{
				return new CodeTypeReference(typeof(Action));
			}
			CodeTypeDeclaration globalType = this.data.CSharpTypeMap[this.data.GlobalSpaceClassName];
			if (globalType.Members.Cast<CodeTypeMember>().All(m => m.Name != @delegate.Name))
			{
				globalType.Members.Add(@delegate);
			}
			delegateNameBuilder.Insert(0, globalType.Name + '.');
			return new CodeTypeReference(delegateNameBuilder.ToString());
		}
		throw new NotSupportedException(typeString);
	}

	public static Match MatchFunctionPointer(string typeString)
	{
		return Regex.Match(typeString, @"^([^(]+)\(\*\)\(([^)]*)\)$");
	}

	#endregion
}
