using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace One_Dimension.Test
{
    [TestClass]
    public class FilterTest
    {
        private void assertFloatEquals(float x, float y)
        {
            Assert.AreEqual(Math.Round(x, 1), Math.Round(y, 1));
        }

        private void updateAndCheckAll
            (
            Filter filter,
            float time, float pos,
            float p, float v, float a
            )
        {
            // measured time and position
            filter.update(time, pos);
            // check with estimated
            assertFloatEquals(p, filter.getP());
            assertFloatEquals(v, filter.getV());
            assertFloatEquals(a, filter.getA());
        }

        [TestMethod]
        public void AlgorithmCheckTest()
        {
            Filter filter = new Filter(0.5f, 0.4f, 0.1f);
            filter.init(30000, 50, 0);

            updateAndCheckAll(filter, 5, 30160, 30205, 42.8f, -0.7f);
            updateAndCheckAll(filter, 5, 30365, 30387.5f, 35.6f, -1.1f);
            updateAndCheckAll(filter, 5, 30890, 30721, 57.2f, 1.6f);
            updateAndCheckAll(filter, 5, 31050, 31038.8f, 67.2f, 1.8f);
            updateAndCheckAll(filter, 5, 31785, 31591.1f, 107.2f, 4.9f);
            updateAndCheckAll(filter, 5, 32215, 32201.7f, 133.9f, 5.1f);

            // Test real movement:
            // 1) a = 0, v = 50 until 15 seconds, start position = 30000
            // 2) a = 8 since 15 seconds
            // 3) current time is 30 seconds
            float realDist = 30000 + 50 * 30 + 8 * 15 * 15 / 2;
            Assert.IsTrue(Math.Abs(realDist - filter.getP()) / realDist < 0.01);
        }
    }
}
