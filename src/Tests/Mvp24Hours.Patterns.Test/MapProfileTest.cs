using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Contract.Mappings;
using Mvp24Hours.Core.Mappings;
using Mvp24Hours.Extensions;
using Xunit;
using Xunit.Priority;

namespace Mvp24Hours.Patterns.Test;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class MapProfileTest
{
    public class TestAClass
    {
        public int MyProperty1 { get; set; }
    }

    [Trait("Category", "Unit")]
    public class TestBClass : IMapFrom
    {
        public int MyProperty1 { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<TestAClass, TestBClass>();
        }
    }

    [Trait("Category", "Unit")]
    public class TestCClass : IMapFrom
    {
        public int MyProperty2 { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<TestBClass, TestCClass>();
        }
    }

    [Trait("Category", "Unit")]
    public class TestDClass : IMapFrom
    {
        public int MyProperty1 { get; set; }
        public int MyProperty2 { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<TestAClass, TestDClass>();
            profile.CreateMap<TestCClass, TestDClass>();
        }
    }

    [Trait("Category", "Unit")]
    public class TestIgnoreClass : IMapFrom
    {
        public int MyProperty1 { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<TestAClass, TestIgnoreClass>()
                .MapIgnore(x => x.MyProperty1);
        }
    }

    [Trait("Category", "Unit")]
    public class TestPropertyClass : IMapFrom
    {
        public int MyPropertyX { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<TestAClass, TestPropertyClass>()
                .MapProperty(x => x.MyProperty1, x => x.MyPropertyX);
        }
    }

    [Fact]
    public void DefaultTest()
    {
        var local = Assembly.GetExecutingAssembly();
        var mapperConfig = new MapperConfiguration(mc => mc.AddProfile(new MappingProfile(local)), NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var classA = new TestAClass { MyProperty1 = 1 };
        TestBClass classB = mapper.Map<TestBClass>(classA);
        Assert.True(classB != null && classB.MyProperty1 == 1);
    }

    [Fact]
    public void IgnoreTest()
    {
        var local = Assembly.GetExecutingAssembly();
        var mapperConfig = new MapperConfiguration(mc => mc.AddProfile(new MappingProfile(local)), NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var classA = new TestAClass { MyProperty1 = 1 };
        TestIgnoreClass classB = mapper.Map<TestIgnoreClass>(classA);
        Assert.True(classB != null && classB.MyProperty1 == 0);
    }

    [Fact]
    public void PropertyTest()
    {
        var local = Assembly.GetExecutingAssembly();
        var mapperConfig = new MapperConfiguration(mc => mc.AddProfile(new MappingProfile(local)), NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var classA = new TestAClass { MyProperty1 = 1 };
        TestPropertyClass classB = mapper.Map<TestPropertyClass>(classA);
        Assert.True(classB != null && classB.MyPropertyX == 1);
    }

    [Fact]
    public void MergeTest()
    {
        var local = Assembly.GetExecutingAssembly();
        var mapperConfig = new MapperConfiguration(mc => mc.AddProfile(new MappingProfile(local)), NullLoggerFactory.Instance);
        IMapper mapper = mapperConfig.CreateMapper();

        var classA = new TestAClass { MyProperty1 = 1 };
        var classC = new TestCClass { MyProperty2 = 2 };
        TestDClass? classD = mapper.MapMerge<TestDClass>(classA, classC);
        Assert.True(classD != null && classD.MyProperty1 == 1 && classD.MyProperty2 == 2);
    }
}
