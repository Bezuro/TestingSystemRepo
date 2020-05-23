using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TrueXmlSerializerDLL.Tests
{
    [TestClass]
    public class TrueXmlSerializerTest
    {
        [TestMethod]
        public void SaveAndLoadTest()
        {
            List<string> list = new List<string>();
            list.Add("qwerty");
            list.Add("asdfgh");
            list.Add("zxcvbn");

            string path = "trueXmlSerializerTest.xml";

            TrueXmlSerializer.Save(list, path);

            List<string> list2 = TrueXmlSerializer.Load<List<string>>(path);

            CollectionAssert.AreEqual(list, list2);
        }

        [TestMethod]
        public void SerializationAndDeserializationTest()
        {
            List<string> list = new List<string>();
            list.Add("qwerty");
            list.Add("asdfgh");
            list.Add("zxcvbn");

            string serializedList = TrueXmlSerializer.XmlSerialize(list);

            List<string> list2 = TrueXmlSerializer.XmlDeserialize<List<string>>(serializedList);

            CollectionAssert.AreEqual(list, list2);
        }
    }
}
