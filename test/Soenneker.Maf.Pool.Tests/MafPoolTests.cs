using Soenneker.Maf.Pool.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Maf.Pool.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public sealed class MafPoolTests : HostedUnitTest
{
    private readonly IMafPool _util;

    public MafPoolTests(Host host) : base(host)
    {
        _util = Resolve<IMafPool>(true);
    }

    [Test]
    public void Default()
    {

    }
}
