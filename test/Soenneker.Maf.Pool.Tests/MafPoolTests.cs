using Soenneker.Maf.Pool.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Maf.Pool.Tests;

[Collection("Collection")]
public sealed class MafPoolTests : FixturedUnitTest
{
    private readonly IMafPool _util;

    public MafPoolTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IMafPool>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
