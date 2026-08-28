using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Common
{
    public static class TestAssertHelper
    {
        public static void AssertSequenceEqual<T>(T[] expected, T[] actual, string message = null)
        {
            if (expected == null && actual == null) return;
            Assert.IsNotNull(expected, $"Expected was null but actual was not null. {message}");
            Assert.IsNotNull(actual, $"Actual was null but expected was not null. {message}");
            Assert.AreEqual(expected.Length, actual.Length, $"Array length mismatch. {message}");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i], $"Mismatch at index {i}. Expected: {expected[i]}, Actual: {actual[i]}. {message}");
            }
        }

        public static void AssertFloatSequenceEqual(float[] expected, float[] actual, float delta = 0.0001f, string message = null)
        {
            if (expected == null && actual == null) return;
            Assert.IsNotNull(expected, $"Expected was null but actual was not null. {message}");
            Assert.IsNotNull(actual, $"Actual was null but expected was not null. {message}");
            Assert.AreEqual(expected.Length, actual.Length, $"Array length mismatch. {message}");

            for (int i = 0; i < expected.Length; i++)
            {
                if (float.IsNaN(expected[i]))
                {
                    Assert.IsTrue(float.IsNaN(actual[i]), $"Mismatch at index {i}. Expected NaN but got {actual[i]}. {message}");
                }
                else
                {
                    Assert.AreEqual(expected[i], actual[i], delta, $"Mismatch at index {i}. {message}");
                }
            }
        }

        public static void AssertDoubleSequenceEqual(double[] expected, double[] actual, double delta = 0.0000001, string message = null)
        {
            if (expected == null && actual == null) return;
            Assert.IsNotNull(expected, $"Expected was null but actual was not null. {message}");
            Assert.IsNotNull(actual, $"Actual was null but expected was not null. {message}");
            Assert.AreEqual(expected.Length, actual.Length, $"Array length mismatch. {message}");

            for (int i = 0; i < expected.Length; i++)
            {
                if (double.IsNaN(expected[i]))
                {
                    Assert.IsTrue(double.IsNaN(actual[i]), $"Mismatch at index {i}. Expected NaN but got {actual[i]}. {message}");
                }
                else
                {
                    Assert.AreEqual(expected[i], actual[i], delta, $"Mismatch at index {i}. {message}");
                }
            }
        }

        public static void AssertListEqual<T>(List<T> expected, List<T> actual, string message = null)
        {
            if (expected == null && actual == null) return;
            Assert.IsNotNull(expected, $"Expected was null but actual was not null. {message}");
            Assert.IsNotNull(actual, $"Actual was null but expected was not null. {message}");
            Assert.AreEqual(expected.Count, actual.Count, $"List count mismatch. {message}");

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i], $"Mismatch at index {i}. Expected: {expected[i]}, Actual: {actual[i]}. {message}");
            }
        }
    }
}
