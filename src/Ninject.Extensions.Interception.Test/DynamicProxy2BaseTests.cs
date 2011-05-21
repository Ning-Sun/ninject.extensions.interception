namespace Ninject.Extensions.Interception
{
    using System;
    using System.Collections.Generic;
    using Castle.DynamicProxy;
    using Ninject.Extensions.Interception.Fakes;
    using Ninject.Extensions.Interception.Infrastructure.Language;
    using Ninject.Extensions.Interception.Interceptors;
    using Xunit;
    using Xunit.Should;

    public class DynamicProxy2BaseTests : DynamicProxy2InterceptionContext
    {
        [Fact]
        public void SelfBoundTypesDeclaringMethodInterceptorsAreProxied()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<ObjectWithMethodInterceptor>().ToSelf();
                var obj = kernel.Get<ObjectWithMethodInterceptor>();
                obj.ShouldNotBeNull();
                var baseTypes = new List<Type>();
                Type type = obj.GetType();
                while (type != typeof(object))
                {
                    baseTypes.Add(type);
                    Type[] interfaces = type.GetInterfaces();
                    baseTypes.AddRange(interfaces);
                    type = type.BaseType;
                }

                typeof(IProxyTargetAccessor).IsAssignableFrom(obj.GetType()).ShouldBeTrue();
            }
        }

        [Fact]
        public void SelfBoundTypesDeclaringMethodInterceptorsCanBeReleased()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<ObjectWithMethodInterceptor>().ToSelf();
                var obj = kernel.Get<ObjectWithMethodInterceptor>();
                obj.ShouldNotBeNull();
            }
        }

        [Fact]
        public void ClassesWithMultiplePropertiesWithTheSameNameCanBeInjected()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<SameNameProperty>().ToSelf();
                var obj = kernel.Get<SameNameProperty>();
                obj.ShouldNotBeNull();
            }
        }

        [Fact]
        public void SelfBoundTypesDeclaringMethodInterceptorsAreIntercepted()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<ObjectWithMethodInterceptor>().ToSelf();
                var obj = kernel.Get<ObjectWithMethodInterceptor>();
                obj.ShouldNotBeNull();
                typeof(IProxyTargetAccessor).IsAssignableFrom(obj.GetType()).ShouldBeTrue();

                CountInterceptor.Reset();

                obj.Foo();
                obj.Bar();

                CountInterceptor.Count.ShouldBe(1);
            }
        }

        [Fact]
        public void SelfBoundTypesDeclaringInterceptorsOnGenericMethodsAreIntercepted()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<ObjectWithGenericMethod>().ToSelf();
                var obj = kernel.Get<ObjectWithGenericMethod>();

                FlagInterceptor.Reset();
                string result = obj.ConvertGeneric(42);

                result.ShouldBe("42");
                FlagInterceptor.WasCalled.ShouldBeTrue();
            }
        }

        [Fact]
        public void ServiceBoundTypesDeclaringMethodInterceptorsAreProxied()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<IFoo>().To<ObjectWithMethodInterceptor>();
                var obj = kernel.Get<IFoo>();

                obj.ShouldNotBeNull();
                typeof(IProxyTargetAccessor).IsAssignableFrom(obj.GetType()).ShouldBeTrue();
            }
        }

        [Fact]
        public void ServiceBoundTypesDeclaringMethodInterceptorsAreIntercepted()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<IFoo>().To<ObjectWithMethodInterceptor>();
                var obj = kernel.Get<IFoo>();

                obj.ShouldNotBeNull();
                typeof(IProxyTargetAccessor).IsAssignableFrom(obj.GetType()).ShouldBeTrue();

                CountInterceptor.Reset();

                obj.Foo();
                obj.Bar();

                CountInterceptor.Count.ShouldBe(1);
            }
        }

        [Fact]
        public void ServiceBoundTypesDeclaringInterceptorsOnGenericMethodsAreIntercepted()
        {
            using ( StandardKernel kernel = CreateDefaultInterceptionKernel() )
            {
                kernel.Bind<IGenericMethod>().To<ObjectWithGenericMethod>();
                var obj = kernel.Get<IGenericMethod>();

                obj.ShouldNotBeNull();
                typeof(IProxyTargetAccessor).IsAssignableFrom(obj.GetType()).ShouldBeTrue();

                FlagInterceptor.Reset();

                string result = obj.ConvertGeneric(42);

                result.ShouldBe("42");
                FlagInterceptor.WasCalled.ShouldBeTrue();
            }
        }

        [Fact]
        public void SelfBoundTypesThatAreProxiedReceiveConstructorInjections()
        {
            using ( StandardKernel kernel = CreateDefaultInterceptionKernel() )
            {
                kernel.Bind<RequestsConstructorInjection>().ToSelf();
                // This is just here to trigger proxying, but we won't intercept any calls
                kernel.Intercept(request => true).With<FlagInterceptor>();

                var obj = kernel.Get<RequestsConstructorInjection>();

                obj.ShouldNotBeNull();
                typeof(IProxyTargetAccessor).IsAssignableFrom(obj.GetType()).ShouldBeTrue();
                obj.Child.ShouldNotBeNull();
            }
        }

        [Fact]
        public void SingletonTests()
        {
            using (StandardKernel kernel = CreateDefaultInterceptionKernel())
            {
                kernel.Bind<RequestsConstructorInjection>().ToSelf().InSingletonScope();
                // This is just here to trigger proxying, but we won't intercept any calls
                kernel.Intercept((request) => true).With<FlagInterceptor>();

                var obj = kernel.Get<RequestsConstructorInjection>();

                obj.ShouldNotBeNull();
                typeof(IProxyTargetAccessor).IsAssignableFrom(obj.GetType()).ShouldBeTrue();
                obj.Child.ShouldNotBeNull();
            }
        }

        [Fact]
        public void NoneVirtualFunctionIntercepted_WhenResolveByInterface_ThenInterceptabe()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                CountInterceptor.Reset();

                kernel.Bind<IFoo>().To<NoneVirtualFooImplementation>().Intercept().With<CountInterceptor>();
                var obj = kernel.Get<IFoo>();
                obj.Foo();

                CountInterceptor.Count.ShouldBe(1);
            }
        }

        [Fact]
        public void NoneVirtualFunctionIntercepted_WhenResolveByInterfaceAndInterceptionByContext_ThenInterceptabe()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                CountInterceptor.Reset();

                kernel.Bind<IFoo>().To<NoneVirtualFooImplementation>();
                kernel.Intercept(ctx => ctx.Request.Service == typeof(IFoo)).With<CountInterceptor>();
                var obj = kernel.Get<IFoo>();
                obj.Foo();

                CountInterceptor.Count.ShouldBe(1);
            }
        }
    
        [Fact]
        public void NoneVirtualPropertyIntercepted_WhenResolveByInterface_ThenInterceptabe()
        {
            using (var kernel = CreateDefaultInterceptionKernel())
            {
                CountInterceptor.Reset();

                const bool OriginalValue = true;
                kernel.Bind<IFoo>().To<NoneVirtualFooImplementation>();
                kernel.Intercept(ctx => ctx.Request.Service == typeof(IFoo)).With<CountInterceptor>();
                var obj = kernel.Get<IFoo>();

                obj.TestProperty = OriginalValue;
                var value = obj.TestProperty;

                CountInterceptor.Count.ShouldBe(1);
                value.ShouldBe(OriginalValue);
            }
        }
    }
}