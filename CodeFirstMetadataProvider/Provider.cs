﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Practices.Unity;
using CodeFirst.Common;

namespace CodeFirst.Provider
{

   public class Provider : ICodeFirstServiceProvider
   {
      private IUnityContainer unityContainer = new UnityContainer();
      private bool isLoaded;
      private IEnumerable<IMapper> mappers;
      private IEnumerable<IMapper2> mappers2;
      private IEnumerable<ICodeFirstEntry> entryPoints;
      private IEnumerable<ITemplate> templates;

      internal void ConfigureContainer()
      {
         //LoadMetadata();
         LoadIntoContainerWithInterfaceName(typeof(IMetadataLoader<>), this);
         //LoadIntoContainerWithInterfaceName(typeof(IMapper<>));
         LoadIntoContainer<IMapper>();
         LoadIntoContainer<IMapper2>();
         LoadIntoContainer<ICodeFirstEntry>();
         LoadIntoContainer<ITemplate>();
         mappers = UnityContainer.ResolveAll<IMapper>();
         mappers2 = UnityContainer.ResolveAll<IMapper2>();
         entryPoints = UnityContainer.ResolveAll<ICodeFirstEntry>();
         templates = UnityContainer.ResolveAll<ITemplate>();
         isLoaded = true;
      }


      public T GetService<T>()
      {
         return GetServices<T>().FirstOrDefault();
      }

      public IEnumerable<T> GetServices<T>(Func<T, bool> selector = null)
      {
         if (!isLoaded) { ConfigureContainer(); isLoaded = true; }
         var type = typeof(T);
         if (type is ITemplate) { return GetServices(templates.OfType<T>(), selector).OfType<T>(); }
         if (type is ICodeFirstEntry) { return GetServices(entryPoints.OfType<T>(), selector).OfType<T>(); }
         return UnityContainer.ResolveAll<T>().OfType<T>();
      }

      private IEnumerable<T> GetServices<T>(IEnumerable<T> candidates, Func<T, bool> selector = null)
      {
         // TODO: Prioritize 
         if (selector != null) { return candidates.Where(selector); }
         return candidates;
      }

      public T GetService<T>(Func<T, bool> selector = null)
      {
         return GetServices<T>(selector).FirstOrDefault();
      }

      public object GetService(Type serviceType)
      {
         throw new NotImplementedException();
      }
      //[ExcludeFromCodeCoverage]
      //private void AssertLoaded()
      //{
      //   if (!isLoaded || unityContainer == null)
      //   {
      //      Guardian.Assert.AccessedProviderBeforeInitialization(typeof(Provider));
      //   }
      //}

      internal IEnumerable<T> GetItems<T>(
          [CallerMemberName] string callerName = "",
          [CallerLineNumber] int callerLineNumber = 0)
      {
         //AssertLoaded();
         if (!isLoaded) { ConfigureContainer(); isLoaded = true; }
         return UnityContainer.ResolveAll<T>();
      }

      public IMetadataLoader<T> GetMetadataLoader<T>()
             where T : CodeFirstMetadata
      {
         var ret = UnityContainer.ResolveAll<IMetadataLoader<T>>().Single();
         return ret;
      }

      public IMapper GetMapper(Type targetType)
      {
         IMapper fallBack = null;
         foreach (var mapper in mappers)
         {
            foreach (var type in mapper.SupportedTypes)
            {
               if (type == typeof(CodeFirstMetadata))
               {
                  fallBack = mapper;
                  continue;
               }
               if (type.IsAssignableFrom(targetType))
               { return mapper; }
            }
         }
         return fallBack;
      }

      public IMapper2 GetMapper2(Type targetType)
      {
         IMapper2 fallBack = null;
         foreach (var mapper in mappers2)
         {
            foreach (var type in mapper.SupportedTypes)
            {
               if (type == typeof(CodeFirstMetadata))
               {
                  fallBack = mapper;
                  continue;
               }
               if (type.IsAssignableFrom(targetType))
               { return mapper; }
            }
         }
         return fallBack;
      }

      public IMapper GetMapper<T>()
               where T : CodeFirstMetadata
      {
         return GetMapper(typeof(T));
         //var registrations = UnityContainer.Registrations.ToList().Select(x => x.Name);
         //var localMappers = mappers.Select(x=>x.GetType().Name);
         //IMapper fallBack = null;
         //foreach (var mapper in mappers)
         //{
         //   foreach (var type in mapper.SupportedTypes )
         //   {
         //      if (type == typeof(CodeFirstMetadata ))
         //      {
         //         fallBack = mapper;
         //         continue;
         //      }
         //      if (type.IsAssignableFrom(typeof(T)))
         //      { return mapper; }
         //   }
         //}
         //return fallBack;
      }

      //internal bool CheckContainer(
      //    [CallerMemberName] string callerName = "",
      //    [CallerLineNumber] int callerLineNumber = 0)
      //{
      //   AssertLoaded();
      //   var containerChecks = UnityContainer.ResolveAll<IContainerCheck>();
      //   foreach (var check in containerChecks)
      //   {
      //      if (!check.ContainerCheck()) { return false; }
      //   }
      //   return true;
      //}

      private void LoadIntoContainerWithInterfaceName(Type interfaceType, object arg = null)
      {
         var injectionMembers = arg == null
                                 ? new InjectionMember[] { }
                                 : new InjectionMember[] { new InjectionConstructor(arg) };
         foreach (var type in types)
         {
            if (type.GetInterface(interfaceType.Name) != null)
            {
               unityContainer.RegisterType(interfaceType, type, type.FullName,
                          new ContainerControlledLifetimeManager(),
                          injectionMembers);
            }
         }
      }


      public void LoadIntoContainer<T>()
      {
         var factoryType = typeof(T);
         foreach (var type in Types)
         {
            if (factoryType.IsAssignableFrom(type))
            {
               unityContainer.RegisterType(factoryType, type, type.FullName,
                          new ContainerControlledLifetimeManager(),
                          new InjectionMember[] { });
            }
         }
      }

      private void LoadIntoContainerWithArgument<T, TArg>(
                    TArg argument)
      {
         var factoryType = typeof(T);
         foreach (var type in Types)
         {
            if (factoryType.IsAssignableFrom(type))
            {
               unityContainer.RegisterType(factoryType, type, type.FullName,
                          new ContainerControlledLifetimeManager(),
                          new InjectionMember[] { new InjectionConstructor(argument) });
            }
         }
      }


      private IUnityContainer UnityContainer
      {
         get
         {
            // AssertLoaded();
            return unityContainer;
         }
      }

      private IEnumerable<Type> types;
      public IEnumerable<Type> Types
      {
         get
         {
            if (types == null)
            {
               types = AllClasses.FromAssembliesInBasePath()
               .Where(x => x.Namespace != null
                         && !x.Namespace.StartsWith("System")
                         && !x.Namespace.StartsWith("Microsoft")
                         && (!x.IsGenericType || x.IsConstructedGenericType
                              || x.GetInterface("IMetadataLoader`1") != null
                              || x.GetInterface("IMapper`1") != null));
            }
            return types;
         }
      }
   }
}