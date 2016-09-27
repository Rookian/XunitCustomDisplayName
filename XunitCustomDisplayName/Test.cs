using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace XunitCustomDisplayName
{
    public class CustomXunitTestCase : XunitTestCase
    {
        private readonly string _rootPath;

        public CustomXunitTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay testMethodDisplay, ITestMethod testMethod, string rootPath)
            : base(diagnosticMessageSink, testMethodDisplay, testMethod)
        {
            _rootPath = rootPath;
        }

        public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, 
            object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            RunSummary summary = null;
            foreach (var directory in Directory.EnumerateDirectories(_rootPath, "*", SearchOption.AllDirectories))
            {
                DisplayName = directory;
                TestMethodArguments = new object[] { directory };
                summary = await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
            }
            
            return summary;
        }
    }

    [XunitTestCaseDiscoverer("XunitCustomDisplayName.CustomFactDiscoverer", "XunitCustomDisplayName")]
    public class CustomFactAttribute : FactAttribute
    {
        public string RootPath { get; set; }
    }

    public class CustomFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public CustomFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod,
            IAttributeInfo factAttribute)
        {
            var rootPath = factAttribute.GetNamedArgument<string>(nameof(CustomFactAttribute.RootPath));
            yield return new CustomXunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, rootPath);
        }
    }


    public class Test
    {
        private readonly ITestOutputHelper _output;

        public Test(ITestOutputHelper output)
        {
            _output = output;
        }

        [CustomFact(RootPath = @"C:\Temp")]
        public void Should(string directory)
        {
            var directoryInfo = new DirectoryInfo(directory);
            _output.WriteLine($"Aktuelle wird folgendes Verzeichnis untersucht: {directory}");

            var enumerateFiles = directoryInfo.EnumerateFiles().ToList();
            _output.WriteLine($"{enumerateFiles.Count} im Verzeichnis.");

            Assert.True(enumerateFiles.Any(), $"'{directory}' beinhaltet nicht mindestens eine Datei.");
        }
    }
}
