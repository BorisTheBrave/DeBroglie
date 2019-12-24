using DeBroglie.Constraints;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.Test.Constraints
{
    [TestFixture]
    class MaxConsecutiveConstraintTest
    {
        [Test]
        public void TestStateMachine()
        {
            void InnerTest(int max, bool[] isSelected, bool periodic, bool expectedContradiction, int[] expectedBans)
            {
                var actualBans = new List<int>();
                var sm = new MaxConsecutiveConstraint.StateMachine(actualBans.Add, periodic, isSelected.Length, max);

                for(var i = 0;i<isSelected.Length;i++)
                {
                    var actualContradiction = sm.Next(i, isSelected[i] ? Tristate.Yes : Tristate.Maybe);
                    if (actualContradiction)
                    {
                        if (expectedContradiction)
                            return;
                        Assert.Fail("Contradiction was unexpected");
                    }
                }
                if (periodic)
                {
                    for (var i = 0; i < max; i++)
                    {
                        var actualContradiction = sm.Next(i, isSelected[i] ? Tristate.Yes : Tristate.Maybe);
                        if (actualContradiction)
                        {
                            if (expectedContradiction)
                                return;
                            Assert.Fail("Contradiction was unexpected");
                        }
                    }
                }

                if (expectedContradiction)
                    Assert.Fail("Expected contradiction");

                CollectionAssert.AreEqual(expectedBans, actualBans);
            }

            /*
            // Case 1 tests (contiguous rouns)
            InnerTest(3, new[] { false, false, false, false, false }, false, false, new int[0]);
            InnerTest(3, new[] { false, true,  false, false, false }, false, false, new int[0]);
            InnerTest(3, new[] { false, true,  true,  false, false }, false, false, new int[0]);
            InnerTest(3, new[] { false, true,  true,  true,  false }, false, false, new[] { 0, 4});
            InnerTest(3, new[] { false, true,  true,  true,  true  }, false, true, null);
            InnerTest(3, new[] { true, true, true, false, false }, false, false, new[] { 3 });

            // Case 1, perioid
            InnerTest(3, new[] { true, true, true, false, false }, true, false, new[] { 4, 3, 4 }); // This one's a bit awkward and generates a redundant ban
            InnerTest(3, new[] { true, true, false, false, true }, true, false, new[] { 3, 2 });
            InnerTest(3, new[] { true, true, true, false, true }, true, true, null);
            */
            // Case 2 tests (discontinuous)
            InnerTest(3, new[] { true, true, false, true, false }, false, false, new [] { 2 });
            InnerTest(3, new[] { true, true, false, true, false }, true, false, new [] { 2, 4 });


        }
    }
}
