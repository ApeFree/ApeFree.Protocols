using ApeFree.Protocols.Json.Jbin.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests.Extensions
{
    [TestClass]
    public class DeepCopyTests
    {
        #region 测试用的辅助类

        [Serializable]
        public class SimpleClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        [Serializable]
        public class NestedClass
        {
            public SimpleClass Simple { get; set; }
            public string Description { get; set; }
        }

        [Serializable]
        public class CollectionClass
        {
            public List<string> Strings { get; set; }
            public int[] Numbers { get; set; }
            public List<SimpleClass> Objects { get; set; }
        }

        [Serializable]
        public class CircularReferenceClass
        {
            public string Name { get; set; }
            public CircularReferenceClass Parent { get; set; }
            public List<CircularReferenceClass> Children { get; set; }

            public CircularReferenceClass()
            {
                Children = new List<CircularReferenceClass>();
            }
        }

        public class NonSerializableClass
        {
            public int Value { get; set; }
            public string Text { get; set; }
        }

        #endregion

        #region 基础类型测试

        [TestMethod]
        public void DeepCopy_NullObject_ReturnsNull()
        {
            object original = null;
            var result = ObjectUtils.DeepCopy(original);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void DeepCopy_ValueType_ReturnsSameValue()
        {
            int original = 42;
            var result = ObjectUtils.DeepCopy(original);
            Assert.AreEqual(original, result);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void DeepCopy_String_ReturnsSameString()
        {
            string original = "Hello, World!";
            var result = ObjectUtils.DeepCopy(original);
            Assert.AreEqual(original, result);
            Assert.AreSame(original, result);
        }

        [TestMethod]
        public void DeepCopy_DateTime_ReturnsSameValue()
        {
            DateTime original = DateTime.Now;
            var result = ObjectUtils.DeepCopy(original);
            Assert.AreEqual(original, result);
            Assert.AreEqual(original.Ticks, ((DateTime)result).Ticks);
        }

        [TestMethod]
        public void DeepCopy_NullableTypeWithValue_ReturnsSameValue()
        {
            int? original = 100;
            var result = ObjectUtils.DeepCopy(original);
            Assert.AreEqual(original, result);
            Assert.AreEqual(100, result);
        }

        [TestMethod]
        public void DeepCopy_NullableTypeWithNull_ReturnsNull()
        {
            int? original = null;
            var result = ObjectUtils.DeepCopy(original);
            Assert.IsNull(result);
        }

        #endregion

        #region 数组测试

        [TestMethod]
        public void DeepCopy_IntArray_ReturnsNewArrayWithSameValues()
        {
            int[] original = { 1, 2, 3, 4, 5 };
            var result = (int[])ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            CollectionAssert.AreEqual(original, result);
        }

        [TestMethod]
        public void DeepCopy_StringArray_ReturnsNewArray()
        {
            string[] original = { "a", "b", "c" };
            var result = (string[])ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            CollectionAssert.AreEqual(original, result);
        }

        [TestMethod]
        public void DeepCopy_ObjectArray_ReturnsNewArrayWithCopiedObjects()
        {
            var obj1 = new SimpleClass { Id = 1, Name = "Test1" };
            var obj2 = new SimpleClass { Id = 2, Name = "Test2" };
            SimpleClass[] original = { obj1, obj2 };

            var result = (SimpleClass[])ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(original.Length, result.Length);

            Assert.AreNotSame(original[0], result[0]);
            Assert.AreEqual(original[0].Id, result[0].Id);
            Assert.AreEqual(original[0].Name, result[0].Name);

            Assert.AreNotSame(original[1], result[1]);
            Assert.AreEqual(original[1].Id, result[1].Id);
            Assert.AreEqual(original[1].Name, result[1].Name);
        }

        [TestMethod]
        public void DeepCopy_EmptyArray_ReturnsNewEmptyArray()
        {
            int[] original = new int[0];
            var result = (int[])ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(0, result.Length);
        }

        #endregion

        #region 简单对象测试

        [TestMethod]
        public void DeepCopy_SimpleClass_ReturnsNewInstanceWithSameValues()
        {
            var original = new SimpleClass
            {
                Id = 1,
                Name = "Test",
                CreatedDate = new DateTime(2023, 1, 1)
            };

            var result = (SimpleClass)ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(original.Id, result.Id);
            Assert.AreEqual(original.Name, result.Name);
            Assert.AreEqual(original.CreatedDate, result.CreatedDate);
        }

        [TestMethod]
        public void DeepCopy_SimpleClassWithNullProperties_HandlesNullsCorrectly()
        {
            var original = new SimpleClass
            {
                Id = 1,
                Name = null,
                CreatedDate = DateTime.Now
            };

            var result = (SimpleClass)ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(original.Id, result.Id);
            Assert.IsNull(result.Name);
            Assert.AreEqual(original.CreatedDate, result.CreatedDate);
        }

        #endregion

        #region 嵌套对象测试

        [TestMethod]
        public void DeepCopy_NestedClass_ReturnsDeepCopy()
        {
            var simple = new SimpleClass { Id = 1, Name = "Nested" };
            var original = new NestedClass
            {
                Simple = simple,
                Description = "Test Description"
            };

            var result = (NestedClass)ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreNotSame(original.Simple, result.Simple);
            Assert.AreEqual(original.Description, result.Description);
            Assert.AreEqual(original.Simple.Id, result.Simple.Id);
            Assert.AreEqual(original.Simple.Name, result.Simple.Name);
        }

        [TestMethod]
        public void DeepCopy_NestedClassWithNull_HandlesNullNestedObject()
        {
            var original = new NestedClass
            {
                Simple = null,
                Description = "Test"
            };

            var result = (NestedClass)ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.IsNull(result.Simple);
            Assert.AreEqual(original.Description, result.Description);
        }

        #endregion

        #region 集合对象测试

        [TestMethod]
        public void DeepCopy_CollectionClass_ReturnsDeepCopy()
        {
            var original = new CollectionClass
            {
                Strings = new List<string> { "a", "b", "c" },
                Numbers = new int[] { 1, 2, 3 },
                Objects = new List<SimpleClass>
                {
                    new SimpleClass { Id = 1, Name = "Obj1" },
                    new SimpleClass { Id = 2, Name = "Obj2" }
                }
            };

            var result = (CollectionClass)ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);

            Assert.AreNotSame(original.Strings, result.Strings);
            CollectionAssert.AreEqual(original.Strings.ToList(), result.Strings.ToList());

            Assert.AreNotSame(original.Numbers, result.Numbers);
            CollectionAssert.AreEqual(original.Numbers, result.Numbers);

            Assert.AreNotSame(original.Objects, result.Objects);
            Assert.AreEqual(original.Objects.Count, result.Objects.Count);
            Assert.AreNotSame(original.Objects[0], result.Objects[0]);
            Assert.AreEqual(original.Objects[0].Id, result.Objects[0].Id);
            Assert.AreEqual(original.Objects[0].Name, result.Objects[0].Name);
        }

        [TestMethod]
        public void DeepCopy_CollectionClassWithEmptyCollections_HandlesEmptyCollections()
        {
            var original = new CollectionClass
            {
                Strings = new List<string>(),
                Numbers = new int[0],
                Objects = new List<SimpleClass>()
            };

            var result = (CollectionClass)ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.IsNotNull(result.Strings);
            Assert.AreEqual(0, result.Strings.Count);
            Assert.IsNotNull(result.Numbers);
            Assert.AreEqual(0, result.Numbers.Length);
            Assert.IsNotNull(result.Objects);
            Assert.AreEqual(0, result.Objects.Count);
        }

        #endregion

        #region 不可序列化对象测试

        [TestMethod]
        public void DeepCopy_NonSerializableClass_ReturnsCopyUsingReflection()
        {
            var original = new NonSerializableClass
            {
                Value = 42,
                Text = "Hello"
            };

            var result = (NonSerializableClass)ObjectUtils.DeepCopy(original);

            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(original.Value, result.Value);
            Assert.AreEqual(original.Text, result.Text);
        }

        #endregion

        #region 修改拷贝不影响原对象测试

        [TestMethod]
        public void DeepCopy_ModifyingCopy_DoesNotAffectOriginal()
        {
            var original = new SimpleClass
            {
                Id = 1,
                Name = "Original"
            };

            var copy = (SimpleClass)ObjectUtils.DeepCopy(original);
            copy.Id = 2;
            copy.Name = "Modified";

            Assert.AreEqual(1, original.Id);
            Assert.AreEqual("Original", original.Name);
            Assert.AreEqual(2, copy.Id);
            Assert.AreEqual("Modified", copy.Name);
        }

        [TestMethod]
        public void DeepCopy_ModifyingNestedObjectInCopy_DoesNotAffectOriginal()
        {
            var nested = new SimpleClass { Id = 1, Name = "Nested" };
            var original = new NestedClass
            {
                Simple = nested,
                Description = "Original"
            };

            var copy = (NestedClass)ObjectUtils.DeepCopy(original);
            copy.Simple.Id = 999;
            copy.Simple.Name = "Modified";
            copy.Description = "Modified Description";

            Assert.AreEqual(1, original.Simple.Id);
            Assert.AreEqual("Nested", original.Simple.Name);
            Assert.AreEqual("Original", original.Description);
        }

        [TestMethod]
        public void DeepCopy_ModifyingCollectionInCopy_DoesNotAffectOriginal()
        {
            var original = new CollectionClass
            {
                Strings = new List<string> { "a", "b" },
                Numbers = new int[] { 1, 2 }
            };

            var copy = (CollectionClass)ObjectUtils.DeepCopy(original);
            copy.Strings.Add("c");
            copy.Numbers[0] = 999;

            Assert.AreEqual(2, original.Strings.Count);
            CollectionAssert.AreEqual(new List<string> { "a", "b" }, original.Strings);
            Assert.AreEqual(1, original.Numbers[0]);
        }

        #endregion

        #region 边界情况测试

        [TestMethod]
        public void DeepCopy_EmptyString_ReturnsEmptyString()
        {
            string original = string.Empty;
            var result = ObjectUtils.DeepCopy(original);
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void DeepCopy_WhitespaceString_ReturnsSameString()
        {
            string original = "   ";
            var result = ObjectUtils.DeepCopy(original);
            Assert.AreEqual("   ", result);
        }

        [TestMethod]
        public void DeepCopy_ZeroValue_ReturnsZero()
        {
            int original = 0;
            var result = ObjectUtils.DeepCopy(original);
            Assert.AreEqual(0, result);
        }

        #endregion

        #region 性能测试

        [TestMethod]
        [Timeout(2000)]
        public void DeepCopy_LargeArray_PerformsWithinReasonableTime()
        {
            var largeArray = Enumerable.Range(0, 10000).ToArray();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = (int[])ObjectUtils.DeepCopy(largeArray);
            stopwatch.Stop();

            Assert.IsNotNull(result);
            Assert.AreEqual(largeArray.Length, result.Length);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 1000, "拷贝操作应在1000毫秒内完成");
        }

        #endregion
    }
}
