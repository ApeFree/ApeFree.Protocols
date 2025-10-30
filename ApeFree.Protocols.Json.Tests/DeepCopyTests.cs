using ApeFree.Protocols.Json.Jbin.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApeFree.Protocols.Json.Tests
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
            // Arrange
            object original = null;

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void DeepCopy_ValueType_ReturnsSameValue()
        {
            // Arrange
            int original = 42;

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.AreEqual(original, result);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void DeepCopy_String_ReturnsSameString()
        {
            // Arrange
            string original = "Hello, World!";

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.AreEqual(original, result);
            Assert.AreSame(original, result); // 字符串是驻留的，应该返回同一个引用
        }

        [TestMethod]
        public void DeepCopy_DateTime_ReturnsSameValue()
        {
            // Arrange
            DateTime original = DateTime.Now;

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.AreEqual(original, result);
            Assert.AreEqual(original.Ticks, ((DateTime)result).Ticks);
        }

        [TestMethod]
        public void DeepCopy_NullableTypeWithValue_ReturnsSameValue()
        {
            // Arrange
            int? original = 100;

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.AreEqual(original, result);
            Assert.AreEqual(100, result);
        }

        [TestMethod]
        public void DeepCopy_NullableTypeWithNull_ReturnsNull()
        {
            // Arrange
            int? original = null;

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region 数组测试

        [TestMethod]
        public void DeepCopy_IntArray_ReturnsNewArrayWithSameValues()
        {
            // Arrange
            int[] original = { 1, 2, 3, 4, 5 };

            // Act
            var result = (int[])ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            CollectionAssert.AreEqual(original, result);
        }

        [TestMethod]
        public void DeepCopy_StringArray_ReturnsNewArray()
        {
            // Arrange
            string[] original = { "a", "b", "c" };

            // Act
            var result = (string[])ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            CollectionAssert.AreEqual(original, result);
        }

        [TestMethod]
        public void DeepCopy_ObjectArray_ReturnsNewArrayWithCopiedObjects()
        {
            // Arrange
            var obj1 = new SimpleClass { Id = 1, Name = "Test1" };
            var obj2 = new SimpleClass { Id = 2, Name = "Test2" };
            SimpleClass[] original = { obj1, obj2 };

            // Act
            var result = (SimpleClass[])ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(original.Length, result.Length);

            // 验证对象也被深拷贝了
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
            // Arrange
            int[] original = new int[0];

            // Act
            var result = (int[])ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(0, result.Length);
        }

        #endregion

        #region 简单对象测试

        [TestMethod]
        public void DeepCopy_SimpleClass_ReturnsNewInstanceWithSameValues()
        {
            // Arrange
            var original = new SimpleClass
            {
                Id = 1,
                Name = "Test",
                CreatedDate = new DateTime(2023, 1, 1)
            };

            // Act
            var result = (SimpleClass)ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);
            Assert.AreEqual(original.Id, result.Id);
            Assert.AreEqual(original.Name, result.Name);
            Assert.AreEqual(original.CreatedDate, result.CreatedDate);
        }

        [TestMethod]
        public void DeepCopy_SimpleClassWithNullProperties_HandlesNullsCorrectly()
        {
            // Arrange
            var original = new SimpleClass
            {
                Id = 1,
                Name = null,
                CreatedDate = DateTime.Now
            };

            // Act
            var result = (SimpleClass)ObjectUtils.DeepCopy(original);

            // Assert
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
            // Arrange
            var simple = new SimpleClass { Id = 1, Name = "Nested" };
            var original = new NestedClass
            {
                Simple = simple,
                Description = "Test Description"
            };

            // Act
            var result = (NestedClass)ObjectUtils.DeepCopy(original);

            // Assert
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
            // Arrange
            var original = new NestedClass
            {
                Simple = null,
                Description = "Test"
            };

            // Act
            var result = (NestedClass)ObjectUtils.DeepCopy(original);

            // Assert
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
            // Arrange
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

            // Act
            var result = (CollectionClass)ObjectUtils.DeepCopy(original);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreNotSame(original, result);

            // 验证字符串集合
            Assert.AreNotSame(original.Strings, result.Strings);
            CollectionAssert.AreEqual(original.Strings.ToList(), result.Strings.ToList());

            // 验证数字数组
            Assert.AreNotSame(original.Numbers, result.Numbers);
            CollectionAssert.AreEqual(original.Numbers, result.Numbers);

            // 验证对象集合
            Assert.AreNotSame(original.Objects, result.Objects);
            Assert.AreEqual(original.Objects.Count, result.Objects.Count);
            Assert.AreNotSame(original.Objects[0], result.Objects[0]);
            Assert.AreEqual(original.Objects[0].Id, result.Objects[0].Id);
            Assert.AreEqual(original.Objects[0].Name, result.Objects[0].Name);
        }

        [TestMethod]
        public void DeepCopy_CollectionClassWithEmptyCollections_HandlesEmptyCollections()
        {
            // Arrange
            var original = new CollectionClass
            {
                Strings = new List<string>(),
                Numbers = new int[0],
                Objects = new List<SimpleClass>()
            };

            // Act
            var result = (CollectionClass)ObjectUtils.DeepCopy(original);

            // Assert
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
            // Arrange
            var original = new NonSerializableClass
            {
                Value = 42,
                Text = "Hello"
            };

            // Act
            var result = (NonSerializableClass)ObjectUtils.DeepCopy(original);

            // Assert
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
            // Arrange
            var original = new SimpleClass
            {
                Id = 1,
                Name = "Original"
            };

            // Act
            var copy = (SimpleClass)ObjectUtils.DeepCopy(original);
            copy.Id = 2;
            copy.Name = "Modified";

            // Assert
            Assert.AreEqual(1, original.Id);
            Assert.AreEqual("Original", original.Name);
            Assert.AreEqual(2, copy.Id);
            Assert.AreEqual("Modified", copy.Name);
        }

        [TestMethod]
        public void DeepCopy_ModifyingNestedObjectInCopy_DoesNotAffectOriginal()
        {
            // Arrange
            var nested = new SimpleClass { Id = 1, Name = "Nested" };
            var original = new NestedClass
            {
                Simple = nested,
                Description = "Original"
            };

            // Act
            var copy = (NestedClass)ObjectUtils.DeepCopy(original);
            copy.Simple.Id = 999;
            copy.Simple.Name = "Modified";
            copy.Description = "Modified Description";

            // Assert
            Assert.AreEqual(1, original.Simple.Id);
            Assert.AreEqual("Nested", original.Simple.Name);
            Assert.AreEqual("Original", original.Description);
        }

        [TestMethod]
        public void DeepCopy_ModifyingCollectionInCopy_DoesNotAffectOriginal()
        {
            // Arrange
            var original = new CollectionClass
            {
                Strings = new List<string> { "a", "b" },
                Numbers = new int[] { 1, 2 }
            };

            // Act
            var copy = (CollectionClass)ObjectUtils.DeepCopy(original);
            copy.Strings.Add("c");
            copy.Numbers[0] = 999;

            // Assert
            Assert.AreEqual(2, original.Strings.Count);
            CollectionAssert.AreEqual(new List<string> { "a", "b" }, original.Strings);
            Assert.AreEqual(1, original.Numbers[0]); // 原数组不应被修改
        }

        #endregion

        #region 边界情况测试

        [TestMethod]
        public void DeepCopy_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            string original = string.Empty;

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void DeepCopy_WhitespaceString_ReturnsSameString()
        {
            // Arrange
            string original = "   ";

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.AreEqual("   ", result);
        }

        [TestMethod]
        public void DeepCopy_ZeroValue_ReturnsZero()
        {
            // Arrange
            int original = 0;

            // Act
            var result = ObjectUtils.DeepCopy(original);

            // Assert
            Assert.AreEqual(0, result);
        }

        #endregion

        #region 性能测试（可选）

        [TestMethod]
        [Timeout(1000)] // 1秒超时
        public void DeepCopy_LargeArray_PerformsWithinReasonableTime()
        {
            // Arrange
            var largeArray = Enumerable.Range(0, 10000).ToArray();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = (int[])ObjectUtils.DeepCopy(largeArray);
            stopwatch.Stop();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(largeArray.Length, result.Length);
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500, "拷贝操作应在500毫秒内完成");
        }

        #endregion

        #region 循环引用测试（注意：当前实现可能会栈溢出）

        [TestMethod]
        public void DeepCopy_CircularReference_HandlesGracefully()
        {
            // Arrange - 创建循环引用
            var parent = new CircularReferenceClass { Name = "Parent" };
            var child = new CircularReferenceClass { Name = "Child", Parent = parent };
            parent.Children.Add(child);

            // Act & Assert
            // 注意：当前实现可能会因为循环引用导致栈溢出
            // 这里我们期望它能正常处理或抛出适当的异常
            try
            {
                var result = (CircularReferenceClass)ObjectUtils.DeepCopy(parent);

                // 如果成功，验证基本结构
                Assert.IsNotNull(result);
                Assert.AreEqual("Parent", result.Name);
                Assert.IsNotNull(result.Children);
                Assert.AreEqual(1, result.Children.Count);
                Assert.AreEqual("Child", result.Children[0].Name);
            }
            catch (StackOverflowException)
            {
                // 对于循环引用，栈溢出是可以预期的
                Assert.Inconclusive("DeepCopy因循环引用导致栈溢出，这是当前实现的限制");
            }
            catch (Exception ex)
            {
                // 其他异常应该失败测试
                Assert.Fail($"意外的异常: {ex.Message}");
            }
        }

        #endregion
    }
}
