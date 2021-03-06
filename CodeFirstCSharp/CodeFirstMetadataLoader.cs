﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CodeFirst.Common;
using RoslynDom;
using RoslynDom.Common;
using Microsoft.CodeAnalysis;
using RoslynDom.CSharp;

namespace CodeFirst
{
   public class CodeFirstMetadataLoader<T> : IMetadataLoader<T>
           where T : CodeFirstMetadata
   {
      private ICodeFirstServiceProvider serviceProvider;

      public CodeFirstMetadataLoader(ICodeFirstServiceProvider serviceProvider)
      {
         this.serviceProvider = serviceProvider;
      }

      public Type ReturnType
      { get { return typeof(T); } }

      public T LoadFromFile(string fileName, string attributeIdentifier)
      {
         var root = RDom.CSharp.LoadFromFile(fileName);
         return LoadFrom(root, attributeIdentifier);
      }

      public T LoadFromString(string input, string attributeIdentifier)
      {
         var root = RDom.CSharp.Load(input);
         return LoadFrom(root, attributeIdentifier);
      }

      public T LoadFrom(Document document, string attributeIdentifier)
      {
         var root = RDom.CSharp.Load(document);
         return LoadFrom(root, attributeIdentifier);
      }

      public T LoadFrom(IRoot root, string attributeIdentifier)
      {
         if (ShouldRun(root, attributeIdentifier))
         {
            // TODO: We need to be more flexible and look for the desired element and then map from that level
            var cfNamespace = root.Namespaces.First();
            var returnType = typeof(T);
            var mapping = TargetMapping.DeriveMapping("root", "root", returnType.GetTypeInfo()) as TargetNamespaceMapping;
            //var mapper = new CodeFirstMapper();
            var mapper = serviceProvider.GetMapper2<T>();
            var newObj = mapper.Map(mapping, cfNamespace, null);
            return newObj as T;
         }
         return null;
      }
   
      private bool ShouldRun(IRoot root, string attributeIdentifier)
      {
         if (string.IsNullOrWhiteSpace(attributeIdentifier)) { return true; }
         var attributeUsed = root.RootClasses
                              .Any(x => x.Attributes
                                    .Any(y => y.Name == attributeIdentifier));
         //var classes = (from x in root.RootClasses
         //               from y in x.Attributes
         //               where y.Name == attributeIdentifier
         //               select true);
         if (attributeUsed) { return true; }
         var hasNamedBase = root.RootClasses
                        .Any(x => x.BaseType != null && x.BaseType.Name == attributeIdentifier + "Base");
         return hasNamedBase;

      }
   }
}
