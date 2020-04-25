using NUnit.Framework;

namespace AssemblyTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            for (var i = -1000; i <= 1000; i++)
            {
                var order = 4;
                var size = 10;
                var offset = size / 2;
                var index = i;
                var coords = GameManager.IndexToCoords(order, size, offset, index);
                var newIndex = GameManager.CoordsToIndex(order, size, offset, coords);
                Assert.AreEqual(index, newIndex);
            }
        }
    }
}