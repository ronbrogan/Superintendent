using Microsoft.VisualStudio.TestTools.UnitTesting;
using Superintendent.Core.Serialization;
using System.Text.Json;

namespace Superintendent.Core.Tests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod] public void Serialization() 
        {
            var chainValues = new nint[] { 0, 1, 2 };

            var result = JsonSerializer.Deserialize<TestOffsets>(TestOffsets.Payload, SerializationOptions.DefaultJson);

            Assert.AreEqual((nint)0x123, result.FunctionA.Address);
            Assert.AreEqual((nint)0x234, result.FunctionB.Address);
            Assert.AreEqual((nint)0x345, result.FunctionC.Address);
            Assert.AreEqual((nint)0x456, result.FunctionD.Address);

            Assert.AreEqual((nint)0x567, result.PointerA.Base);
            Assert.AreEqual(0, result.PointerA.Chain.Length);

            Assert.AreEqual((nint)0x678, result.PointerB.Base);
            CollectionAssert.AreEqual(chainValues, result.PointerB.Chain);

            Assert.AreEqual((nint)0x789, result.PointerC.Base);
            CollectionAssert.AreEqual(chainValues, result.PointerC.Chain);

            Assert.AreEqual((nint)0x567, result.AbsPointerA.Base);
            Assert.AreEqual(0, result.AbsPointerA.Chain.Length);

            Assert.AreEqual((nint)0x678, result.AbsPointerB.Base);
            CollectionAssert.AreEqual(chainValues, result.AbsPointerB.Chain);

            Assert.AreEqual((nint)0x789, result.AbsPointerC.Base);
            CollectionAssert.AreEqual(chainValues, result.AbsPointerC.Chain);

        }
    }

    public class TestOffsets
    {
        public Fun FunctionA { get; set; }
        public FunVoid FunctionB { get; set; }

        public Fun<int> FunctionC { get; set; }
        public FunVoid<int> FunctionD { get; set; }

        public Ptr<ushort> PointerA { get; set; }
        public Ptr<ulong> PointerB { get; set; }
        public Ptr<nint> PointerC { get; set; }

        public AbsolutePtr<ushort> AbsPointerA { get; set; }
        public AbsolutePtr<ulong> AbsPointerB { get; set; }
        public AbsolutePtr<nint> AbsPointerC { get; set; }

        public static string Payload = """
            {
                "functionA": "0x123",
                "functionB": "0x234",
                "functionC": "0x345",
                "functionD": "0x456",
                
                "PointerA": "0x567",
                "PointerB": ["0x678", 0, 1, 2],
                "pointerC": ["0x789", "0x0", "0x1", "0x2"],
                
                "AbsPointerA": "0x567",
                "AbsPointerB": ["0x678", 0, 1, 2],
                "absPointerC": ["0x789", "0x0", "0x1", "0x2"]
            }
            """;
    }
}
