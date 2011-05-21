namespace Ninject.Extensions.Interception
{
    using Ninject.Extensions.Interception.Fakes;
    using Ninject.Extensions.Interception.Infrastructure.Language;
    using Xunit;
    using Xunit.Should;

    public abstract class MethodInterceptionContext<TInterceptionModule> : InterceptionTestContext<TInterceptionModule>
        where TInterceptionModule : InterceptionModule, new()
    {
        protected override StandardKernel CreateDefaultInterceptionKernel()
        {
            StandardKernel kernel = base.CreateDefaultInterceptionKernel();
            kernel.Bind<Mock>().ToSelf().WithConstructorArgument("myProperty", "start");
            return kernel;
        }

        [Fact]
        public void MethodInterceptedWithReplace()
        {  
            using (StandardKernel kernel = CreateDefaultInterceptionKernel())
            {
                var mock = kernel.Get<Mock>();

                mock.MyProperty.ShouldBe("start");
                mock.GetMyProperty().ShouldBe("start");
            }

            using (StandardKernel kernel = CreateDefaultInterceptionKernel())
            {
                kernel.InterceptReplace<Mock>(o => o.GetMyProperty(), i => i.ReturnValue = "intercepted");

                var mock = kernel.Get<Mock>();
                mock.MyProperty.ShouldBe("start");
                mock.GetMyProperty().ShouldBe("intercepted");
            }
        }

        [Fact]
        public void MethodInterceptedBefore()
        {
            string testString = "empty";

            using (StandardKernel kernel = CreateDefaultInterceptionKernel())
            {
                kernel.InterceptBefore<Mock>(
                    o => o.SetMyProperty(string.Empty), 
                    i => testString = ((Mock)i.Request.Target).MyProperty);
                var mock = kernel.Get<Mock>();

                mock.MyProperty.ShouldBe("start");
                testString.ShouldBe("empty");

                mock.SetMyProperty("end");

                mock.MyProperty.ShouldBe("end");
                testString.ShouldBe("start");
            }
        }

        [Fact]
        public void MethodInterceptedAfter()
        {
            string testString = "empty";

            using (StandardKernel kernel = CreateDefaultInterceptionKernel())
            {
                kernel.InterceptAfter<Mock>(
                    o => o.SetMyProperty(string.Empty), 
                    i => testString = ((Mock)i.Request.Target).MyProperty);

                var mock = kernel.Get<Mock>();

                mock.MyProperty.ShouldBe("start");
                testString.ShouldBe("empty");

                mock.SetMyProperty("end");

                mock.MyProperty.ShouldBe("end");
                testString.ShouldBe("end");
            }
        }

        [Fact]
        public void MethodCanBeInterceptedWithAddMethodInterceptor()
        {
            string testString = "empty";

            using (StandardKernel kernel = CreateDefaultInterceptionKernel())
            {
                kernel.AddMethodInterceptor(typeof(Mock).GetMethod("SetMyProperty"),
                    i => testString = "intercepted");
                var mock = kernel.Get<Mock>();

                mock.SetMyProperty( "dummy" );

                mock.MyProperty.ShouldBe("start");
                testString.ShouldBe("intercepted");
            }
        }

        [Fact]
        public void MethodInterceptedWithAddMethodInterceptorCanBeResumed()
        {
            string testString = "empty";

            using (StandardKernel kernel = CreateDefaultInterceptionKernel())
            {
                kernel.AddMethodInterceptor( typeof (Mock).GetMethod( "SetMyProperty" ),
                                             i => { 
                                                 testString = "intercepted";  
                                                 i.Proceed();
                                             } );
                var mock = kernel.Get<Mock>();

                mock.SetMyProperty("dummy");

                mock.MyProperty.ShouldBe("dummy");
                testString.ShouldBe("intercepted");
            }
        }
    }
}