using BonnFireGames.CustomNativeContainers;
using NUnit.Framework;
using Unity.Mathematics;

namespace Tests.Editor
{
    public class ZOrderGenerationTests
    {
        [Test]
        public void SinglePositionZOrderTest()
        {
            int2 position = new int2(2, 4);
            int expectedZOrderValue = 36;

            uint2 uPosition = new uint2(position);
            uint uExpectedZOrderValue = 36;

            var zPosition = ZOrderGenerator.CalculateZOrderForPosition(position);
            var invertedPosition = ZOrderGenerator.InvertZOrder(zPosition);
            
            var uZPosition = ZOrderGenerator.CalculateZOrderForPosition(uPosition);
            var uInvertedPosition = ZOrderGenerator.InvertZOrder(uZPosition);
            
            Assert.AreEqual(expectedZOrderValue, zPosition);
            Assert.AreEqual(position, invertedPosition);
            
            Assert.AreEqual(uExpectedZOrderValue, uZPosition);
            Assert.AreEqual(uPosition, uInvertedPosition);
        }

        [TestCase(2, 4, 12, 8)]
        [TestCase(7, 8, 22, 15)]
        [TestCase(0, 0, 16, 16)]
        [TestCase(25, 112, 1, 23)]
        public void AreaZOrderTest(int x, int y, int width, int height)
        {
            int4 area = new int4(x, y, x + width, y + height);
            uint4 uArea = new uint4(area);
            

            var zValues = ZOrderGenerator.CalculateZOrderForArea(uArea);
            var zValues2 = ZOrderGenerator.CalculateZOrderForArea(area);

            var inverse = ZOrderGenerator.InvertZOrderForArea(zValues);
            var inverse2 = ZOrderGenerator.InvertZOrderForArea(zValues2);
            
            Assert.AreEqual(uArea, inverse);
            Assert.AreEqual(area, inverse2);
        }
    }
}