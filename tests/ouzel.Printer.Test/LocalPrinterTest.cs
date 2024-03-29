namespace ouzel.Printer.Test;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;


/// <summary></summary>
[TestClass]
public class LocalPrinterTest
{
    /// <summary></summary>
    [TestMethod]
    public void GetAllTest()
    {
        foreach(var lp in LocalPrinter.GetAll())
        {
            Console.WriteLine($"{lp.Name} {lp.DeviceId} {lp.Comment}");
        }
    }
}
